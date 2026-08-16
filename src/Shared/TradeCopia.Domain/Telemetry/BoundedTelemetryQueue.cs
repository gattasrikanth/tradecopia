using System;
using System.Collections.Generic;

namespace TradeCopia.Domain.Telemetry
{
    public sealed class TelemetryItem
    {
        public TelemetryItem(int priority, string code, DateTime utc)
        {
            Priority = priority;
            Code = code ?? string.Empty;
            Utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        public int Priority { get; }
        public string Code { get; }
        public DateTime Utc { get; }
    }

    /// <summary>
    /// In-memory bounded queue. Never performs disk/HTTP/DB I/O.
    /// P2 samples may drop; P0 safety events keep reserved capacity.
    /// </summary>
    public sealed class BoundedTelemetryQueue
    {
        private readonly int _capacity;
        private readonly Queue<TelemetryItem> _items;
        private int _dropped;

        public BoundedTelemetryQueue(int capacity)
        {
            if (capacity < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _items = new Queue<TelemetryItem>(capacity);
        }

        public int Dropped => _dropped;
        public int Count => _items.Count;

        public bool TryEnqueue(TelemetryItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_items.Count < _capacity)
            {
                _items.Enqueue(item);
                return true;
            }

            if (item.Priority <= 0)
            {
                DropLowestPriority();
                if (_items.Count < _capacity)
                {
                    _items.Enqueue(item);
                    return true;
                }
            }

            _dropped++;
            return false;
        }

        public TelemetryItem Dequeue()
        {
            return _items.Dequeue();
        }

        private void DropLowestPriority()
        {
            TelemetryItem? victim = null;
            var kept = new Queue<TelemetryItem>(_capacity);
            var droppedOne = false;
            foreach (var item in _items)
            {
                if (!droppedOne && item.Priority >= 2)
                {
                    victim = item;
                    droppedOne = true;
                    continue;
                }

                kept.Enqueue(item);
            }

            if (victim == null)
            {
                return;
            }

            _items.Clear();
            foreach (var item in kept)
            {
                _items.Enqueue(item);
            }

            _dropped++;
        }
    }

    public sealed class LatencySample
    {
        public LatencySample(long observedTicks, long decidedTicks)
        {
            ObservedTicks = observedTicks;
            DecidedTicks = decidedTicks;
        }

        public long ObservedTicks { get; }
        public long DecidedTicks { get; }
        public long DecisionDeltaTicks => DecidedTicks >= ObservedTicks ? DecidedTicks - ObservedTicks : 0;
    }
}
