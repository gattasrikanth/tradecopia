using System;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.AddOns
{
    /// <summary>
    /// Native AddOn entry. Copying starts disabled. This type never calls
    /// Account.Submit/Change/Cancel/Flatten in the current Alpha.
    /// Starts the shipped <see cref="TradeCopia.Native.TradeCopiaEngineHost"/>,
    /// which hosts the named-pipe engine server.
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
