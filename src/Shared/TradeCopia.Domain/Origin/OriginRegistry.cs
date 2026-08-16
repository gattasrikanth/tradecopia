using System;
using System.Collections.Generic;

namespace TradeCopia.Domain.Origin
{
    public interface IOriginRegistry
    {
        void RegisterPending(CommandId commandId, AccountKey follower, InstrumentKey instrument);
        void Bind(CommandId commandId, FollowerOrderKey followerOrder);
        bool IsCopierOriginated(AccountKey account, string nativeOrderKey);
        bool IsCopierOriginated(FollowerOrderKey followerOrder);
    }

    public sealed class OriginRegistry : IOriginRegistry
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, CommandId> _orders = new Dictionary<string, CommandId>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _pending = new Dictionary<string, string>(StringComparer.Ordinal);

        public void RegisterPending(CommandId commandId, AccountKey follower, InstrumentKey instrument)
        {
            lock (_gate)
            {
                _pending[PendingKey(commandId)] = AccountOrderPrefix(follower);
            }
        }

        public void Bind(CommandId commandId, FollowerOrderKey followerOrder)
        {
            lock (_gate)
            {
                _orders[followerOrder.Value] = commandId;
                _pending.Remove(PendingKey(commandId));
            }
        }

        public bool IsCopierOriginated(AccountKey account, string nativeOrderKey)
        {
            if (string.IsNullOrWhiteSpace(nativeOrderKey))
            {
                return false;
            }

            lock (_gate)
            {
                if (_orders.ContainsKey(nativeOrderKey))
                {
                    return true;
                }

                return nativeOrderKey.StartsWith("TC:", StringComparison.Ordinal);
            }
        }

        public bool IsCopierOriginated(FollowerOrderKey followerOrder)
        {
            lock (_gate)
            {
                return _orders.ContainsKey(followerOrder.Value);
            }
        }

        public static string CorrelationMarker(CommandId commandId)
        {
            var compact = commandId.Value.ToString("N");
            var marker = "TC:" + compact;
            return marker.Length <= 50 ? marker : marker.Substring(0, 50);
        }

        private static string PendingKey(CommandId commandId) => commandId.Value.ToString("D");

        private static string AccountOrderPrefix(AccountKey follower) => follower.Value;
    }
}
