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
                    try
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
                        var display = account.DisplayName;
                        if (string.IsNullOrEmpty(display))
                        {
                            display = account.Name;
                        }

                        if (string.IsNullOrEmpty(display))
                        {
                            display = key;
                        }

                        list.Add(new EngineAccountRecord(
                            key,
                            display,
                            provider,
                            mode,
                            isDemo,
                            safety));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return list;
        }
    }
}
