using System;
using NinjaTrader.Cbi;
using TradeCopia.Domain;
using TradeCopia.Domain.Intents;
using TradeCopia.Native.Adapter;

namespace TradeCopia.Native
{
    /// <summary>
    /// Native submit adapter. Must only be reached through
    /// <see cref="GuardedNativeRuntime"/> after positive SIM classification.
    /// </summary>
    public sealed class NinjaTraderOrderBridge : INativeOrderExecutor
    {
        public NativeExecutionResult Execute(ExecutionIntent intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (intent.Kind == IntentKind.NoOp || intent.Kind == IntentKind.RaiseDivergence)
            {
                return new NativeExecutionResult(true, intent.ReasonCode);
            }

            if (!intent.Follower.HasValue)
            {
                return NativeExecutionResult.Blocked("missing-follower");
            }

            var account = FindAccount(intent.Follower.Value.Value);
            if (account == null)
            {
                return NativeExecutionResult.Blocked("follower-account-not-found");
            }

            var classification = AccountSimulationGate.ClassifyProvider(account.Provider.ToString());
            if (!AccountSimulationGate.AllowsNativeSubmit(classification))
            {
                return NativeExecutionResult.Blocked("simulation-not-positive:" + classification);
            }

            if (intent.Kind != IntentKind.SubmitFollowerOrder)
            {
                return NativeExecutionResult.Blocked("native-intent-not-armed:" + intent.Kind);
            }

            if (!intent.Instrument.HasValue || intent.Quantity <= 0)
            {
                return NativeExecutionResult.Blocked("invalid-quantity-or-instrument");
            }

            var instrument = Instrument.GetInstrument(intent.Instrument.Value.Value, false);
            if (instrument == null)
            {
                return NativeExecutionResult.Blocked("instrument-not-found");
            }

            var name = "TC:" + intent.CommandId.Value.ToString("N");
            if (name.Length > 50)
            {
                name = name.Substring(0, 50);
            }

            var order = account.CreateOrder(
                instrument,
                MapAction(intent.Action),
                MapType(intent.OrderType),
                OrderEntry.Automated,
                TimeInForce.Day,
                intent.Quantity,
                (double)(intent.LimitPrice ?? 0m),
                (double)(intent.StopPrice ?? 0m),
                intent.OcoId ?? string.Empty,
                name,
                DateTime.MaxValue,
                null);
            account.Submit(new[] { order });
            return new NativeExecutionResult(true, "submitted");
        }

        private static OrderAction MapAction(OrderActionKind action)
        {
            switch (action)
            {
                case OrderActionKind.Sell:
                    return OrderAction.Sell;
                case OrderActionKind.BuyToCover:
                    return OrderAction.BuyToCover;
                case OrderActionKind.SellShort:
                    return OrderAction.SellShort;
                default:
                    return OrderAction.Buy;
            }
        }

        private static OrderType MapType(DomainOrderType type)
        {
            switch (type)
            {
                case DomainOrderType.Limit:
                    return OrderType.Limit;
                case DomainOrderType.StopMarket:
                    return OrderType.StopMarket;
                case DomainOrderType.StopLimit:
                    return OrderType.StopLimit;
                case DomainOrderType.Mit:
                    return OrderType.MIT;
                default:
                    return OrderType.Market;
            }
        }

        public static TriState Classify(AccountKey key)
        {
            var account = FindAccount(key.Value);
            if (account == null)
            {
                return TriState.Unknown;
            }

            return AccountSimulationGate.ClassifyProvider(account.Provider.ToString());
        }

        private static Account? FindAccount(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            lock (Account.All)
            {
                foreach (Account account in Account.All)
                {
                    if (account != null
                        && (string.Equals(account.Name, name, StringComparison.Ordinal)
                            || string.Equals(account.DisplayName, name, StringComparison.Ordinal)))
                    {
                        return account;
                    }
                }
            }

            return null;
        }
    }
}
