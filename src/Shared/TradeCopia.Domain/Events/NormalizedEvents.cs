using System;

namespace TradeCopia.Domain.Events
{
    public sealed class NormalizedOrderEvent
    {
        public NormalizedOrderEvent(
            EventId eventId,
            DateTime observedAtUtc,
            long observedHighResTicks,
            AccountKey account,
            LeaderOrderKey orderKey,
            InstrumentKey instrument,
            OrderActionKind action,
            DomainOrderType orderType,
            LeaderOrderState state,
            int quantity,
            int filledQuantity,
            decimal? limitPrice,
            decimal? stopPrice,
            string timeInForce,
            string ocoIdentity,
            string orderName,
            string alternateOrderKey = "")
        {
            EventId = eventId;
            ObservedAtUtc = DateTime.SpecifyKind(observedAtUtc, DateTimeKind.Utc);
            ObservedHighResTicks = observedHighResTicks;
            Account = account;
            OrderKey = orderKey;
            Instrument = instrument;
            Action = action;
            OrderType = orderType;
            State = state;
            Quantity = quantity;
            FilledQuantity = filledQuantity;
            LimitPrice = limitPrice;
            StopPrice = stopPrice;
            TimeInForce = timeInForce ?? string.Empty;
            OcoIdentity = ocoIdentity ?? string.Empty;
            OrderName = orderName ?? string.Empty;
            AlternateOrderKey = alternateOrderKey ?? string.Empty;
        }

        public EventId EventId { get; }
        public DateTime ObservedAtUtc { get; }
        public long ObservedHighResTicks { get; }
        public AccountKey Account { get; }
        public LeaderOrderKey OrderKey { get; }
        public InstrumentKey Instrument { get; }
        public OrderActionKind Action { get; }
        public DomainOrderType OrderType { get; }
        public LeaderOrderState State { get; }
        public int Quantity { get; }
        public int FilledQuantity { get; }
        public decimal? LimitPrice { get; }
        public decimal? StopPrice { get; }
        public string TimeInForce { get; }
        public string OcoIdentity { get; }
        public string OrderName { get; }
        public string AlternateOrderKey { get; }

        public int WorkingQuantity
        {
            get
            {
                var remaining = Quantity - FilledQuantity;
                return remaining > 0 ? remaining : 0;
            }
        }

        public bool IsTerminal =>
            State == LeaderOrderState.Filled
            || State == LeaderOrderState.Canceled
            || State == LeaderOrderState.Rejected
            || State == LeaderOrderState.UnknownTerminal;

        public bool LooksLikeEntry =>
            Action == OrderActionKind.Buy || Action == OrderActionKind.SellShort;
    }

    public sealed class NormalizedExecutionEvent
    {
        public NormalizedExecutionEvent(
            EventId eventId,
            DateTime observedAtUtc,
            AccountKey account,
            ExecutionKey executionKey,
            LeaderOrderKey orderKey,
            InstrumentKey instrument,
            OrderActionKind action,
            int quantity,
            decimal price)
        {
            EventId = eventId;
            ObservedAtUtc = DateTime.SpecifyKind(observedAtUtc, DateTimeKind.Utc);
            Account = account;
            ExecutionKey = executionKey;
            OrderKey = orderKey;
            Instrument = instrument;
            Action = action;
            Quantity = quantity;
            Price = price;
        }

        public EventId EventId { get; }
        public DateTime ObservedAtUtc { get; }
        public AccountKey Account { get; }
        public ExecutionKey ExecutionKey { get; }
        public LeaderOrderKey OrderKey { get; }
        public InstrumentKey Instrument { get; }
        public OrderActionKind Action { get; }
        public int Quantity { get; }
        public decimal Price { get; }
    }
}
