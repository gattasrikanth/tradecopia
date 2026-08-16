using System;
using TradeCopia.Domain;
using TradeCopia.Domain.Intents;

namespace TradeCopia.Native.Adapter
{
    /// <summary>
    /// Execution boundary used by the shipped engine. Copying disabled or
    /// non-positive simulation never reaches the inner submit adapter.
    /// </summary>
    public sealed class GuardedNativeRuntime
    {
        private readonly Func<bool> _copyingEnabled;
        private readonly INativeOrderExecutor _guarded;

        public GuardedNativeRuntime(
            INativeOrderExecutor inner,
            Func<AccountKey, TriState> classify,
            Func<bool> copyingEnabled)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _copyingEnabled = copyingEnabled ?? throw new ArgumentNullException(nameof(copyingEnabled));
            _guarded = new SimulationGuardedExecutor(inner, classify);
        }

        public NativeExecutionResult Dispatch(ExecutionIntent intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }

            if (intent.Kind == IntentKind.NoOp || intent.Kind == IntentKind.RaiseDivergence)
            {
                return _guarded.Execute(intent);
            }

            if (!_copyingEnabled())
            {
                return NativeExecutionResult.Blocked("copying-disabled");
            }

            return _guarded.Execute(intent);
        }
    }
}
