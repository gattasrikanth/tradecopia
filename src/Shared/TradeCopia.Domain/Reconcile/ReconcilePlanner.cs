using System;
using System.Collections.Generic;

namespace TradeCopia.Domain.Reconcile
{
    public enum ReconcileRiskLevel
    {
        None = 0,
        ReduceOnly = 1,
        ExposureIncreasing = 2,
        Ambiguous = 3
    }

    public sealed class ReconcileAction
    {
        public ReconcileAction(string description, bool increasesExposure)
        {
            Description = description ?? string.Empty;
            IncreasesExposure = increasesExposure;
        }

        public string Description { get; }
        public bool IncreasesExposure { get; }
    }

    public sealed class ReconcilePlan
    {
        public ReconcilePlan(
            Guid planId,
            ConfigVersion configVersion,
            string observedStateHash,
            DateTime generatedAtUtc,
            DateTime expiresAtUtc,
            ReconcileRiskLevel risk,
            IReadOnlyList<ReconcileAction> actions,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> unresolvable)
        {
            PlanId = planId;
            ConfigVersion = configVersion;
            ObservedStateHash = observedStateHash ?? string.Empty;
            GeneratedAtUtc = DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc);
            ExpiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
            Risk = risk;
            Actions = actions ?? Array.Empty<ReconcileAction>();
            Warnings = warnings ?? Array.Empty<string>();
            Unresolvable = unresolvable ?? Array.Empty<string>();
        }

        public Guid PlanId { get; }
        public ConfigVersion ConfigVersion { get; }
        public string ObservedStateHash { get; }
        public DateTime GeneratedAtUtc { get; }
        public DateTime ExpiresAtUtc { get; }
        public ReconcileRiskLevel Risk { get; }
        public IReadOnlyList<ReconcileAction> Actions { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<string> Unresolvable { get; }
    }

    public sealed class ReconcileExecutionRequest
    {
        public ReconcileExecutionRequest(Guid planId, ConfigVersion configVersion, string observedStateHash)
        {
            PlanId = planId;
            ConfigVersion = configVersion;
            ObservedStateHash = observedStateHash ?? string.Empty;
        }

        public Guid PlanId { get; }
        public ConfigVersion ConfigVersion { get; }
        public string ObservedStateHash { get; }
    }

    public static class ReconcilePlanner
    {
        public static ReconcilePlan Preview(
            ConfigVersion configVersion,
            string observedStateHash,
            IReadOnlyList<ReconcileAction> proposed,
            DateTime utcNow)
        {
            proposed = proposed ?? Array.Empty<ReconcileAction>();
            var warnings = new List<string>();
            var unresolvable = new List<string>();
            var risk = ReconcileRiskLevel.None;

            foreach (var action in proposed)
            {
                if (action.IncreasesExposure)
                {
                    risk = ReconcileRiskLevel.ExposureIncreasing;
                    warnings.Add("Exposure-increasing reconcile requires explicit confirmation.");
                }
                else if (risk == ReconcileRiskLevel.None)
                {
                    risk = ReconcileRiskLevel.ReduceOnly;
                }
            }

            return new ReconcilePlan(
                Guid.NewGuid(),
                configVersion,
                observedStateHash,
                utcNow,
                utcNow.AddMinutes(2),
                risk,
                proposed,
                warnings,
                unresolvable);
        }

        public static bool CanExecute(ReconcilePlan plan, ReconcileExecutionRequest request, DateTime utcNow)
        {
            if (plan == null || request == null)
            {
                return false;
            }

            if (plan.PlanId != request.PlanId)
            {
                return false;
            }

            if (plan.ConfigVersion != request.ConfigVersion)
            {
                return false;
            }

            if (!string.Equals(plan.ObservedStateHash, request.ObservedStateHash, StringComparison.Ordinal))
            {
                return false;
            }

            if (utcNow > plan.ExpiresAtUtc)
            {
                return false;
            }

            return plan.Risk != ReconcileRiskLevel.Ambiguous;
        }
    }
}
