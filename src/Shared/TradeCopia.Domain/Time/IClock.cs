using System;

namespace TradeCopia.Domain.Time
{
    public interface IClock
    {
        DateTime UtcNow { get; }
        long HighResolutionTicks { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public long HighResolutionTicks => DateTime.UtcNow.Ticks;
    }

    public sealed class FrozenClock : IClock
    {
        public FrozenClock(DateTime utcNow, long highResolutionTicks)
        {
            UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            HighResolutionTicks = highResolutionTicks;
        }

        public DateTime UtcNow { get; private set; }
        public long HighResolutionTicks { get; private set; }

        public void Advance(TimeSpan delta)
        {
            UtcNow = UtcNow.Add(delta);
            HighResolutionTicks += delta.Ticks;
        }
    }
}
