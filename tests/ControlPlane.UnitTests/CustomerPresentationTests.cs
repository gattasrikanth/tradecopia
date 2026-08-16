using TradeCopia.ControlPlane.Groups;
using TradeCopia.ControlPlane.Presentation;
using TradeCopia.Domain.Safety;
using TradeCopia.Protocol;

namespace TradeCopia.ControlPlane.UnitTests;

public class CustomerPresentationTests
{
    private static EngineAccountRecord Sim() =>
        new("Simulator|2", "Sim101", "Simulator", "Simulation", false, AccountSafetyClass.Simulation);

    private static EngineAccountRecord Demo() =>
        new("Provider31|3", "Personal Demo", "Provider31", "Live", false, AccountSafetyClass.DemoPaper);

    private static EngineAccountRecord Live() =>
        new("InteractiveBrokers|9", "Live Broker", "InteractiveBrokers", "Live", false, AccountSafetyClass.Live);

    private static EngineAccountRecord Unknown() =>
        new("Unknown|1", "Mystery", "Unknown", "", false, AccountSafetyClass.Unknown);

    [Fact]
    public void Safety_labels_are_customer_facing_not_raw_enums()
    {
        Assert.Equal("Simulation", CustomerPresentation.SafetyLabel(AccountSafetyClass.Simulation));
        Assert.Equal("Demo / Paper", CustomerPresentation.SafetyLabel(AccountSafetyClass.DemoPaper));
        Assert.Equal("Live — Locked", CustomerPresentation.SafetyLabel(AccountSafetyClass.Live));
        Assert.Equal("Unknown — Blocked", CustomerPresentation.SafetyLabel(AccountSafetyClass.Unknown));
        Assert.Equal("Demo / Paper", CustomerPresentation.SafetyLabel("DemoPaper"));
        Assert.DoesNotContain("DemoPaper", CustomerPresentation.SafetyLabel(AccountSafetyClass.DemoPaper));
        Assert.False(CustomerPresentation.CustomerLabelContainsInternalKey(CustomerPresentation.SafetyLabel(AccountSafetyClass.Simulation)));
        Assert.False(CustomerPresentation.CustomerLabelContainsInternalKey(CustomerPresentation.DisplayNameFor(new[] { Sim() }, Sim().StableKey)));
    }

    [Fact]
    public void Leader_is_excluded_from_follower_choices()
    {
        var choices = CustomerPresentation.Choices(new[] { Sim(), Demo(), Live(), Unknown() }, Sim().StableKey);
        var leader = choices.Single(c => c.StableKey == Sim().StableKey);
        var demo = choices.Single(c => c.StableKey == Demo().StableKey);
        var live = choices.Single(c => c.StableKey == Live().StableKey);
        var unknown = choices.Single(c => c.StableKey == Unknown().StableKey);
        Assert.False(leader.AvailableAsFollower);
        Assert.Equal("This account is the leader", leader.LockReason);
        Assert.True(demo.AvailableAsFollower);
        Assert.True(demo.AvailableAsLeader);
        Assert.False(live.AvailableAsLeader);
        Assert.False(live.AvailableAsFollower);
        Assert.Equal("Locked in Alpha", live.EligibilityLabel);
        Assert.False(unknown.AvailableAsFollower);
        Assert.Equal("Unknown — Blocked", unknown.LockReason);
        Assert.DoesNotContain("DemoPaper", demo.SafetyLabel);
        Assert.Equal("Personal Demo", demo.DisplayName);
    }

    [Fact]
    public void Blank_alerts_render_as_empty_html()
    {
        Assert.Equal(string.Empty, CustomerPresentation.AlertHtml(Array.Empty<CustomerAlert>()));
        Assert.Equal(string.Empty, CustomerPresentation.AlertHtml(new[]
        {
            new CustomerAlert { Severity = "critical", Title = "", Message = "" }
        }));
        var html = CustomerPresentation.AlertHtml(new[]
        {
            new CustomerAlert { Severity = "warning", Title = "Copying starts disabled", Message = "This dashboard cannot place discretionary trades." }
        });
        Assert.Contains("Copying starts disabled", html);
        Assert.DoesNotContain("UNKNOWN", html);
    }

