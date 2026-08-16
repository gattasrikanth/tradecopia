using System;
using System.Collections.Generic;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Model;

namespace TradeCopia.Domain.UnitTests
{
    internal static class TestSupport
    {
        public static AccountKey Leader => new AccountKey("SIM-LEADER-01");
        public static AccountKey Follower1 => new AccountKey("SIM-FOLLOWER-01");
        public static AccountKey Follower2 => new AccountKey("SIM-FOLLOWER-02");
        public static InstrumentKey Nq => new InstrumentKey("NQ 06-26");

        public static ActiveConfigSnapshot Config(
            EngineSafetyState engine = EngineSafetyState.Enabled,
            GroupEnabledState group = GroupEnabledState.Enabled,
            SizingPolicy? sizing = null,
            bool simOnly = false,
            AccountReadiness followerReady = AccountReadiness.Ready,
            params AccountKey[] extraFollowers)
        {
            sizing = sizing ?? SizingPolicy.OneToOne();
            var followers = new List<FollowerRule>
            {
                new FollowerRule(Follower1, true, sizing, Array.Empty<InstrumentMapping>())
            };
            foreach (var extra in extraFollowers)
            {
                followers.Add(new FollowerRule(extra, true, SizingPolicy.OneToOne(), Array.Empty<InstrumentMapping>()));
            }

            var copyGroup = new CopyGroup(
                CopyGroupId.New(),
                "SIM group",
                Leader,
                followers.ToArray(),
                CopyMode.OrderMirror,
                group);

            var accounts = new Dictionary<AccountKey, AccountDescriptor>
            {
                [Leader] = new AccountDescriptor(Leader, "SIM Leader", "Sim", AccountReadiness.Ready, TriState.KnownTrue),
                [Follower1] = new AccountDescriptor(Follower1, "SIM Follower 1", "Sim", followerReady, TriState.KnownTrue)
            };
            foreach (var extra in extraFollowers)
            {
                accounts[extra] = new AccountDescriptor(extra, extra.Value, "Sim", AccountReadiness.Ready, TriState.KnownTrue);
            }

            return new ActiveConfigSnapshot(
                new ConfigVersion(1),
                engine,
                new RiskPolicy(simOnly, false, true),
                new[] { copyGroup },
                accounts);
        }

        public static NormalizedOrderEvent Order(
            string orderId,
            LeaderOrderState state,
            int quantity = 1,
            int filled = 0,
            DomainOrderType type = DomainOrderType.Market,
            OrderActionKind action = OrderActionKind.Buy,
            decimal? limit = null,
            decimal? stop = null,
            string name = "",
            AccountKey? account = null)
        {
            return new NormalizedOrderEvent(
                EventId.New(),
                new DateTime(2026, 8, 16, 14, 0, 0, DateTimeKind.Utc),
                1000,
                account ?? Leader,
                new LeaderOrderKey(orderId),
                Nq,
                action,
                type,
                state,
                quantity,
                filled,
                limit,
                stop,
                "Day",
                string.Empty,
                name);
        }
    }
}
