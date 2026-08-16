using System;
using System.Diagnostics;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using TradeCopia.Domain;
using TradeCopia.Native.Adapter;

namespace NinjaTrader.NinjaScript.AddOns
{
    /// <summary>
    /// Native AddOn entry. Copying starts disabled. This type never calls
    /// Account.Submit/Change/Cancel/Flatten in the current Alpha.
    /// </summary>
    public class TradeCopiaAddOn : AddOnBase
    {
        private readonly TradeCopia.Native.TradeCopiaEngineHost _host = new TradeCopia.Native.TradeCopiaEngineHost();

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = TradeCopia.Native.TradeCopiaEngineHost.ProductName;
                Description = "Local-first TradeCopia engine host. Copying starts disabled.";
                _host.Start();
                Account.AccountStatusUpdate += OnAccountStatusUpdate;
                _host.Subscriptions.Register("static:AccountStatusUpdate");
            }
            else if (State == State.Terminated)
            {
                Account.AccountStatusUpdate -= OnAccountStatusUpdate;
                _host.Stop();
            }
        }

        private static void OnAccountStatusUpdate(object sender, AccountStatusEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }
        }
    }
}

namespace TradeCopia.Native
{
    public sealed class TradeCopiaEngineHost
    {
        public const string ProductName = "TradeCopia";
        public static readonly EngineSafetyState DefaultState = EngineSafetyState.Disabled;

        private readonly SubscriptionRegistry _subscriptions = new SubscriptionRegistry();
        private readonly DisabledOrderExecutor _executor = new DisabledOrderExecutor();

        public EngineSafetyState State { get; private set; } = DefaultState;
        public SubscriptionRegistry Subscriptions => _subscriptions;
        public INativeOrderExecutor Executor => _executor;

        public void Start()
        {
            State = EngineSafetyState.Disabled;
            _subscriptions.Register("engine:status");
        }

        public void Stop()
        {
            _subscriptions.UnregisterAll();
            State = EngineSafetyState.Disabled;
        }

        public static void OpenDashboard(int port)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://127.0.0.1:" + port,
                UseShellExecute = true
            });
        }
    }
}
