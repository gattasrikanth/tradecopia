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
                PublishAccounts();
                SubscribeOrderUpdates();
                Account.AccountStatusUpdate += OnAccountStatusUpdate;
                _host.Subscriptions.Register("static:AccountStatusUpdate");
                _host.Subscriptions.Register("instance:OrderUpdate");
            }
            else if (State == State.Terminated)
            {
                Account.AccountStatusUpdate -= OnAccountStatusUpdate;
                UnsubscribeOrderUpdates();
                _host.Stop();
            }
        }

        private void OnAccountStatusUpdate(object sender, AccountStatusEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            PublishAccounts();
            SubscribeOrderUpdates();
        }

        private void SubscribeOrderUpdates()
        {
            lock (Account.All)
            {
                foreach (Account account in Account.All)
                {
                    if (account == null)
                    {
                        continue;
                    }

                    account.OrderUpdate -= OnOrderUpdate;
                    account.OrderUpdate += OnOrderUpdate;
                }
            }
        }

        private void UnsubscribeOrderUpdates()
        {
            lock (Account.All)
            {
                foreach (Account account in Account.All)
                {
                    if (account == null)
                    {
                        continue;
                    }

                    account.OrderUpdate -= OnOrderUpdate;
                }
            }
        }

        private void OnOrderUpdate(object sender, OrderEventArgs args)
        {
            if (args == null || args.Order == null)
            {
                return;
            }

            var account = sender as Account ?? args.Order.Account;
            var evt = TradeCopia.Native.NinjaTraderOrderNormalizer.Capture(account, args.Order);
            if (evt == null)
            {
                return;
            }

            _host.HandleOrder(evt);
        }

        private void PublishAccounts()
        {
            _host.PublishAccounts(TradeCopia.Native.NinjaTraderAccountCatalog.Capture());
        }
    }
}
