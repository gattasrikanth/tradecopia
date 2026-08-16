using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Intents;

namespace TradeCopia.Native.Adapter
{
    public static class SimulationIdentity
    {
        public static bool IsPositiveSimulation(TriState state)
        {
            return state == TriState.KnownTrue;
        }
    }

    /// <summary>
    /// Fail-closed wrapper: submit/change/cancel/flatten only when the follower
    /// account is positively classified as simulation. Name substrings are not used.
    /// </summary>
    public sealed class SimulationGuardedExecutor : INativeOrderExecutor
    {
        private readonly INativeOrderExecutor _inner;
        private readonly Func<AccountKey, TriState> _classify;

        public SimulationGuardedExecutor(INativeOrderExecutor inner, Func<AccountKey, TriState> classify)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _classify = classify ?? throw new ArgumentNullException(nameof(classify));
        }

        public NativeExecutionResult Execute(ExecutionIntent intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (intent.Kind == IntentKind.NoOp || intent.Kind == IntentKind.RaiseDivergence)
            {
                return _inner.Execute(intent);
            }

            if (!intent.Follower.HasValue)
            {
                return NativeExecutionResult.Blocked("missing-follower");
            }

            var classification = _classify(intent.Follower.Value);
            if (!SimulationIdentity.IsPositiveSimulation(classification))
            {
                return NativeExecutionResult.Blocked("simulation-not-positive:" + classification);
            }

            return _inner.Execute(intent);
        }
    }

    public sealed class RecordingOrderExecutor : INativeOrderExecutor
    {
        public int SubmitAttempts { get; private set; }
        public ExecutionIntent? Last { get; private set; }

        public NativeExecutionResult Execute(ExecutionIntent intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (intent.Kind == IntentKind.SubmitFollowerOrder
                || intent.Kind == IntentKind.ChangeFollowerOrder
                || intent.Kind == IntentKind.CancelFollowerOrder
                || intent.Kind == IntentKind.FlattenFollowerInstrument)
            {
                SubmitAttempts++;
                Last = intent;
            }

            return new NativeExecutionResult(true, "recorded");
        }
    }
}
