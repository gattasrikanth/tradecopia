using System.Globalization;
using System.Text;
using TradeCopia.Domain.Events;

namespace TradeCopia.Domain.Fingerprints
{
    public static class SemanticFingerprint
    {
        public static string Compute(NormalizedOrderEvent evt)
        {
            var builder = new StringBuilder(128);
            builder.Append(evt.OrderKey.Value).Append('|');
            builder.Append((int)evt.State).Append('|');
            builder.Append((int)evt.OrderType).Append('|');
            builder.Append((int)evt.Action).Append('|');
            builder.Append(evt.Quantity).Append('|');
            builder.Append(evt.FilledQuantity).Append('|');
            builder.Append(FormatPrice(evt.LimitPrice)).Append('|');
            builder.Append(FormatPrice(evt.StopPrice)).Append('|');
            builder.Append(evt.TimeInForce).Append('|');
            builder.Append(evt.OcoIdentity);
            return builder.ToString();
        }

        private static string FormatPrice(decimal? price)
        {
            return price.HasValue
                ? price.Value.ToString("0.########", CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}
