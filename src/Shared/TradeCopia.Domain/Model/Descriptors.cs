using System;

namespace TradeCopia.Domain.Model
{
    public sealed class AccountDescriptor
    {
        public AccountDescriptor(
            AccountKey key,
            string displayName,
            string connectionName,
            AccountReadiness readiness,
            TriState isSimulation)
        {
            Key = key;
            DisplayName = displayName ?? string.Empty;
            ConnectionName = connectionName ?? string.Empty;
            Readiness = readiness;
            IsSimulation = isSimulation;
        }

        public AccountKey Key { get; }
        public string DisplayName { get; }
        public string ConnectionName { get; }
        public AccountReadiness Readiness { get; }
        public TriState IsSimulation { get; }

        public bool IsReadyForNewEntries => Readiness == AccountReadiness.Ready;

        public bool IsPositivelySimulation => IsSimulation == TriState.KnownTrue;
    }

    public sealed class InstrumentDescriptor
    {
        public InstrumentDescriptor(
            InstrumentKey key,
            string fullName,
            string rootSymbol,
            string expiry,
            decimal tickSize)
        {
            Key = key;
            FullName = fullName ?? string.Empty;
            RootSymbol = rootSymbol ?? string.Empty;
            Expiry = expiry ?? string.Empty;
            if (tickSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickSize));
            }

            TickSize = tickSize;
        }

        public InstrumentKey Key { get; }
        public string FullName { get; }
        public string RootSymbol { get; }
        public string Expiry { get; }
        public decimal TickSize { get; }
    }

    public sealed class InstrumentMapping
    {
        public InstrumentMapping(
            string sourceRoot,
            string targetRoot,
            decimal contractValueRatio,
            decimal defaultQuantityRatio,
            ExpiryMappingPolicy expiryPolicy)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                throw new ArgumentException("Source root is required.", nameof(sourceRoot));
            }

            if (string.IsNullOrWhiteSpace(targetRoot))
            {
                throw new ArgumentException("Target root is required.", nameof(targetRoot));
            }

            if (contractValueRatio <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractValueRatio));
            }

            if (defaultQuantityRatio <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultQuantityRatio));
            }

            SourceRoot = sourceRoot.Trim();
            TargetRoot = targetRoot.Trim();
            ContractValueRatio = contractValueRatio;
            DefaultQuantityRatio = defaultQuantityRatio;
            ExpiryPolicy = expiryPolicy;
        }

        public string SourceRoot { get; }
        public string TargetRoot { get; }
        public decimal ContractValueRatio { get; }
        public decimal DefaultQuantityRatio { get; }
        public ExpiryMappingPolicy ExpiryPolicy { get; }
    }

    public sealed class SizingPolicy
    {
        public SizingPolicy(
            SizingMode mode,
            decimal multiplier,
            int fixedQuantity,
            bool minimumOne,
            int? maxQuantity,
            int? maxAbsolutePosition)
        {
            if (mode == SizingMode.Multiplier && multiplier <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be positive.");
            }

            if (mode == SizingMode.Fixed && fixedQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedQuantity), "Fixed quantity must be positive.");
            }

            if (maxQuantity.HasValue && maxQuantity.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQuantity));
            }

            if (maxAbsolutePosition.HasValue && maxAbsolutePosition.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAbsolutePosition));
            }

            Mode = mode;
            Multiplier = multiplier;
            FixedQuantity = fixedQuantity;
            MinimumOne = minimumOne;
            MaxQuantity = maxQuantity;
            MaxAbsolutePosition = maxAbsolutePosition;
        }

        public static SizingPolicy OneToOne()
        {
            return new SizingPolicy(SizingMode.OneToOne, 1m, 0, false, null, null);
        }

        public SizingMode Mode { get; }
        public decimal Multiplier { get; }
        public int FixedQuantity { get; }
        public bool MinimumOne { get; }
        public int? MaxQuantity { get; }
        public int? MaxAbsolutePosition { get; }
    }

    public sealed class RiskPolicy
    {
        public RiskPolicy(bool simulationOnly, bool dryRun, bool blockWhenAnyFollowerDisconnected)
        {
            SimulationOnly = simulationOnly;
            DryRun = dryRun;
            BlockWhenAnyFollowerDisconnected = blockWhenAnyFollowerDisconnected;
        }

        public static RiskPolicy Default()
        {
            return new RiskPolicy(simulationOnly: false, dryRun: false, blockWhenAnyFollowerDisconnected: true);
        }

        public bool SimulationOnly { get; }
        public bool DryRun { get; }
        public bool BlockWhenAnyFollowerDisconnected { get; }
    }

    public sealed class FollowerRule
    {
        public FollowerRule(
            AccountKey account,
            bool enabled,
            SizingPolicy sizing,
            InstrumentMapping[]? instrumentMappings)
        {
            Account = account;
            Enabled = enabled;
            Sizing = sizing ?? throw new ArgumentNullException(nameof(sizing));
            InstrumentMappings = instrumentMappings ?? Array.Empty<InstrumentMapping>();
        }

        public AccountKey Account { get; }
        public bool Enabled { get; }
        public SizingPolicy Sizing { get; }
        public InstrumentMapping[] InstrumentMappings { get; }
    }

    public sealed class CopyGroup
    {
        public CopyGroup(
            CopyGroupId id,
            string name,
            AccountKey leader,
            FollowerRule[] followers,
            CopyMode copyMode,
            GroupEnabledState enabledState)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Group name is required.", nameof(name));
            }

            Id = id;
            Name = name.Trim();
            Leader = leader;
            Followers = followers ?? throw new ArgumentNullException(nameof(followers));
            CopyMode = copyMode;
            EnabledState = enabledState;
        }

        public CopyGroupId Id { get; }
        public string Name { get; }
        public AccountKey Leader { get; }
        public FollowerRule[] Followers { get; }
        public CopyMode CopyMode { get; }
        public GroupEnabledState EnabledState { get; }

        public bool AllowsNewEntries => EnabledState == GroupEnabledState.Enabled;
        public bool AllowsRiskReducingActions => EnabledState != GroupEnabledState.Disabled;
    }
}
