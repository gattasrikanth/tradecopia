using System;

namespace TradeCopia.Domain.Intents
{
    public sealed class ExecutionIntent
    {
        public ExecutionIntent(
            CommandId commandId,
            EventId sourceEventId,
            IntentKind kind,
            CopyGroupId? groupId,
            AccountKey? follower,
            LogicalOrderId? logicalOrderId,
            InstrumentKey? instrument,
            DomainOrderType orderType,
            OrderActionKind action,
            int quantity,
            decimal? limitPrice,
            decimal? stopPrice,
            string ocoId,
            string reasonCode,
            DateTime createdAtUtc)
        {
            CommandId = commandId;
            SourceEventId = sourceEventId;
            Kind = kind;
            GroupId = groupId;
            Follower = follower;
            LogicalOrderId = logicalOrderId;
            Instrument = instrument;
            OrderType = orderType;
            Action = action;
            Quantity = quantity;
            LimitPrice = limitPrice;
            StopPrice = stopPrice;
            OcoId = ocoId ?? string.Empty;
            ReasonCode = reasonCode ?? string.Empty;
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        }

        public CommandId CommandId { get; }
        public EventId SourceEventId { get; }
        public IntentKind Kind { get; }
        public CopyGroupId? GroupId { get; }
        public AccountKey? Follower { get; }
        public LogicalOrderId? LogicalOrderId { get; }
        public InstrumentKey? Instrument { get; }
        public DomainOrderType OrderType { get; }
        public OrderActionKind Action { get; }
        public int Quantity { get; }
        public decimal? LimitPrice { get; }
        public decimal? StopPrice { get; }
        public string OcoId { get; }
        public string ReasonCode { get; }
        public DateTime CreatedAtUtc { get; }

        public static ExecutionIntent NoOp(EventId source, DateTime utc, string reason)
        {
            return new ExecutionIntent(
                CommandId.New(),
                source,
                IntentKind.NoOp,
                null,
                null,
                null,
                null,
                DomainOrderType.Market,
                OrderActionKind.Buy,
                0,
                null,
                null,
                string.Empty,
                reason,
                utc);
        }
    }
}
