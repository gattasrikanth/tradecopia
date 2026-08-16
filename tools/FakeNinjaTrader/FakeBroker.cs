using System;
using System.Collections.Generic;
using TradeCopia.Domain;
using TradeCopia.Domain.Config;
using TradeCopia.Domain.Engine;
using TradeCopia.Domain.Events;
using TradeCopia.Domain.Intents;
using TradeCopia.Domain.Origin;
using TradeCopia.Domain.Time;
using TradeCopia.Native.Adapter;

namespace TradeCopia.FakeNinjaTrader
{
    public sealed class FakeBroker
    {
        private readonly CopyCoordinator _coordinator;
        private readonly INativeOrderExecutor _executor;
        private readonly List<ExecutionIntent> _accepted = new List<ExecutionIntent>();
        private readonly List<NativeExecutionResult> _results = new List<NativeExecutionResult>();

        public FakeBroker(ActiveConfigSnapshot config, IClock clock, INativeOrderExecutor executor)
        {
            _coordinator = new CopyCoordinator(config, new OriginRegistry(), clock);
            _executor = executor ?? new DisabledOrderExecutor();
        }

        public CopyCoordinator Coordinator => _coordinator;
        public IReadOnlyList<ExecutionIntent> AcceptedIntents => _accepted;
        public IReadOnlyList<NativeExecutionResult> Results => _results;

        public CoordinatorResult InjectOrder(NormalizedOrderEvent evt)
        {
            var result = _coordinator.ProcessOrder(evt);
            foreach (var intent in result.Intents)
            {
                var exec = _executor.Execute(intent);
                _results.Add(exec);
                if (exec.Accepted && intent.Kind != IntentKind.NoOp)
                {
                    _accepted.Add(intent);
                }
            }

            return result;
        }

        public CoordinatorResult InjectExecution(NormalizedExecutionEvent evt)
        {
            return _coordinator.ProcessExecution(evt);
        }
    }

    public static class SyntheticIds
    {
        public static AccountKey Leader => new AccountKey("SIM-LEADER-01");
        public static AccountKey Follower1 => new AccountKey("SIM-FOLLOWER-01");
        public static AccountKey Follower2 => new AccountKey("SIM-FOLLOWER-02");
        public static InstrumentKey Nq => new InstrumentKey("NQ 06-26");
    }
}
