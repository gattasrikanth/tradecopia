using System.Text.Json;
using TradeCopia.Domain.Safety;
using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane.Groups;

public sealed class GroupRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string LeaderKey { get; set; } = "";
    public List<string> FollowerKeys { get; set; } = new();
    public string Sizing { get; set; } = "OneToOne";
    public string Status { get; set; } = "draft";
    public int Version { get; set; } = 1;
}

public sealed class GroupConfigStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private List<GroupRecord> _groups = new();

    public GroupConfigStore(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "groups.json");
        Load();
    }

    public IReadOnlyList<GroupRecord> List()
    {
        lock (_gate)
        {
            return _groups.Select(Clone).ToList();
        }
    }

    public IReadOnlyList<GroupRecord> ListCustomerCards()
    {
        lock (_gate)
        {
            CompactDuplicatesUnlocked();
            return _groups
                .GroupBy(g => string.IsNullOrWhiteSpace(g.Name) ? g.Id : g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(x => x.Status == "active") ?? g.Last())
                .Select(Clone)
                .ToList();
        }
    }

    public GroupRecord CreateDraft(string name, string leaderKey, IEnumerable<string> followers, string sizing = "OneToOne")
    {
        var record = new GroupRecord
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Primary" : name.Trim(),
            LeaderKey = leaderKey ?? "",
            FollowerKeys = followers?.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.Ordinal).ToList() ?? new List<string>(),
            Sizing = string.IsNullOrWhiteSpace(sizing) ? "OneToOne" : sizing.Trim(),
            Status = "draft",
            Version = 1
        };
        lock (_gate)
        {
            _groups.Add(record);
            Persist();
            return Clone(record);
        }
    }

    public GroupRecord? Get(string id)
    {
        lock (_gate)
        {
            var found = _groups.FirstOrDefault(g => g.Id == id);
            return found == null ? null : Clone(found);
        }
    }

    public GroupRecord? ReplaceDraft(string id, string name, string leaderKey, IEnumerable<string> followers, string sizing)
    {
        lock (_gate)
        {
            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                return null;
            }

            group.Name = string.IsNullOrWhiteSpace(name) ? group.Name : name.Trim();
            group.LeaderKey = leaderKey ?? "";
            group.FollowerKeys = followers?.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.Ordinal).ToList() ?? new List<string>();
            group.Sizing = string.IsNullOrWhiteSpace(sizing) ? group.Sizing : sizing.Trim();
            Persist();
            return Clone(group);
        }
    }

    public (bool Ok, string Reason, GroupRecord? Group) SaveAndActivate(
        string? id,
        string name,
        string leaderKey,
        IEnumerable<string> followers,
        string sizing,
        IReadOnlyList<EngineAccountRecord> accounts)
    {
        GroupRecord draft;
        var resolvedId = id;
        if (string.IsNullOrWhiteSpace(resolvedId))
        {
            lock (_gate)
            {
                CompactDuplicatesUnlocked();
                var existing = FindLogicalUnlocked(name);
                resolvedId = existing?.Id;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedId))
        {
            draft = CreateDraft(name, leaderKey, followers, sizing);
        }
        else
        {
            var updated = ReplaceDraft(resolvedId, name, leaderKey, followers, sizing);
            if (updated == null)
            {
                return (false, "not-found", null);
            }

            draft = updated;
        }

        var validated = Validate(draft.Id, accounts);
        if (!validated.Ok || validated.Group == null)
        {
            return (false, validated.Reason, validated.Group);
        }

        if (string.Equals(validated.Group.Status, "active", StringComparison.Ordinal))
        {
            return (false, "validate-must-not-activate", validated.Group);
        }

        return Activate(validated.Group.Id, validated.Group.Version, accounts);
    }

    public (bool Ok, string Reason, GroupRecord? Group) Validate(string id, IReadOnlyList<EngineAccountRecord> accounts)
    {
        lock (_gate)
        {
            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                return (false, "not-found", null);
            }

            var reason = ValidateCore(group, accounts);
            if (reason != null)
            {
                group.Status = "draft";
                Persist();
                return (false, reason, Clone(group));
            }

            group.Status = "validated";
            group.Version++;
            Persist();
            return (true, "validated", Clone(group));
        }
    }

    public (bool Ok, string Reason, GroupRecord? Group) Activate(string id, int expectedVersion, IReadOnlyList<EngineAccountRecord> accounts)
    {
        lock (_gate)
        {
            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                return (false, "not-found", null);
            }

            if (group.Version != expectedVersion)
            {
                return (false, "stale-activate", Clone(group));
            }

            var reason = ValidateCore(group, accounts);
            if (reason != null)
            {
                return (false, reason, Clone(group));
            }

            _groups.RemoveAll(other =>
                other.Id != group.Id
                && string.Equals(
                    string.IsNullOrWhiteSpace(other.Name) ? other.Id : other.Name.Trim(),
                    string.IsNullOrWhiteSpace(group.Name) ? group.Id : group.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            group.Status = "active";
            group.Version++;
            Persist();
            return (true, "activated", Clone(group));
        }
    }

    public static string? ValidateCore(GroupRecord group, IReadOnlyList<EngineAccountRecord> accounts)
    {
        if (string.IsNullOrWhiteSpace(group.LeaderKey))
        {
            return "leader-required";
        }

        if (group.FollowerKeys.Count == 0)
        {
            return "follower-required";
        }

        if (group.FollowerKeys.Contains(group.LeaderKey, StringComparer.Ordinal))
        {
            return "leader-cannot-follow";
        }

        var byKey = accounts.ToDictionary(a => a.StableKey, StringComparer.Ordinal);
        if (!byKey.TryGetValue(group.LeaderKey, out var leader))
        {
            return "leader-not-discovered";
        }

        if (!AccountSafetyClassifier.AlphaMaySelect(leader.SafetyClass))
        {
            return "leader-not-selectable:" + leader.SafetyClass;
        }

        foreach (var followerKey in group.FollowerKeys)
        {
            if (!byKey.TryGetValue(followerKey, out var follower))
            {
                return "follower-not-discovered";
            }

            if (!AccountSafetyClassifier.AlphaMaySelect(follower.SafetyClass))
            {
                return "follower-not-selectable:" + follower.SafetyClass;
            }
        }

        return null;
    }

    private void Load()
    {
        if (!File.Exists(_path))
        {
            return;
        }

        var json = File.ReadAllText(_path);
        _groups = JsonSerializer.Deserialize<List<GroupRecord>>(json) ?? new List<GroupRecord>();
        CompactDuplicatesUnlocked();
        Persist();
    }

    private GroupRecord? FindLogicalUnlocked(string name)
    {
        var key = string.IsNullOrWhiteSpace(name) ? "Primary" : name.Trim();
        var named = _groups.Where(g =>
            string.Equals(string.IsNullOrWhiteSpace(g.Name) ? "Primary" : g.Name.Trim(), key, StringComparison.OrdinalIgnoreCase)).ToList();
        if (named.Count == 0 && _groups.Count == 1)
        {
            return _groups[0];
        }

        return named.FirstOrDefault(g => g.Status == "active") ?? named.LastOrDefault();
    }

    private void CompactDuplicatesUnlocked()
    {
        if (_groups.Count < 2)
        {
            return;
        }

        var keep = new List<GroupRecord>();
        foreach (var set in _groups.GroupBy(g => string.IsNullOrWhiteSpace(g.Name) ? g.Id : g.Name.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            keep.Add(set.FirstOrDefault(x => x.Status == "active") ?? set.Last());
        }

        _groups = keep;
    }

    private void Persist()
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(_groups));
    }

    private static GroupRecord Clone(GroupRecord g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        LeaderKey = g.LeaderKey,
        FollowerKeys = g.FollowerKeys.ToList(),
        Sizing = g.Sizing,
        Status = g.Status,
        Version = g.Version
    };
}
