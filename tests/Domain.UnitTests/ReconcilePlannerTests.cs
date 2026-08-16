using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Reconcile;

namespace TradeCopia.Domain.UnitTests
{
    public class ReconcilePlannerTests
    {
        [Fact]
        public void Stale_hash_or_config_cannot_execute()
        {
            var now = new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc);
            var plan = ReconcilePlanner.Preview(
                new ConfigVersion(2),
                "hash-a",
                new[] { new ReconcileAction("cancel leftover working order", false) },
                now);

            Assert.False(ReconcilePlanner.CanExecute(
                plan,
                new ReconcileExecutionRequest(plan.PlanId, new ConfigVersion(3), "hash-a"),
                now));
            Assert.False(ReconcilePlanner.CanExecute(
                plan,
                new ReconcileExecutionRequest(plan.PlanId, new ConfigVersion(2), "hash-b"),
                now));
            Assert.True(ReconcilePlanner.CanExecute(
                plan,
                new ReconcileExecutionRequest(plan.PlanId, new ConfigVersion(2), "hash-a"),
                now));
        }

        [Fact]
        public void Expired_plan_is_rejected()
        {
            var now = new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc);
            var plan = ReconcilePlanner.Preview(
                new ConfigVersion(1),
                "h",
                new[] { new ReconcileAction("flatten extra follower qty", false) },
                now);
            Assert.False(ReconcilePlanner.CanExecute(
                plan,
                new ReconcileExecutionRequest(plan.PlanId, plan.ConfigVersion, plan.ObservedStateHash),
                now.AddMinutes(3)));
        }
    }
}
