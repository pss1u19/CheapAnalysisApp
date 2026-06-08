using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CheapAnalysis.Application.Idempotency;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CheapAnalysis.Api.Middleware;

/// <summary>
/// Per-user, Redis-backed idempotency for mutating requests (T-018). A client opts in by
/// sending an <c>Idempotency-Key</c> header on a POST/PUT/PATCH/DELETE. The first request
/// for a key is processed and its response captured; identical retries replay that response,
/// and a reuse of the key with a different payload is rejected with 409.
/// </summary>
public sealed partial class IdempotencyMiddleware(
    RequestDelegate next,
    IOptions<IdempotencyOptions> options,
    ILogger<IdempotencyMiddleware> logger)
{
    /// <summary>Request header carrying the client-supplied idempotency key.</summary>
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>Response header set to <c>true</c> when a stored response is replayed.</summary>
    public const string ReplayedHeader = "Idempotency-Replayed";

    private readonly IdempotencyOptions options = options.Value;

    /// <summary>Processes the request through the idempotency pipeline.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldHandle(context))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyKeyHeader].ToString();
        if (idempotencyKey.Length > options.MaxKeyLength)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid Idempotency-Key",
                $"The {IdempotencyKeyHeader} header must be at most {options.MaxKeyLength} characters.");
            return;
        }

        var store = context.RequestServices.GetRequiredService<IIdempotencyStore>();
        var fingerprint = await ComputeRequestFingerprintAsync(context.Request);
        var storageKey = BuildStorageKey(context, idempotencyKey);

        IdempotencyBeginResult begin;
        try
        {
            begin = await store.TryBeginAsync(storageKey, fingerprint, options.TimeToLive, context.RequestAborted);
        }
        catch (RedisException redisException) when (options.FailOpenOnStoreError)
        {
            LogStoreUnavailable(context.Request.Method, context.Request.Path.ToString(), redisException);
            await next(context);
            return;
        }

        if (!begin.Acquired)
        {
            await HandleExistingAsync(context, begin.Existing, fingerprint);
            return;
        }

        await ProcessAndCaptureAsync(context, store, storageKey, fingerprint);
    }

    private bool ShouldHandle(HttpContext context)
        => options.Enabled
            && IsMutating(context.Request.Method)
            && !string.IsNullOrWhiteSpace(context.Request.Headers[IdempotencyKeyHeader].ToString());

    private static bool IsMutating(string method)
        => HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);

    private static async Task HandleExistingAsync(HttpContext context, IdempotencyEntry? existing, string fingerprint)
    {
        // SET NX failed but the entry is gone (TTL expired between claim and read): another
        // request still holds it logically, so treat it as in-flight.
        if (existing is null || existing.Status == IdempotencyStatus.InProgress)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Request in progress",
                $"A request with this {IdempotencyKeyHeader} is already being processed.");
            return;
        }

        if (existing.RequestFingerprint != fingerprint)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Idempotency-Key conflict",
                $"This {IdempotencyKeyHeader} was already used for a request with a different payload.");
            return;
        }

        await ReplayAsync(context, existing);
    }

    private static async Task ReplayAsync(HttpContext context, IdempotencyEntry entry)
    {
        context.Response.StatusCode = entry.StatusCode ?? StatusCodes.Status200OK;
        if (!string.IsNullOrEmpty(entry.ContentType))
        {
            context.Response.ContentType = entry.ContentType;
        }

        context.Response.Headers[ReplayedHeader] = "true";

        if (entry.Body is { Length: > 0 } body)
        {
            await context.Response.Body.WriteAsync(body);
        }
    }

    private async Task ProcessAndCaptureAsync(
        HttpContext context,
        IIdempotencyStore store,
        string storageKey,
        string fingerprint)
    {
        var originalBody = context.Response.Body;
        using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;

        try
        {
            await next(context);
        }
        catch
        {
            context.Response.Body = originalBody;
            await SafeAbortAsync(store, storageKey);
            throw;
        }

        context.Response.Body = originalBody;
        capturedBody.Position = 0;
        await capturedBody.CopyToAsync(originalBody);

        // Don't cache server errors: they're usually transient, so a retry with the same
        // key should be free to reprocess rather than replay the failure.
        if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            await SafeAbortAsync(store, storageKey);
            return;
        }

        var entry = new IdempotencyEntry(
            fingerprint,
            IdempotencyStatus.Completed,
            context.Response.StatusCode,
            context.Response.ContentType,
            capturedBody.ToArray());

        try
        {
            await store.CompleteAsync(storageKey, entry, options.TimeToLive, context.RequestAborted);
        }
        catch (RedisException redisException) when (options.FailOpenOnStoreError)
        {
            LogCompleteFailed(storageKey, redisException);
        }
    }

    private async Task SafeAbortAsync(IIdempotencyStore store, string storageKey)
    {
        try
        {
            await store.AbortAsync(storageKey, CancellationToken.None);
        }
        catch (RedisException redisException) when (options.FailOpenOnStoreError)
        {
            LogAbortFailed(storageKey, redisException);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Idempotency store unavailable; processing {Method} {Path} without replay protection.")]
    private partial void LogStoreUnavailable(string method, string path, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to persist idempotency result for {StorageKey}.")]
    private partial void LogCompleteFailed(string storageKey, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to release idempotency key {StorageKey}.")]
    private partial void LogAbortFailed(string storageKey, Exception exception);

    private static string BuildStorageKey(HttpContext context, string idempotencyKey)
        => $"idempotency:{ResolveUserId(context)}:{idempotencyKey}";

    private static string ResolveUserId(HttpContext context)
    {
        // No auth scheme is wired yet (Phase 1, T-101); until then everything is "anonymous".
        // Reading the principal now keeps the key namespace correct once sign-in lands.
        var principal = context.User;
        var identifier = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(identifier) ? "anonymous" : identifier;
    }

    private static async Task<string> ComputeRequestFingerprintAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var bodyBuffer = new MemoryStream();
        await request.Body.CopyToAsync(bodyBuffer);
        request.Body.Position = 0;
        return ComputeFingerprint(
            request.Method,
            request.Path,
            bodyBuffer.GetBuffer().AsSpan(0, (int)bodyBuffer.Length));
    }

    /// <summary>
    /// Fingerprints a request as the SHA-256 of method, path, and body. Folding method and
    /// path in means the same key reused on a different route reads as a payload conflict.
    /// </summary>
    internal static string ComputeFingerprint(string method, string path, ReadOnlySpan<byte> body)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(Encoding.UTF8.GetBytes($"{method}\n{path}\n"));
        hasher.AppendData(body);
        return Convert.ToHexStringLower(hasher.GetHashAndReset());
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }
}

/// <summary>Pipeline registration for <see cref="IdempotencyMiddleware"/>.</summary>
public static class IdempotencyMiddlewareExtensions
{
    /// <summary>Adds <see cref="IdempotencyMiddleware"/> to the request pipeline.</summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