    [Fact]
    public void Default_sizing_is_one_to_one()
    {
        Assert.Equal("OneToOne", CustomerPresentation.DefaultSizing());
        Assert.Equal("1 : 1", CustomerPresentation.SizingLabel("OneToOne"));
        Assert.Equal("1 : 1", CustomerPresentation.SizingLabel(""));
        Assert.Equal("Multiplier", CustomerPresentation.SizingLabel("Multiplier"));
        Assert.Equal("Fixed quantity", CustomerPresentation.SizingLabel("Fixed"));
    }

    [Fact]
    public void Disconnected_message_is_honest()
    {
        Assert.Contains("disconnected", CustomerPresentation.DisconnectedMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SIM-LEADER-01", CustomerPresentation.DisconnectedMessage());
    }

    [Fact]
    public void Preflight_is_not_ready_when_any_blocking_check_fails()
    {
        var fail = CustomerPresentation.Preflight(false, false, null, Array.Empty<EngineAccountRecord>(), "OneToOne", false, false);
        Assert.False(fail.Ready);
        Assert.DoesNotContain("UNKNOWN", fail.Checks.Select(c => c.Label));

        var ok = CustomerPresentation.Preflight(true, false, Sim(), new[] { Demo() }, "OneToOne", true, false);
        Assert.True(ok.Ready);
        Assert.Equal("Ready", CustomerPresentation.StatusHeadline(true, false, ok.Ready));
        Assert.Equal("Blocked", CustomerPresentation.StatusHeadline(true, false, false));
        Assert.Equal("Engine Disconnected", CustomerPresentation.StatusHeadline(false, false, false));
        Assert.Equal("Copying Enabled", CustomerPresentation.StatusHeadline(true, true, true));
        Assert.Equal("Copying Disabled", CustomerPresentation.CopyingLabel(false));
        Assert.Equal("Engine Connected", CustomerPresentation.EngineStateLabel(true, "Disabled"));
    }

    [Fact]
    public void Save_and_activate_does_not_activate_on_validation_failure()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tc-ux-" + Guid.NewGuid().ToString("N"));
        var store = new GroupConfigStore(dir);
        var accounts = new[] { Sim(), Demo(), Live() };
        var failed = store.SaveAndActivate(null, "Primary", Sim().StableKey, new[] { Sim().StableKey }, "OneToOne", accounts);
        Assert.False(failed.Ok);
        Assert.Equal("leader-cannot-follow", failed.Reason);
        Assert.NotEqual("active", failed.Group?.Status);

        var liveFail = store.SaveAndActivate(null, "Live", Live().StableKey, new[] { Demo().StableKey }, "OneToOne", accounts);
        Assert.False(liveFail.Ok);
        Assert.Contains("leader-not-selectable", liveFail.Reason);
        Assert.NotEqual("active", liveFail.Group?.Status);

        var ok = store.SaveAndActivate(null, "Primary", Sim().StableKey, new[] { Demo().StableKey }, "OneToOne", accounts);
        Assert.True(ok.Ok);
        Assert.Equal("active", ok.Group!.Status);
        Assert.Equal("OneToOne", ok.Group.Sizing);
        Assert.Equal(1, store.List().Count(g => g.Id == ok.Group.Id));
        Assert.Equal("1 : 1", CustomerPresentation.SizingLabel(ok.Group.Sizing));
        Assert.Equal("Sim101", CustomerPresentation.DisplayNameFor(accounts, ok.Group.LeaderKey));
    }

    [Fact]
    public void Enable_confirmation_names_accounts_not_keys()
    {
        var text = CustomerPresentation.EnableConfirmation("Sim101", new[] { "Personal Demo" }, "1 : 1");
        Assert.Contains("Enable Non-Live Copying", text);
        Assert.Contains("Sim101", text);
        Assert.Contains("Personal Demo", text);
        Assert.Contains("1 : 1", text);
        Assert.DoesNotContain("Provider31|3", text);
        Assert.DoesNotContain("DemoPaper", text);
        Assert.Contains("Pause New Entries", CustomerPresentation.PauseHelp());
        Assert.Contains("Disable Copying", CustomerPresentation.DisableHelp());
    }
}
