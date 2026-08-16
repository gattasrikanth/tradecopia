using System;
using NinjaTrader.Cbi;
using TradeCopia.Domain;
using TradeCopia.Domain.Events;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Native
{
    public static class NinjaTraderOrderNormalizer
    {
        public static NormalizedOrderEvent? Capture(Account account, Order order)
        {
            if (account == null || order == null || order.Instrument == null)
            {
                return null;
            }

            var key = NativeAccountIdentity.StableKey(account.Provider.ToString(), account.Id.ToString());
            var instrumentName = order.Instrument.FullName;
            if (string.IsNullOrWhiteSpace(instrumentName))
            {
                return null;
            }

            var sessionId = order.Id.ToString();
            var brokerId = order.OrderId ?? string.Empty;
            var primary = !string.IsNullOrWhiteSpace(sessionId) && sessionId != "0"
                ? sessionId
                : brokerId;
            if (string.IsNullOrWhiteSpace(primary))
            {
                return null;
            }

            var alternate = !string.IsNullOrWhiteSpace(brokerId)
                && !string.Equals(brokerId, primary, StringComparison.Ordinal)
                ? brokerId
                : string.Empty;

            return new NormalizedOrderEvent(
                EventId.New(),
                DateTime.UtcNow,
                DateTime.UtcNow.Ticks,
                new AccountKey(key),
                new LeaderOrderKey(primary),
                new InstrumentKey(instrumentName),
                MapAction(order.OrderAction),
                MapType(order.OrderType),
                MapState(order.OrderState),
                order.Quantity,
                order.Filled,
                order.LimitPrice != 0 ? (decimal)order.LimitPrice : (decimal?)null,
                order.StopPrice != 0 ? (decimal)order.StopPrice : (decimal?)null,
                order.TimeInForce.ToString(),
                order.Oco ?? string.Empty,
                order.Name ?? string.Empty,
                alternate);
        }

        internal static OrderActionKind MapAction(OrderAction action)
        {
            if (action == OrderAction.Sell)
            {
                return OrderActionKind.Sell;
            }

            if (action == OrderAction.BuyToCover)
            {
                return OrderActionKind.BuyToCover;
            }

            if (action == OrderAction.SellShort)
            {
                return OrderActionKind.SellShort;
            }

            return OrderActionKind.Buy;
        }

        internal static DomainOrderType MapType(OrderType type)
        {
            if (type == OrderType.Limit)
            {
                return DomainOrderType.Limit;
            }

            if (type == OrderType.StopMarket)
            {
                return DomainOrderType.StopMarket;
            }

            if (type == OrderType.StopLimit)
            {
                return DomainOrderType.StopLimit;
            }

            if (type == OrderType.MIT)
            {
                return DomainOrderType.Mit;
            }

            if (type == OrderType.Market)
            {
                return DomainOrderType.Market;
            }

            return DomainOrderType.Unsupported;
        }

        internal static LeaderOrderState MapState(OrderState state)
        {
            if (state == OrderState.Submitted || state == OrderState.Initialized)
            {
                return LeaderOrderState.PendingSubmission;
            }

            if (state == OrderState.Working
                || state == OrderState.Accepted
                || state == OrderState.AcceptedByRisk
                || state == OrderState.TriggerPending)
            {
                return LeaderOrderState.Working;
            }

            if (state == OrderState.PartFilled)
            {
                return LeaderOrderState.PartiallyFilled;
            }

            if (state == OrderState.Filled)
            {
                return LeaderOrderState.Filled;
            }

            if (state == OrderState.CancelPending || state == OrderState.CancelSubmitted)
            {
                return LeaderOrderState.CancelPending;
            }

            if (state == OrderState.Cancelled)
            {
                return LeaderOrderState.Canceled;
            }

            if (state == OrderState.ChangePending || state == OrderState.ChangeSubmitted)
            {
                return LeaderOrderState.ChangePending;
            }

            if (state == OrderState.Rejected)
            {
                return LeaderOrderState.Rejected;
            }

            return LeaderOrderState.Observed;
        }
    }
}
