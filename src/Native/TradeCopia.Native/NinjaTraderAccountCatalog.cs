using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using TradeCopia.Domain.Safety;
using TradeCopia.Protocol;

namespace TradeCopia.Native
{
    public static class NinjaTraderAccountCatalog
    {
        public static IReadOnlyList<EngineAccountRecord> Capture()
        {
            var list = new List<EngineAccountRecord>();
            lock (Account.All)
            {
                foreach (Account account in Account.All)
                {
                    if (account == null)
                    {
                        continue;
                    }

                    var provider = account.Provider.ToString();
                    var mode = string.Empty;
                    var isDemo = false;
                    try
                    {
                        var connection = account.Connection;
                        if (connection != null && connection.Options != null)
                        {
                            isDemo = connection.Options.IsDemo;
                            mode = connection.Options.Mode.ToString();
                        }
                    }
                    catch (Exception)
                    {
                    }

                    var safety = AccountSafetyClassifier.Classify(provider, mode, isDemo);
                    var key = provider + "|" + account.Id.ToString();
                    list.Add(new EngineAccountRecord(
                        key,
                        account.Name ?? account.DisplayName ?? key,
                        provider,
                        mode,
                        isDemo,
                        safety));
                }
            }

            return list;
        }
    }
}
