namespace TradeCopia.Domain
{
    public enum TriState
    {
        Unknown = 0,
        KnownTrue = 1,
        KnownFalse = 2
    }

    public enum CopyMode
    {
        OrderMirror = 0,
        ExecutionMirror = 1
    }

    public enum EngineSafetyState
    {
        Disabled = 0,
        PausedNewEntries = 1,
        Enabled = 2
    }

    public enum GroupEnabledState
    {
        Disabled = 0,
        PausedNewEntries = 1,
        Enabled = 2
    }

    public enum SizingMode
    {
        OneToOne = 0,
        Multiplier = 1,
        Fixed = 2,
        Disabled = 3
    }

    public enum DomainOrderType
    {
        Market = 0,
        Limit = 1,
        StopMarket = 2,
        StopLimit = 3,
        Mit = 4,
        Unsupported = 5
    }

    public enum OrderActionKind
    {
        Buy = 0,
        Sell = 1,
        BuyToCover = 2,
        SellShort = 3
    }

    public enum LeaderOrderState
    {
        Observed = 0,
        PendingSubmission = 1,
        Working = 2,
        PartiallyFilled = 3,
        Filled = 4,
        CancelPending = 5,
        Canceled = 6,
        ChangePending = 7,
        Rejected = 8,
        UnknownTerminal = 9
    }

    public enum LogicalCopyState
    {
        Discovered = 0,
        Validated = 1,
        Dispatching = 2,
        Active = 3,
        PartiallySatisfied = 4,
        Satisfied = 5,
        Canceling = 6,
        Canceled = 7,
        Failed = 8,
        Divergent = 9,
        Terminal = 10
    }

    public enum FollowerLinkHealth
    {
        NotApplicable = 0,
        Pending = 1,
        Dispatched = 2,
        Acknowledged = 3,
        Working = 4,
        PartiallyFilled = 5,
        Filled = 6,
        Canceled = 7,
        Rejected = 8,
        Disconnected = 9,
        Divergent = 10,
        Unknown = 11
    }

    public enum AccountReadiness
    {
        Unknown = 0,
        Disconnected = 1,
        Connecting = 2,
        ConnectedButUnverified = 3,
        Ready = 4,
        BlockedByRisk = 5,
        BlockedByConfig = 6
    }

    public enum IntentKind
    {
        NoOp = 0,
        SubmitFollowerOrder = 1,
        ChangeFollowerOrder = 2,
        CancelFollowerOrder = 3,
        FlattenFollowerInstrument = 4,
        RaiseDivergence = 5,
        StageProtectionOrder = 6,
        ActivateProtectionOrder = 7
    }

    public enum DivergenceClass
    {
        FollowerDisconnected = 0,
        MissingFollowerOrder = 1,
        UnexpectedFollowerOrder = 2,
        FollowerRejected = 3,
        PositionQuantityMismatch = 4,
        PositionDirectionMismatch = 5,
        FollowerFlatWhileLeaderExposed = 6,
        UnexpectedFollowerExposure = 7,
        MissingStop = 8,
        MissingTarget = 9,
        ProtectionQuantityMismatch = 10,
        ProtectionPriceMismatch = 11,
        OrphanMappedOrder = 12,
        UnknownNativeOrderState = 13,
        ConfigMismatch = 14,
        RecoveryAmbiguity = 15,
        UnsupportedOrderType = 16,
        RiskCapBlocked = 17,
        SimulationRequired = 18
    }

    public enum Severity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    public enum ExpiryMappingPolicy
    {
        ExactMonthOnly = 0
    }

    public enum IntentClassification
    {
        Unknown = 0,
        Entry = 1,
        ScaleIn = 2,
        ScaleOut = 3,
        Exit = 4,
        ProtectionStop = 5,
        ProtectionTarget = 6,
        CancelRemainder = 7
    }
}
