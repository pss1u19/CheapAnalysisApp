namespace CheapAnalysis.Api.Middleware;

/// <summary>
/// Configuration for <see cref="IdempotencyMiddleware"/> (T-018), bound from the
/// <c>Idempotency</c> configuration section.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Idempotency";

    /// <summary>
    /// Whether idempotency handling is active. Forced off at startup when no Redis
    /// connection string is configured, since there is nowhere to store keys.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long a stored idempotency result is replayable. Defaults to 24 hours.</summary>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Maximum accepted length of the <c>Idempotency-Key</c> header value.</summary>
    public int MaxKeyLength { get; set; } = 255;

    /// <summary>
    /// When true, a request proceeds without replay protection if the store is
    /// unreachable (availability over strict idempotency). When false, store failures
    /// surface as errors. Defaults to true.
    /// </summary>
    public bool FailOpenOnStoreError { get; set; } = true;
}
