using System;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.AddOns
{
    /// <summary>
    /// Native AddOn entry. Copying starts disabled. Submit is only reachable
    /// through the SIM-positive execution gate after an explicit enable.
    /// Starts the shipped <see cref="TradeCopia.Native.TradeCopiaEngineHost"/>.
    /// </summary>
    public class TradeCopiaAddOn : AddOnBase
    {
        private readonly TradeCopia.Native.TradeCopiaEngineHost _host =
            new TradeCopia.Native.TradeCopiaEngineHost(
                new TradeCopia.Native.NinjaTraderOrderBridge(),
                TradeCopia.Native.NinjaTraderOrderBridge.Classify);

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
