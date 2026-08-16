using TradeCopia.Domain.Safety;
using TradeCopia.Protocol;
using TradeCopia.ControlPlane.Groups;

namespace TradeCopia.ControlPlane.Presentation;

public sealed class CustomerAlert
{
    public string Severity { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string Affected { get; init; } = "";
}

public sealed class AccountChoice
{
    public string StableKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string SafetyLabel { get; init; } = "";
    public string ConnectionLabel { get; init; } = "Connected";
    public string EligibilityLabel { get; init; } = "";
    public string LockReason { get; init; } = "";
    public bool AvailableAsLeader { get; init; }
    public bool AvailableAsFollower { get; init; }
}

public sealed class PreflightCheck
{
    public string Label { get; init; } = "";
    public bool Passed { get; init; }
    public bool Blocking { get; init; } = true;
}

public sealed class PreflightReport
{
    public bool Ready { get; init; }
    public IReadOnlyList<PreflightCheck> Checks { get; init; } = Array.Empty<PreflightCheck>();
}

public static class CustomerPresentation
{
    public static string SafetyLabel(AccountSafetyClass safety)
    {
        return safety switch
        {
            AccountSafetyClass.Simulation => "Simulation",
            AccountSafetyClass.DemoPaper => "Demo / Paper",
            AccountSafetyClass.Live => "Live — Locked",
            _ => "Unknown — Blocked"
        };
    }

    public static string SafetyLabel(string safetyClass)
    {
        if (Enum.TryParse<AccountSafetyClass>(safetyClass, true, out var parsed))
        {
            return SafetyLabel(parsed);
        }

        return "Unknown — Blocked";
    }

    public static string SizingLabel(string sizing)
    {
        if (string.Equals(sizing, "Multiplier", StringComparison.OrdinalIgnoreCase))
        {
            return "Multiplier";
        }

        if (string.Equals(sizing, "Fixed", StringComparison.OrdinalIgnoreCase))
        {
            return "Fixed quantity";
        }

        return "1 : 1";
    }

    public static string DefaultSizing() => "OneToOne";

    public static string EngineStateLabel(bool engineConnected, string engineState)
    {
        return engineConnected ? "Engine Connected" : "Engine Disconnected";
    }

    public static string CopyingLabel(bool copyingEnabled)
    {
        return copyingEnabled ? "Copying Enabled" : "Copying Disabled";
    }

    public static string StatusHeadline(bool engineConnected, bool copyingEnabled, bool preflightReady)
    {
        if (!engineConnected)
        {
            return "Engine Disconnected";
        }

        if (copyingEnabled)
        {
            return "Copying Enabled";
        }

        return preflightReady ? "Ready" : "Blocked";
    }

    public static string AlertHtml(IReadOnlyList<CustomerAlert> alerts)
    {
        if (alerts == null || alerts.Count == 0)
        {
            return string.Empty;
        }

        var html = new System.Text.StringBuilder();
        foreach (var alert in alerts)
        {
            if (string.IsNullOrWhiteSpace(alert.Title) && string.IsNullOrWhiteSpace(alert.Message))
            {
                continue;
            }

            var css = string.Equals(alert.Severity, "critical", StringComparison.OrdinalIgnoreCase) ? "critical" : "warning";
            html.Append("<div class=\"").Append(css).Append("\" role=\"status\">");
            html.Append("<strong>").Append(Escape(alert.Title)).Append("</strong>");
            if (!string.IsNullOrWhiteSpace(alert.Message))
            {
                html.Append(" ").Append(Escape(alert.Message));
            }

            if (!string.IsNullOrWhiteSpace(alert.Affected))
            {
                html.Append(" (").Append(Escape(alert.Affected)).Append(")");
            }

            html.Append("</div>");
        }

        return html.ToString();
    }

    public static IReadOnlyList<AccountChoice> Choices(IReadOnlyList<EngineAccountRecord> accounts, string? leaderKey)
    {
        var list = new List<AccountChoice>();
        if (accounts == null)
        {
            return list;
        }

        foreach (var account in accounts)
        {
            var isLeader = !string.IsNullOrEmpty(leaderKey)
                && string.Equals(account.StableKey, leaderKey, StringComparison.Ordinal);
            var selectable = AccountSafetyClassifier.AlphaMaySelect(account.SafetyClass);
            var lockReason = "";
            var eligibility = "Available";
            if (isLeader)
            {
                eligibility = "Leader";
                lockReason = "This account is the leader";
            }
            else if (account.SafetyClass == AccountSafetyClass.Live)
            {
                eligibility = "Locked in Alpha";
                lockReason = "Live — Locked";
            }
            else if (account.SafetyClass == AccountSafetyClass.Unknown || !selectable)
            {
                eligibility = "Blocked";
                lockReason = "Unknown — Blocked";
            }

            list.Add(new AccountChoice
            {
                StableKey = account.StableKey,
                DisplayName = string.IsNullOrWhiteSpace(account.DisplayName) ? "Account" : account.DisplayName,
                SafetyLabel = SafetyLabel(account.SafetyClass),
                ConnectionLabel = "Connected",
                EligibilityLabel = eligibility,
                LockReason = lockReason,
                AvailableAsLeader = selectable,
                AvailableAsFollower = selectable && !isLeader
            });
        }

        return list;
    }

