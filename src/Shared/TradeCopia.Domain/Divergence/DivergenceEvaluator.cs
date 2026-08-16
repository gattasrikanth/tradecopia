using System;
using System.Collections.Generic;
using TradeCopia.Domain.Engine;

namespace TradeCopia.Domain.Divergence
{
    public sealed class DivergenceFinding
    {
        public DivergenceFinding(
            DivergenceClass klass,
            Severity severity,
            AccountKey account,
            InstrumentKey? instrument,
            LogicalOrderId? logicalOrder,
            string detail)
        {
            Class = klass;
            Severity = severity;
            Account = account;
            Instrument = instrument;
            LogicalOrder = logicalOrder;
            Detail = detail ?? string.Empty;
        }

        public DivergenceClass Class { get; }
        public Severity Severity { get; }
        public AccountKey Account { get; }
        public InstrumentKey? Instrument { get; }
        public LogicalOrderId? LogicalOrder { get; }
        public string Detail { get; }
    }

    public static class DivergenceEvaluator
    {
        public static IReadOnlyList<DivergenceFinding> Evaluate(LogicalOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var findings = new List<DivergenceFinding>();
            if (order.Links.Count == 0 && order.State == LogicalCopyState.Failed)
            {
                findings.Add(new DivergenceFinding(
                    DivergenceClass.MissingFollowerOrder,
                    Severity.Error,
                    order.LeaderAccount,
                    order.Instrument,
                    order.Id,
                    "No follower links were created for a leader order that required copy."));
            }

            foreach (var link in order.Links)
            {
                if (link.Health == FollowerLinkHealth.Rejected)
                {
                    findings.Add(new DivergenceFinding(
                        DivergenceClass.FollowerRejected,
                        Severity.Error,
                        link.Follower,
                        link.Instrument,
                        order.Id,
                        link.LastError));
                }

                if (link.Health == FollowerLinkHealth.Disconnected)
                {
                    findings.Add(new DivergenceFinding(
                        DivergenceClass.FollowerDisconnected,
                        Severity.Critical,
                        link.Follower,
                        link.Instrument,
                        order.Id,
                        "Follower disconnected while a mapped order is active."));
                }

                if (link.Health == FollowerLinkHealth.Unknown)
                {
                    findings.Add(new DivergenceFinding(
                        DivergenceClass.UnknownNativeOrderState,
                        Severity.Critical,
                        link.Follower,
                        link.Instrument,
                        order.Id,
                        "Follower order state cannot be proven."));
                }
            }

            return findings;
        }
    }
}
