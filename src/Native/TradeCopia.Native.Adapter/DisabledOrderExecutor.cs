using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Intents;

namespace TradeCopia.Native.Adapter
{
    public sealed class NativeExecutionResult
    {
        public NativeExecutionResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason ?? string.Empty;
        }

        public bool Accepted { get; }
        public string Reason { get; }

        public static NativeExecutionResult Blocked(string reason)
        {
            return new NativeExecutionResult(false, reason);
        }
    }

    public interface INativeOrderExecutor
    {
        NativeExecutionResult Execute(ExecutionIntent intent);
    }

    /// <summary>
    /// Phase 1 facade. Refuses every submit/change/cancel/flatten so the
    /// AddOn can load and subscribe without placing orders.
    /// </summary>
    public sealed class DisabledOrderExecutor : INativeOrderExecutor
    {
        public NativeExecutionResult Execute(ExecutionIntent intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (intent.Kind == IntentKind.NoOp || intent.Kind == IntentKind.RaiseDivergence)
            {
                return new NativeExecutionResult(true, intent.ReasonCode);
            }

            return NativeExecutionResult.Blocked("order-submission-disabled:" + intent.Kind);
        }
    }

    public sealed class SubscriptionRegistry
    {
        private readonly object _gate = new object();
        private readonly System.Collections.Generic.HashSet<string> _active = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        public void Register(string subscriptionKey)
        {
            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentException("Subscription key is required.", nameof(subscriptionKey));
            }

            lock (_gate)
            {
                if (!_active.Add(subscriptionKey))
                {
                    throw new InvalidOperationException("Duplicate subscription for " + subscriptionKey);
                }
            }
        }

        public void Unregister(string subscriptionKey)
        {
            lock (_gate)
            {
                _active.Remove(subscriptionKey);
            }
        }

        public void UnregisterAll()
        {
            lock (_gate)
            {
                _active.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _active.Count;
                }
            }
        }
    }
}