    public static string DisplayNameFor(IReadOnlyList<EngineAccountRecord> accounts, string stableKey)
    {
        if (accounts == null || string.IsNullOrEmpty(stableKey))
        {
            return "";
        }

        foreach (var account in accounts)
        {
            if (string.Equals(account.StableKey, stableKey, StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(account.DisplayName) ? "Account" : account.DisplayName;
            }
        }

        return "Missing account";
    }

    public static bool CustomerLabelContainsInternalKey(string label)
    {
        return !string.IsNullOrEmpty(label) && label.Contains('|');
    }

    public static PreflightReport Preflight(
        bool engineConnected,
        bool copyingEnabled,
        EngineAccountRecord? leader,
        IReadOnlyList<EngineAccountRecord> followers,
        string sizing,
        bool topologyValid,
        bool blockingDivergence)
    {
        var checks = new List<PreflightCheck>
        {
            new() { Label = "NinjaTrader engine connected", Passed = engineConnected },
            new() { Label = "Leader connected", Passed = leader != null },
            new()
            {
                Label = "Leader verified non-live",
                Passed = leader != null && AccountSafetyClassifier.AlphaMaySelect(leader.SafetyClass)
            },
            new() { Label = "Follower connected", Passed = followers != null && followers.Count > 0 },
            new()
            {
                Label = "Follower verified non-live",
                Passed = followers != null
                    && followers.Count > 0
                    && followers.All(f => AccountSafetyClassifier.AlphaMaySelect(f.SafetyClass))
            },
            new() { Label = "1:1 sizing valid", Passed = string.Equals(SizingLabel(sizing), "1 : 1", StringComparison.Ordinal) },
            new() { Label = "Topology valid", Passed = topologyValid },
            new() { Label = "No blocking divergence", Passed = !blockingDivergence },
            new()
            {
                Label = copyingEnabled ? "Copying enabled" : "Copying currently disabled",
                Passed = true
            }
        };

        var ready = checks.TrueForAll(c => !c.Blocking || c.Passed);
        return new PreflightReport { Ready = ready, Checks = checks };
    }

    public static string EnableConfirmation(
        string leaderDisplay,
        IReadOnlyList<string> followerDisplays,
        string sizingLabel)
    {
        return "Enable Non-Live Copying? Leader: " + leaderDisplay
            + ". Followers: " + string.Join(", ", followerDisplays ?? Array.Empty<string>())
            + ". Sizing: " + sizingLabel
            + ". Instrument: Same instrument / contract. Environment: Simulation / Demo only. TradeCopia will reject Live and Unknown accounts.";
    }

    public static string PauseHelp()
    {
        return "Pause New Entries stops new follower orders. Existing associated protection and exits can still be managed.";
    }

    public static string DisableHelp()
    {
        return "Disable Copying turns the group off. No new copies are generated until you enable again.";
    }

    public static string DisconnectedMessage()
    {
        return "NinjaTrader engine disconnected. Launch/connect NinjaTrader to discover accounts.";
    }

    public static (string? LeaderKey, IReadOnlyList<string> FollowerKeys) PreferredPair(IReadOnlyList<EngineAccountRecord> accounts)
    {
        var choices = Choices(accounts, null);
        var sims = choices.Where(c => c.AvailableAsLeader && c.SafetyLabel == "Simulation").ToList();
        var leader = sims.FirstOrDefault(c =>
            c.DisplayName.IndexOf("backtest", StringComparison.OrdinalIgnoreCase) < 0) ?? sims.FirstOrDefault();
        if (leader == null)
        {
            return (null, Array.Empty<string>());
        }

        var demos = choices
            .Where(c => c.AvailableAsFollower
                && c.StableKey != leader.StableKey
                && c.SafetyLabel == "Demo / Paper")
            .ToList();
        var personal = demos.Where(c =>
            c.DisplayName.IndexOf("playback", StringComparison.OrdinalIgnoreCase) < 0).ToList();
        var followers = (personal.Count > 0 ? personal : demos).Select(c => c.StableKey).ToList();
        return (leader.StableKey, followers);
    }

    private static string Escape(string value)
    {
        return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
