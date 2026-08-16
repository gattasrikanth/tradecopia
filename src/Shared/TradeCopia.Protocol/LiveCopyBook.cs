using System;
using System.Collections.Generic;
using System.Globalization;

namespace TradeCopia.Protocol
{
    /// <summary>
    /// In-memory live tape. Mutated on the native order thread; snapshots
    /// are copied for the 1s companion poll. Never submits orders.
    /// </summary>
    public sealed class LiveCopyBook
    {
        public const int Capacity = 64;

        private readonly List<MutableRow> _rows = new List<MutableRow>();
        private readonly Dictionary<string, MutableRow> _byKey = new Dictionary<string, MutableRow>(StringComparer.Ordinal);
        private readonly List<LiveDivergenceRecord> _divergences = new List<LiveDivergenceRecord>();

        public void Observe(
            string orderKey,
            string alternateKey,
            string account,
            string instrument,
            string side,
            string orderType,
            int quantity,
            int filled,
            string state,
            string limitPrice,
            string stopPrice,
            string orderName,
            bool copierOriginated,
            DateTime utc)
        {
            if (string.IsNullOrEmpty(orderKey) && string.IsNullOrEmpty(alternateKey))
            {
                return;
            }

            if (copierOriginated)
            {
                ObserveFollower(orderKey, alternateKey, account, instrument, side, orderType, quantity, filled, state, limitPrice, stopPrice, orderName, utc);
                return;
            }

            var row = FindByKey(orderKey) ?? FindByKey(alternateKey);
            if (row == null)
            {
                row = new MutableRow
                {
                    Id = Guid.NewGuid().ToString("N"),
                    LeaderOrderKey = orderKey ?? alternateKey ?? string.Empty
                };
                _rows.Add(row);
                Trim();
            }

            Index(orderKey ?? string.Empty, row);
            Index(alternateKey ?? string.Empty, row);
            row.Instrument = instrument ?? string.Empty;
            row.Side = side ?? string.Empty;
            row.OrderType = orderType ?? string.Empty;
            row.LeaderAccount = account ?? string.Empty;
            row.LeaderQty = quantity;
            row.LeaderFilled = filled;
            row.LeaderState = state ?? string.Empty;
            row.LimitPrice = limitPrice ?? string.Empty;
            row.StopPrice = stopPrice ?? string.Empty;
            row.UpdatedAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        public void AddDivergence(string className, string detail)
        {
            if (string.IsNullOrEmpty(detail))
            {
                return;
            }

            _divergences.Add(new LiveDivergenceRecord(className, detail));
            if (_divergences.Count > 32)
            {
                _divergences.RemoveAt(0);
            }
        }

        public IReadOnlyList<LiveCopyRecord> Snapshot()
        {
            var list = new List<LiveCopyRecord>(_rows.Count);
            for (var i = 0; i < _rows.Count; i++)
            {
                list.Add(_rows[i].Freeze());
            }

            return list;
        }

        public IReadOnlyList<LiveDivergenceRecord> DivergenceSnapshot()
        {
            return new List<LiveDivergenceRecord>(_divergences);
        }

        private void ObserveFollower(
            string orderKey,
            string alternateKey,
            string account,
            string instrument,
            string side,
            string orderType,
            int quantity,
            int filled,
            string state,
            string limitPrice,
            string stopPrice,
            string orderName,
            DateTime utc)
        {
            var row = FindMatchingLeader(instrument, side, orderType, quantity, limitPrice, stopPrice);
            if (row == null)
            {
                row = new MutableRow
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Instrument = instrument ?? string.Empty,
                    Side = side ?? string.Empty,
                    OrderType = orderType ?? string.Empty,
                    LeaderAccount = string.Empty,
                    LeaderOrderKey = string.Empty,
                    LeaderQty = quantity,
                    LimitPrice = limitPrice ?? string.Empty,
                    StopPrice = stopPrice ?? string.Empty
                };
                _rows.Add(row);
                Trim();
            }

            var followerKey = !string.IsNullOrEmpty(orderName) ? orderName : (orderKey ?? alternateKey ?? string.Empty);
            MutableFollower? found = null;
            for (var i = 0; i < row.Followers.Count; i++)
            {
                if (string.Equals(row.Followers[i].OrderName, followerKey, StringComparison.Ordinal)
                    || string.Equals(row.Followers[i].Account, account, StringComparison.Ordinal)
                    && string.Equals(row.Followers[i].OrderName, orderName, StringComparison.Ordinal))
                {
                    found = row.Followers[i];
                    break;
                }
            }

            if (found == null)
            {
                found = new MutableFollower();
                row.Followers.Add(found);
            }

            found.Account = account ?? string.Empty;
            found.Qty = quantity;
            found.Filled = filled;
            found.State = state ?? string.Empty;
            found.Fill = !string.IsNullOrEmpty(limitPrice)
                ? limitPrice!
                : (!string.IsNullOrEmpty(stopPrice) ? stopPrice! : string.Empty);
            found.OrderName = followerKey;
            row.UpdatedAtUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        private MutableRow? FindMatchingLeader(string instrument, string side, string orderType, int quantity, string limitPrice, string stopPrice)
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                var row = _rows[i];
                if (string.Equals(row.Instrument, instrument, StringComparison.Ordinal)
                    && string.Equals(row.Side, side, StringComparison.Ordinal)
                    && string.Equals(row.OrderType, orderType, StringComparison.Ordinal)
                    && row.LeaderQty == quantity
                    && string.Equals(row.LimitPrice, limitPrice ?? string.Empty, StringComparison.Ordinal)
                    && string.Equals(row.StopPrice, stopPrice ?? string.Empty, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(row.LeaderAccount))
                {
                    return row;
                }
            }

            return null;
        }

