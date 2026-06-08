using System.Text.Json;
using CheapAnalysis.Application.Idempotency;
using StackExchange.Redis;

namespace CheapAnalysis.Infrastructure.Idempotency;

/// <summary>
/// Redis-backed <see cref="IIdempotencyStore"/> (T-018). The atomic claim is a
/// <c>SET key value EX ttl NX</c>: only the first caller for a key writes the
/// in-progress marker, so concurrent duplicates observe it instead.
/// </summary>
public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<IdempotencyBeginResult> TryBeginAsync(
        string key,
        string requestFingerprint,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        var marker = Serialize(new IdempotencyEntry(requestFingerprint, IdempotencyStatus.InProgress));

        var acquired = await database
            .StringSetAsync(key, marker, timeToLive, keepTtl: false, When.NotExists)
            .ConfigureAwait(false);
        if (acquired)
        {
            return new IdempotencyBeginResult(Acquired: true, Existing: null);
        }

        var stored = await database.StringGetAsync(key).ConfigureAwait(false);
        var existing = stored.IsNullOrEmpty ? null : Deserialize(stored!);
        return new IdempotencyBeginResult(Acquired: false, existing);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(
        string key,
        IdempotencyEntry entry,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        await database.StringSetAsync(key, Serialize(entry), timeToLive).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AbortAsync(string key, CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    private static string Serialize(IdempotencyEntry entry)
        => JsonSerializer.Serialize(entry, SerializerOptions);

    private static IdempotencyEntry Deserialize(string value)
        => JsonSerializer.Deserialize<IdempotencyEntry>(value, SerializerOptions)!;
}
