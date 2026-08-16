using System;
using System.Collections.Generic;
using System.Text;
using TradeCopia.Domain.Safety;

namespace TradeCopia.Protocol
{
    public sealed class EngineAccountRecord
    {
        public EngineAccountRecord(
            string stableKey,
            string displayName,
            string provider,
            string officialMode,
            bool isDemo,
            AccountSafetyClass safetyClass)
        {
            StableKey = stableKey ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Provider = provider ?? string.Empty;
            OfficialMode = officialMode ?? string.Empty;
            IsDemo = isDemo;
            SafetyClass = safetyClass;
        }

        public string StableKey { get; }
        public string DisplayName { get; }
        public string Provider { get; }
        public string OfficialMode { get; }
        public bool IsDemo { get; }
        public AccountSafetyClass SafetyClass { get; }
        public bool Selectable => AccountSafetyClassifier.AlphaMaySelect(SafetyClass);

        public string ToJson()
        {
            return "{\"stableKey\":" + Quote(StableKey)
                + ",\"displayName\":" + Quote(DisplayName)
                + ",\"provider\":" + Quote(Provider)
                + ",\"officialMode\":" + Quote(OfficialMode)
                + ",\"isDemo\":" + (IsDemo ? "true" : "false")
                + ",\"safetyClass\":" + Quote(SafetyClass.ToString())
                + ",\"selectable\":" + (Selectable ? "true" : "false") + "}";
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        public static IReadOnlyList<EngineAccountRecord> ParseArray(string payload)
        {
            var list = new List<EngineAccountRecord>();
            if (string.IsNullOrEmpty(payload))
            {
                return list;
            }

            var start = payload.IndexOf("\"accounts\":[", StringComparison.Ordinal);
            if (start < 0)
            {
                return list;
            }

            start = payload.IndexOf('[', start);
            var end = payload.IndexOf(']', start);
            if (start < 0 || end < 0)
            {
                return list;
            }

            var inner = payload.Substring(start + 1, end - start - 1);
            var objects = inner.Split(new[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in objects)
            {
                var obj = raw.Trim();
                if (obj.Length == 0)
                {
                    continue;
                }

                if (!obj.StartsWith("{", StringComparison.Ordinal))
                {
                    obj = "{" + obj;
                }

                if (!obj.EndsWith("}", StringComparison.Ordinal))
                {
                    obj += "}";
                }

                var key = ReadJsonString(obj, "stableKey");
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var safetyText = ReadJsonString(obj, "safetyClass");
                AccountSafetyClass safety;
                if (!Enum.TryParse(safetyText, true, out safety))
                {
                    safety = AccountSafetyClass.Unknown;
                }

                list.Add(new EngineAccountRecord(
                    key,
                    ReadJsonString(obj, "displayName"),
                    ReadJsonString(obj, "provider"),
                    ReadJsonString(obj, "officialMode"),
                    obj.IndexOf("\"isDemo\":true", StringComparison.Ordinal) >= 0,
                    safety));
            }

            return list;
        }

        private static string ReadJsonString(string json, string name)
        {
            var token = "\"" + name + "\":\"";
            var i = json.IndexOf(token, StringComparison.Ordinal);
            if (i < 0)
            {
                return string.Empty;
            }

            i += token.Length;
            var end = json.IndexOf('"', i);
            return end < 0 ? string.Empty : json.Substring(i, end - i);
        }
    }
}