        private MutableRow? FindByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            MutableRow row;
            return _byKey.TryGetValue(key, out row) ? row : null;
        }

        private void Index(string key, MutableRow row)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _byKey[key] = row;
            }
        }

        private void Trim()
        {
            while (_rows.Count > Capacity)
            {
                var oldest = _rows[0];
                _rows.RemoveAt(0);
                var remove = new List<string>();
                foreach (var pair in _byKey)
                {
                    if (ReferenceEquals(pair.Value, oldest))
                    {
                        remove.Add(pair.Key);
                    }
                }

                for (var i = 0; i < remove.Count; i++)
                {
                    _byKey.Remove(remove[i]);
                }
            }
        }

        private sealed class MutableRow
        {
            public string Id = string.Empty;
            public string Instrument = string.Empty;
            public string Side = string.Empty;
            public string OrderType = string.Empty;
            public string LeaderAccount = string.Empty;
            public string LeaderOrderKey = string.Empty;
            public int LeaderQty;
            public int LeaderFilled;
            public string LeaderState = string.Empty;
            public string LimitPrice = string.Empty;
            public string StopPrice = string.Empty;
            public DateTime UpdatedAtUtc = DateTime.UtcNow;
            public readonly List<MutableFollower> Followers = new List<MutableFollower>();

            public LiveCopyRecord Freeze()
            {
                var followers = new LiveFollowerRecord[Followers.Count];
                for (var i = 0; i < Followers.Count; i++)
                {
                    var f = Followers[i];
                    followers[i] = new LiveFollowerRecord(f.Account, f.Qty, f.Filled, f.State, f.Fill, f.OrderName);
                }

                return new LiveCopyRecord(
                    Id,
                    Instrument,
                    Side,
                    OrderType,
                    LeaderAccount,
                    LeaderOrderKey,
                    LeaderQty,
                    LeaderFilled,
                    LeaderState,
                    LimitPrice,
                    StopPrice,
                    UpdatedAtUtc,
                    followers);
            }
        }

        private sealed class MutableFollower
        {
            public string Account = string.Empty;
            public int Qty;
            public int Filled;
            public string State = string.Empty;
            public string Fill = string.Empty;
            public string OrderName = string.Empty;
        }

        public static string FormatPrice(decimal? price)
        {
            return price.HasValue ? price.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }
    }
}
