using System;
using TradeCopia.Domain.Model;

namespace TradeCopia.Domain.Mapping
{
    public sealed class InstrumentMapResult
    {
        public InstrumentMapResult(bool succeeded, InstrumentKey? instrument, string reason)
        {
            Succeeded = succeeded;
            Instrument = instrument;
            Reason = reason ?? string.Empty;
        }

        public bool Succeeded { get; }
        public InstrumentKey? Instrument { get; }
        public string Reason { get; }

        public static InstrumentMapResult Same(InstrumentKey source)
        {
            return new InstrumentMapResult(true, source, "same-instrument");
        }

        public static InstrumentMapResult Mapped(InstrumentKey target)
        {
            return new InstrumentMapResult(true, target, "explicit-mapping");
        }

        public static InstrumentMapResult Fail(string reason)
        {
            return new InstrumentMapResult(false, null, reason);
        }
    }

    public static class InstrumentMapper
    {
        public static InstrumentMapResult Map(FollowerRule follower, InstrumentKey source)
        {
            if (follower == null)
            {
                throw new ArgumentNullException(nameof(follower));
            }

            if (follower.InstrumentMappings == null || follower.InstrumentMappings.Length == 0)
            {
                return InstrumentMapResult.Same(source);
            }

            foreach (var mapping in follower.InstrumentMappings)
            {
                if (!source.Value.StartsWith(mapping.SourceRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (mapping.ExpiryPolicy != ExpiryMappingPolicy.ExactMonthOnly)
                {
                    return InstrumentMapResult.Fail("expiry-policy-unsupported");
                }

                var suffix = source.Value.Length > mapping.SourceRoot.Length
                    ? source.Value.Substring(mapping.SourceRoot.Length)
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(suffix))
                {
                    return InstrumentMapResult.Fail("expiry-unresolved");
                }

                return InstrumentMapResult.Mapped(new InstrumentKey(mapping.TargetRoot + suffix));
            }

            return InstrumentMapResult.Same(source);
        }
    }
}
