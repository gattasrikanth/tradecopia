using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TradeCopia.Protocol
{
    public sealed class LiveFollowerRecord
    {
        public LiveFollowerRecord(string account, int qty, int filled, string state, string fill, string orderName)
        {
            Account = account ?? string.Empty;
            Qty = qty;
            Filled = filled;
            State = state ?? string.Empty;
            Fill = fill ?? string.Empty;
            OrderName = orderName ?? string.Empty;
        }

        public string Account { get; }
        public int Qty { get; }
        public int Filled { get; }
        public string State { get; }
        public string Fill { get; }
        public string OrderName { get; }

        public string ToJson()
        {
            return "{\"account\":" + LiveCopyRecord.Quote(Account)
                + ",\"qty\":" + Qty.ToString(CultureInfo.InvariantCulture)
                + ",\"filled\":" + Filled.ToString(CultureInfo.InvariantCulture)
                + ",\"state\":" + LiveCopyRecord.Quote(State)
                + ",\"fill\":" + LiveCopyRecord.Quote(Fill)
                + ",\"orderName\":" + LiveCopyRecord.Quote(OrderName) + "}";
        }
    }

    public sealed class LiveCopyRecord
    {
        public LiveCopyRecord(
            string id,
            string instrument,
            string side,
            string orderType,
            string leaderAccount,
            string leaderOrderKey,
            int leaderQty,
            int leaderFilled,
            string leaderState,
            string limitPrice,
            string stopPrice,
            DateTime updatedAtUtc,
            IReadOnlyList<LiveFollowerRecord> followers)
        {
            Id = id ?? string.Empty;
            Instrument = instrument ?? string.Empty;
            Side = side ?? string.Empty;
            OrderType = orderType ?? string.Empty;
            LeaderAccount = leaderAccount ?? string.Empty;
            LeaderOrderKey = leaderOrderKey ?? string.Empty;
            LeaderQty = leaderQty;
            LeaderFilled = leaderFilled;
            LeaderState = leaderState ?? string.Empty;
            LimitPrice = limitPrice ?? string.Empty;
            StopPrice = stopPrice ?? string.Empty;
            UpdatedAtUtc = DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc);
            Followers = followers ?? Array.Empty<LiveFollowerRecord>();
        }

        public string Id { get; }
        public string Instrument { get; }
        public string Side { get; }
        public string OrderType { get; }
        public string LeaderAccount { get; }
        public string LeaderOrderKey { get; }
        public int LeaderQty { get; }
        public int LeaderFilled { get; }
        public string LeaderState { get; }
        public string LimitPrice { get; }
        public string StopPrice { get; }
        public DateTime UpdatedAtUtc { get; }
        public IReadOnlyList<LiveFollowerRecord> Followers { get; }

        public string ToJson()
        {
            var followers = new StringBuilder();
            followers.Append('[');
            for (var i = 0; i < Followers.Count; i++)
            {
                if (i > 0)
                {
                    followers.Append(',');
                }

                followers.Append(Followers[i].ToJson());
            }

            followers.Append(']');
            return "{\"id\":" + Quote(Id)
                + ",\"instrument\":" + Quote(Instrument)
                + ",\"side\":" + Quote(Side)
                + ",\"orderType\":" + Quote(OrderType)
                + ",\"leaderAccount\":" + Quote(LeaderAccount)
                + ",\"leaderOrderKey\":" + Quote(LeaderOrderKey)
                + ",\"leaderQty\":" + LeaderQty.ToString(CultureInfo.InvariantCulture)
                + ",\"leaderFilled\":" + LeaderFilled.ToString(CultureInfo.InvariantCulture)
                + ",\"leaderState\":" + Quote(LeaderState)
                + ",\"limitPrice\":" + Quote(LimitPrice)
                + ",\"stopPrice\":" + Quote(StopPrice)
                + ",\"updatedAtUtc\":" + Quote(UpdatedAtUtc.ToString("o", CultureInfo.InvariantCulture))
                + ",\"followers\":" + followers + "}";
        }

        internal static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        public static IReadOnlyList<LiveCopyRecord> ParseArray(string payload)
        {
            var list = new List<LiveCopyRecord>();
            var inner = ExtractArrayInner(payload, "liveTrades");
            if (string.IsNullOrEmpty(inner))
            {
                return list;
            }

            foreach (var obj in SplitObjects(inner))
            {
                var id = ReadString(obj, "id");
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var followers = new List<LiveFollowerRecord>();
                var followerInner = ExtractArrayInner(obj, "followers");
                foreach (var f in SplitObjects(followerInner))
                {
                    var account = ReadString(f, "account");
                    if (string.IsNullOrEmpty(account))
                    {
                        continue;
                    }

                    followers.Add(new LiveFollowerRecord(
                        account,
                        ReadInt(f, "qty"),
                        ReadInt(f, "filled"),
                        ReadString(f, "state"),
                        ReadString(f, "fill"),
                        ReadString(f, "orderName")));
                }

                DateTime updated;
                if (!DateTime.TryParse(ReadString(obj, "updatedAtUtc"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out updated))
                {
                    updated = DateTime.UtcNow;
                }

                list.Add(new LiveCopyRecord(
                    id,
                    ReadString(obj, "instrument"),
                    ReadString(obj, "side"),
                    ReadString(obj, "orderType"),
                    ReadString(obj, "leaderAccount"),
                    ReadString(obj, "leaderOrderKey"),
                    ReadInt(obj, "leaderQty"),
                    ReadInt(obj, "leaderFilled"),
                    ReadString(obj, "leaderState"),
                    ReadString(obj, "limitPrice"),
                    ReadString(obj, "stopPrice"),
                    updated.ToUniversalTime(),
                    followers));
            }

            return list;
        }

        public static IReadOnlyList<LiveDivergenceRecord> ParseDivergences(string payload)
        {
            var list = new List<LiveDivergenceRecord>();
            foreach (var obj in SplitObjects(ExtractArrayInner(payload, "liveDivergences")))
            {
                var detail = ReadString(obj, "detail");
                if (string.IsNullOrEmpty(detail))
                {
                    continue;
                }

                list.Add(new LiveDivergenceRecord(ReadString(obj, "className"), detail));
            }

            return list;
        }

        internal static string ExtractArrayInner(string payload, string name)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var token = "\"" + name + "\":";
            var i = payload.IndexOf(token, StringComparison.Ordinal);
            if (i < 0)
            {
                return string.Empty;
            }

            i = payload.IndexOf('[', i);
            if (i < 0)
            {
                return string.Empty;
            }

            var end = MatchBracket(payload, i, '[', ']');
            if (end < 0 || end - i < 2)
            {
                return string.Empty;
            }

            return payload.Substring(i + 1, end - i - 1);
        }

        internal static IEnumerable<string> SplitObjects(string inner)
        {
            if (string.IsNullOrEmpty(inner))
            {
                yield break;
            }

            var start = -1;
            var depth = 0;
            var inString = false;
            var escape = false;
            for (var i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                    }

                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        yield return inner.Substring(start, i - start + 1);
                        start = -1;
                    }
                }
            }
        }

        internal static int MatchBracket(string text, int openIndex, char open, char close)
        {
            var depth = 0;
            var inString = false;
            var escape = false;
            for (var i = openIndex; i < text.Length; i++)
            {
                var c = text[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == open)
                {
                    depth++;
                }
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        internal static string ReadString(string json, string name)
        {
            var token = "\"" + name + "\":\"";
            var i = json.IndexOf(token, StringComparison.Ordinal);
            if (i < 0)
            {
                return string.Empty;
            }

            i += token.Length;
            var sb = new StringBuilder();
            for (var n = i; n < json.Length; n++)
            {
                var c = json[n];
                if (c == '\\' && n + 1 < json.Length)
                {
                    sb.Append(json[n + 1]);
                    n++;
                    continue;
                }

                if (c == '"')
                {
                    break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        internal static int ReadInt(string json, string name)
        {
            var token = "\"" + name + "\":";
            var i = json.IndexOf(token, StringComparison.Ordinal);
            if (i < 0)
            {
                return 0;
            }

            i += token.Length;
            var end = i;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }

            int value;
            return int.TryParse(json.Substring(i, end - i), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }
    }

    public sealed class LiveDivergenceRecord
    {
        public LiveDivergenceRecord(string className, string detail)
        {
            ClassName = className ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string ClassName { get; }
        public string Detail { get; }

        public string ToJson()
        {
            return "{\"className\":" + LiveCopyRecord.Quote(ClassName)
                + ",\"detail\":" + LiveCopyRecord.Quote(Detail) + "}";
        }
    }
}
