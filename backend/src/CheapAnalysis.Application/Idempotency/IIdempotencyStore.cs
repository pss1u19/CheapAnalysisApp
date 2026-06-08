namespace CheapAnalysis.Application.Idempotency;

/// <summary>
/// Backing store for HTTP idempotency keys (T-018). Implementations must make
/// <see cref="TryBeginAsync"/> atomic so that, across concurrent requests sharing
/// a key, exactly one acquires ownership and the rest observe the existing entry.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically claims <paramref name="key"/> for the current request. If the key is
    /// unclaimed, stores an <see cref="IdempotencyStatus.InProgress"/> marker carrying
    /// <paramref name="requestFingerprint"/> and reports the claim as acquired. Otherwise
    /// returns the already-stored entry without modifying it.
    /// </summary>
    Task<IdempotencyBeginResult> TryBeginAsync(
        string key,
        string requestFingerprint,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the in-progress marker with the completed <paramref name="entry"/> and
    /// (re)sets its expiry to <paramref name="timeToLive"/> from now.
    /// </summary>
    Task CompleteAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases <paramref name="key"/> so a later request may reprocess it. Used when a
    /// request fails in a way that should not be replayed (e.g. a 5xx response).
    /// </summary>
    Task AbortAsync(string key, CancellationToken cancellationToken);
}

/// <summary>Lifecycle of a stored idempotency entry.</summary>
public enum IdempotencyStatus
{
    /// <summary>A request holds the key but has not yet produced a final response.</summary>
    InProgress,

    /// <summary>A final response has been captured and can be replayed.</summary>
    Completed,
}

/// <summary>
/// A stored idempotency record: the request fingerprint plus, once completed, the
/// captured response to replay. Response fields are null while <see cref="Status"/>
/// is <see cref="IdempotencyStatus.InProgress"/>.
/// </summary>
public sealed record IdempotencyEntry(
    string RequestFingerprint,
    IdempotencyStatus Status,
    int? StatusCode = null,
    string? ContentType = null,
    byte[]? Body = null);

/// <summary>
/// Outcome of <see cref="IIdempotencyStore.TryBeginAsync"/>. When <see cref="Acquired"/>
/// is true the caller owns the key and <see cref="Existing"/> is null; otherwise
/// <see cref="Existing"/> holds the entry already stored (or null if it expired mid-flight).
/// </summary>
public sealed record IdempotencyBeginResult(bool Acquired, IdempotencyEntry? Existing);
