using System.Text;
using CheapAnalysis.Api.Middleware;
using CheapAnalysis.Application.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CheapAnalysis.IntegrationTests.Idempotency;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task Passes_through_when_no_idempotency_key_header()
    {
        var store = new RecordingIdempotencyStore();
        var calls = 0;

        var result = await RunAsync(store, EchoCounting(() => calls++), idempotencyKey: null);

        calls.Should().Be(1);
        store.BeginCalls.Should().Be(0);
        result.ResponseText.Should().Be("ok");
    }

    [Fact]
    public async Task Passes_through_for_non_mutating_methods()
    {
        var store = new RecordingIdempotencyStore();
        var calls = 0;

        await RunAsync(store, EchoCounting(() => calls++), method: HttpMethods.Get);

        calls.Should().Be(1);
        store.BeginCalls.Should().Be(0);
    }

    [Fact]
    public async Task Passes_through_when_disabled()
    {
        var store = new RecordingIdempotencyStore();
        var calls = 0;

        await RunAsync(store, EchoCounting(() => calls++), options: new IdempotencyOptions { Enabled = false });

        calls.Should().Be(1);
        store.BeginCalls.Should().Be(0);
    }

    [Fact]
    public async Task First_request_is_processed_and_its_response_is_stored()
    {
        var store = new RecordingIdempotencyStore();

        var result = await RunAsync(store, Echo("{\"id\":1}"), body: "{\"a\":1}");

        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.ResponseText.Should().Be("{\"id\":1}");
        store.Completed.Should().ContainSingle();
        store.Completed[0].StatusCode.Should().Be(StatusCodes.Status200OK);
        Encoding.UTF8.GetString(store.Completed[0].Body!).Should().Be("{\"id\":1}");
    }

    [Fact]
    public async Task Duplicate_request_replays_the_stored_response_without_reinvoking_the_endpoint()
    {
        var store = new RecordingIdempotencyStore();
        await RunAsync(store, Echo("{\"id\":1}"), body: "{\"a\":1}", idempotencyKey: "dup");

        var calls = 0;
        var replay = await RunAsync(
            store,
            EchoCounting(() => calls++),
            body: "{\"a\":1}",
            idempotencyKey: "dup");

        calls.Should().Be(0);
        replay.Context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        replay.Context.Response.Headers[IdempotencyMiddleware.ReplayedHeader].ToString().Should().Be("true");
        replay.ResponseText.Should().Be("{\"id\":1}");
    }

    [Fact]
    public async Task Same_key_with_a_different_body_is_rejected_as_a_conflict()
    {
        var store = new RecordingIdempotencyStore();
        await RunAsync(store, Echo("{\"id\":1}"), body: "{\"a\":1}", idempotencyKey: "k");

        var calls = 0;
        var conflict = await RunAsync(
            store,
            EchoCounting(() => calls++),
            body: "{\"a\":2}",
            idempotencyKey: "k");

        conflict.Context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        calls.Should().Be(0);
    }

    [Fact]
    public async Task In_flight_key_is_rejected_as_a_conflict()
    {
        var store = new RecordingIdempotencyStore();
        const string body = "{\"a\":1}";
        var fingerprint = IdempotencyMiddleware.ComputeFingerprint(
            HttpMethods.Post,
            "/v1/things",
            Encoding.UTF8.GetBytes(body));
        store.Seed("idempotency:anonymous:k", new IdempotencyEntry(fingerprint, IdempotencyStatus.InProgress));

        var calls = 0;
        var result = await RunAsync(store, EchoCounting(() => calls++), body: body, idempotencyKey: "k");

        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        calls.Should().Be(0);
    }

    [Fact]
    public async Task Server_errors_are_not_cached_so_the_key_can_be_retried()
    {
        var store = new RecordingIdempotencyStore();

        var result = await RunAsync(
            store,
            Echo("upstream exploded", StatusCodes.Status500InternalServerError, "text/plain"),
            body: "{}",
            idempotencyKey: "k");

        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.ResponseText.Should().Be("upstream exploded");
        store.Completed.Should().BeEmpty();
        store.Aborted.Should().Contain("idempotency:anonymous:k");
    }

    [Fact]
    public async Task Key_exceeding_the_max_length_is_rejected()
    {
        var store = new RecordingIdempotencyStore();
        var calls = 0;

        var result = await RunAsync(
            store,
            EchoCounting(() => calls++),
            idempotencyKey: new string('x', 300));

        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        calls.Should().Be(0);
        store.BeginCalls.Should().Be(0);
    }

    private static RequestDelegate Echo(
        string body,
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json")
        => async context =>
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            await context.Response.WriteAsync(body);
        };

    private static RequestDelegate EchoCounting(Action onInvoke)
        => async context =>
        {
            onInvoke();
            await context.Response.WriteAsync("ok");
        };

    private static async Task<InvocationResult> RunAsync(
        IIdempotencyStore store,
        RequestDelegate next,
        string method = "POST",
        string path = "/v1/things",
        string body = "",
        string? idempotencyKey = "key-123",
        IdempotencyOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        services.AddSingleton(store);
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Accept = "application/json";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        if (idempotencyKey is not null)
        {
            context.Request.Headers[IdempotencyMiddleware.IdempotencyKeyHeader] = idempotencyKey;
        }

        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var middleware = new IdempotencyMiddleware(
            next,
            Options.Create(options ?? new IdempotencyOptions()),
            NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        return new InvocationResult(context, Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    private sealed record InvocationResult(HttpContext Context, string ResponseText);

    private sealed class RecordingIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotencyEntry> entries = new(StringComparer.Ordinal);

        public int BeginCalls { get; private set; }

        public List<IdempotencyEntry> Completed { get; } = [];

        public List<string> Aborted { get; } = [];

        public void Seed(string key, IdempotencyEntry entry) => entries[key] = entry;

        public Task<IdempotencyBeginResult> TryBeginAsync(
            string key,
            string requestFingerprint,
            TimeSpan timeToLive,
            CancellationToken cancellationToken)
        {
            BeginCalls++;
            if (entries.TryGetValue(key, out var existing))
            {
                return Task.FromResult(new IdempotencyBeginResult(Acquired: false, existing));
            }

            entries[key] = new IdempotencyEntry(requestFingerprint, IdempotencyStatus.InProgress);
            return Task.FromResult(new IdempotencyBeginResult(Acquired: true, Existing: null));
        }

        public Task CompleteAsync(
            string key,
            IdempotencyEntry entry,
            TimeSpan timeToLive,
            CancellationToken cancellationToken)
        {
            entries[key] = entry;
            Completed.Add(entry);
            return Task.CompletedTask;
        }

        public Task AbortAsync(string key, CancellationToken cancellationToken)
        {
            entries.Remove(key);
            Aborted.Add(key);
            return Task.CompletedTask;
        }
    }
}
