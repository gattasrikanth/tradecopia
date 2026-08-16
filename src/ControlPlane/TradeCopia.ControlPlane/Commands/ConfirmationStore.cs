using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace TradeCopia.ControlPlane.Commands;

public sealed class ConfirmationRecord
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string ActionHash { get; init; }
    public required object Preview { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed class ConfirmationStore
{
    private readonly ConcurrentDictionary<string, ConfirmationRecord> _items = new(StringComparer.Ordinal);

    public ConfirmationRecord Prepare(string kind, object preview, string actionHash)
    {
        var record = new ConfirmationRecord
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            Kind = kind,
            ActionHash = actionHash,
            Preview = preview,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2)
        };
        _items[record.Id] = record;
        return record;
    }

    public bool TryConsume(string id, string kind, string actionHash, out ConfirmationRecord? record)
    {
        record = null;
        if (!_items.TryRemove(id, out var found))
        {
            return false;
        }

        if (!string.Equals(found.Kind, kind, StringComparison.Ordinal)
            || !string.Equals(found.ActionHash, actionHash, StringComparison.Ordinal)
            || DateTimeOffset.UtcNow > found.ExpiresAt)
        {
            return false;
        }

        record = found;
        return true;
    }
}
