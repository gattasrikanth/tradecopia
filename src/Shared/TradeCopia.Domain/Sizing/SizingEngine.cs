using System;
using TradeCopia.Domain.Model;

namespace TradeCopia.Domain.Sizing
{
    public sealed class SizingResult
    {
        public SizingResult(int quantity, bool blockedByCap, string reason)
        {
            Quantity = quantity;
            BlockedByCap = blockedByCap;
            Reason = reason ?? string.Empty;
        }

        public int Quantity { get; }
        public bool BlockedByCap { get; }
        public string Reason { get; }
        public bool HasQuantity => Quantity > 0 && !BlockedByCap;
    }

    public static class SizingEngine
    {
        public static SizingResult ComputeEntryQuantity(
            int leaderQuantity,
            SizingPolicy policy,
            int currentFollowerPosition)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (leaderQuantity <= 0 || policy.Mode == SizingMode.Disabled)
            {
                return new SizingResult(0, false, "no-entry");
            }

            int raw;
            switch (policy.Mode)
            {
                case SizingMode.OneToOne:
                    raw = leaderQuantity;
                    break;
                case SizingMode.Multiplier:
                    raw = FloorTowardZero(leaderQuantity * policy.Multiplier);
                    if (raw == 0 && policy.MinimumOne)
                    {
                        raw = 1;
                    }

                    break;
                case SizingMode.Fixed:
                    raw = policy.FixedQuantity;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (raw <= 0)
            {
                return new SizingResult(0, false, "rounded-to-zero");
            }

            if (policy.MaxQuantity.HasValue && raw > policy.MaxQuantity.Value)
            {
                return new SizingResult(0, true, "max-quantity");
            }

            if (policy.MaxAbsolutePosition.HasValue)
            {
                var projected = Math.Abs(currentFollowerPosition) + raw;
                if (projected > policy.MaxAbsolutePosition.Value)
                {
                    return new SizingResult(0, true, "max-absolute-position");
                }
            }

            return new SizingResult(raw, false, "ok");
        }

        public static int ComputeScaleOutRemaining(
            int leaderInitialQuantity,
            int leaderRemainingQuantity,
            int followerInitialTarget,
            int followerActualPosition)
        {
            if (followerActualPosition <= 0)
            {
                return 0;
            }

            if (leaderInitialQuantity <= 0)
            {
                return followerActualPosition;
            }

            if (leaderRemainingQuantity < 0)
            {
                leaderRemainingQuantity = 0;
            }

            if (leaderRemainingQuantity > leaderInitialQuantity)
            {
                leaderRemainingQuantity = leaderInitialQuantity;
            }

            var targetRemaining = (int)Math.Floor(
                followerInitialTarget * (double)leaderRemainingQuantity / leaderInitialQuantity);

            if (targetRemaining < 0)
            {
                targetRemaining = 0;
            }

            if (targetRemaining > followerActualPosition)
            {
                return followerActualPosition;
            }

            return targetRemaining;
        }

        public static int ComputeScaleOutReduction(
            int leaderInitialQuantity,
            int leaderRemainingQuantity,
            int followerInitialTarget,
            int followerActualPosition)
        {
            var remaining = ComputeScaleOutRemaining(
                leaderInitialQuantity,
                leaderRemainingQuantity,
                followerInitialTarget,
                followerActualPosition);
            var reduction = followerActualPosition - remaining;
            return reduction < 0 ? 0 : reduction;
        }

        public static int FloorTowardZero(decimal value)
        {
            return value >= 0
                ? (int)Math.Floor(value)
                : (int)Math.Ceiling(value);
        }
    }
}
