using System;

namespace TradeCopia.Domain
{
    public readonly struct AccountKey : IEquatable<AccountKey>
    {
        public AccountKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Account key is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(AccountKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AccountKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(AccountKey left, AccountKey right) => left.Equals(right);
        public static bool operator !=(AccountKey left, AccountKey right) => !left.Equals(right);
    }

    public readonly struct InstrumentKey : IEquatable<InstrumentKey>
    {
        public InstrumentKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Instrument key is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(InstrumentKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is InstrumentKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(InstrumentKey left, InstrumentKey right) => left.Equals(right);
        public static bool operator !=(InstrumentKey left, InstrumentKey right) => !left.Equals(right);
    }

    public readonly struct LeaderOrderKey : IEquatable<LeaderOrderKey>
    {
        public LeaderOrderKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Leader order key is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(LeaderOrderKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LeaderOrderKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(LeaderOrderKey left, LeaderOrderKey right) => left.Equals(right);
        public static bool operator !=(LeaderOrderKey left, LeaderOrderKey right) => !left.Equals(right);
    }

    public readonly struct FollowerOrderKey : IEquatable<FollowerOrderKey>
    {
        public FollowerOrderKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Follower order key is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(FollowerOrderKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is FollowerOrderKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(FollowerOrderKey left, FollowerOrderKey right) => left.Equals(right);
        public static bool operator !=(FollowerOrderKey left, FollowerOrderKey right) => !left.Equals(right);
    }

    public readonly struct ExecutionKey : IEquatable<ExecutionKey>
    {
        public ExecutionKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Execution key is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(ExecutionKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ExecutionKey other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ExecutionKey left, ExecutionKey right) => left.Equals(right);
        public static bool operator !=(ExecutionKey left, ExecutionKey right) => !left.Equals(right);
    }

    public readonly struct CopyGroupId : IEquatable<CopyGroupId>
    {
        public CopyGroupId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Copy group id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static CopyGroupId New() => new CopyGroupId(Guid.NewGuid());
        public bool Equals(CopyGroupId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is CopyGroupId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(CopyGroupId left, CopyGroupId right) => left.Equals(right);
        public static bool operator !=(CopyGroupId left, CopyGroupId right) => !left.Equals(right);
    }

    public readonly struct LogicalOrderId : IEquatable<LogicalOrderId>
    {
        public LogicalOrderId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Logical order id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static LogicalOrderId New() => new LogicalOrderId(Guid.NewGuid());
        public bool Equals(LogicalOrderId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is LogicalOrderId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(LogicalOrderId left, LogicalOrderId right) => left.Equals(right);
        public static bool operator !=(LogicalOrderId left, LogicalOrderId right) => !left.Equals(right);
    }

    public readonly struct LogicalTradeId : IEquatable<LogicalTradeId>
    {
        public LogicalTradeId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Logical trade id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static LogicalTradeId New() => new LogicalTradeId(Guid.NewGuid());
        public bool Equals(LogicalTradeId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is LogicalTradeId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(LogicalTradeId left, LogicalTradeId right) => left.Equals(right);
        public static bool operator !=(LogicalTradeId left, LogicalTradeId right) => !left.Equals(right);
    }

    public readonly struct CommandId : IEquatable<CommandId>
    {
        public CommandId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Command id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static CommandId New() => new CommandId(Guid.NewGuid());
        public bool Equals(CommandId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is CommandId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(CommandId left, CommandId right) => left.Equals(right);
        public static bool operator !=(CommandId left, CommandId right) => !left.Equals(right);
    }

    public readonly struct EventId : IEquatable<EventId>
    {
        public EventId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Event id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static EventId New() => new EventId(Guid.NewGuid());
        public bool Equals(EventId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is EventId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(EventId left, EventId right) => left.Equals(right);
        public static bool operator !=(EventId left, EventId right) => !left.Equals(right);
    }

    public readonly struct ConfigVersion : IEquatable<ConfigVersion>, IComparable<ConfigVersion>
    {
        public ConfigVersion(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Config version cannot be negative.");
            }

            Value = value;
        }

        public long Value { get; }

        public ConfigVersion Next() => new ConfigVersion(Value + 1);
        public int CompareTo(ConfigVersion other) => Value.CompareTo(other.Value);
        public bool Equals(ConfigVersion other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ConfigVersion other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(ConfigVersion left, ConfigVersion right) => left.Equals(right);
        public static bool operator !=(ConfigVersion left, ConfigVersion right) => !left.Equals(right);
    }

    public readonly struct DivergenceId : IEquatable<DivergenceId>
    {
        public DivergenceId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Divergence id cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public Guid Value { get; }

        public static DivergenceId New() => new DivergenceId(Guid.NewGuid());
        public bool Equals(DivergenceId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is DivergenceId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("D");
        public static bool operator ==(DivergenceId left, DivergenceId right) => left.Equals(right);
        public static bool operator !=(DivergenceId left, DivergenceId right) => !left.Equals(right);
    }

    public readonly struct OcoGroupId : IEquatable<OcoGroupId>
    {
        public OcoGroupId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("OCO group id is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public static OcoGroupId NewForFollower(AccountKey follower, string leaderOco)
        {
            var suffix = string.IsNullOrEmpty(leaderOco) ? Guid.NewGuid().ToString("N") : leaderOco;
            return new OcoGroupId("TC-" + follower.Value + "-" + suffix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        public bool Equals(OcoGroupId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is OcoGroupId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(OcoGroupId left, OcoGroupId right) => left.Equals(right);
        public static bool operator !=(OcoGroupId left, OcoGroupId right) => !left.Equals(right);
    }
}
