using System.Collections.Generic;
using TradeCopia.Domain.Events;

namespace TradeCopia.Domain.Engine
{
    public sealed class FollowerLink
    {
        public FollowerLink(AccountKey follower, int intendedQuantity, InstrumentKey instrument)
        {
            Follower = follower;
            IntendedQuantity = intendedQuantity;
            Instrument = instrument;
            Health = FollowerLinkHealth.Pending;
            LastError = string.Empty;
        }

        public AccountKey Follower { get; }
        public InstrumentKey Instrument { get; }
        public int IntendedQuantity { get; set; }
        public int SubmittedQuantity { get; set; }
        public int FilledQuantity { get; set; }
        public FollowerOrderKey? NativeOrder { get; set; }
        public CommandId? SubmitCommand { get; set; }
        public FollowerLinkHealth Health { get; set; }
        public string LastError { get; set; }
    }

    public sealed class LogicalOrder
    {
        public LogicalOrder(
            LogicalOrderId id,
            CopyGroupId groupId,
            NormalizedOrderEvent first)
        {
            Id = id;
            GroupId = groupId;
            LeaderOrder = first.OrderKey;
            LeaderAccount = first.Account;
            Instrument = first.Instrument;
            Action = first.Action;
            OrderType = first.OrderType;
            RequestedQuantity = first.Quantity;
            InitialQuantity = first.Quantity;
            FilledQuantity = first.FilledQuantity;
            LimitPrice = first.LimitPrice;
            StopPrice = first.StopPrice;
            TimeInForce = first.TimeInForce;
            LeaderOco = first.OcoIdentity;
            State = LogicalCopyState.Discovered;
            FirstObservedAtUtc = first.ObservedAtUtc;
            LastObservedAtUtc = first.ObservedAtUtc;
            LastFingerprint = string.Empty;
            Links = new List<FollowerLink>();
        }

        public LogicalOrderId Id { get; }
        public CopyGroupId GroupId { get; }
        public LeaderOrderKey LeaderOrder { get; }
        public AccountKey LeaderAccount { get; }
        public InstrumentKey Instrument { get; }
        public OrderActionKind Action { get; }
        public DomainOrderType OrderType { get; }
        public int RequestedQuantity { get; set; }
        public int InitialQuantity { get; }
        public int FilledQuantity { get; set; }
        public decimal? LimitPrice { get; set; }
        public decimal? StopPrice { get; set; }
        public string TimeInForce { get; set; }
        public string LeaderOco { get; set; }
        public LogicalCopyState State { get; set; }
        public System.DateTime FirstObservedAtUtc { get; }
        public System.DateTime LastObservedAtUtc { get; set; }
        public string LastFingerprint { get; set; }
        public LeaderOrderState LastLeaderState { get; set; }
        public List<FollowerLink> Links { get; }
        public IntentClassification Classification { get; set; }
    }
}
