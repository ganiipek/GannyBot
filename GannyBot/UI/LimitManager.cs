using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.UI
{
    internal class LimitManager
    {
        public static Form1 _form1;

        static Trade.OrderToken limitTokenInput = new Trade.OrderToken();

        List<Trade.OrderToken> limitTokens = new List<Trade.OrderToken>();

        Trade.LimitOrderList limitOrders = new Trade.LimitOrderList();

        System.Threading.Timer timer_LimitTokenInfo;
        System.Threading.Timer timer_LimitOrder;

        int maxLimitOrderCount = 20;
        int maxUniqueTokenLimitCount = 5;

        public LimitManager()
        {
            timer_LimitOrder = new System.Threading.Timer(_ => Timer_LimitOrder(), null, 1000, Timeout.Infinite);
            timer_LimitTokenInfo = new System.Threading.Timer(_ => Timer_LimitTokenInfo(), null, 2500, Timeout.Infinite);
        }

        async public void Limit_AddTokenInfo(string tokenAddress)
        {
            if (limitTokenInput.Address != tokenAddress)
            {
                try
                {
                    if (await Chain.TokenManager.IsToken(tokenAddress))
                    {
                        limitTokenInput = await Chain.TokenManager.GetTokenInfo(Chain.WalletManager.Address(), tokenAddress);
                        _form1.Limit_ShowTokenInfo(limitTokenInput);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("LimitManager (AddToken): " + ex.Message);
                    _form1.Limit_ShowError("Tekrar dene");
                }
            }
        }

        async void Timer_LimitTokenInfo()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                bool isTokenListed = false;
                if (limitTokenInput.Address != null && _form1.CheckSelectedMainTab("mainTab_limit"))
                {
                    foreach (Trade.OrderToken limitToken in limitTokens.ToList())
                    {
                        if (limitToken.Address == limitTokenInput.Address)
                        {
                            _form1.Limit_ShowTokenInfo(limitToken);
                            isTokenListed = true;
                        }
                    }

                    if (!isTokenListed)
                    {
                        try
                        {
                            BigDecimal tokenPrice = Chain.ChainManager.Token().Price / await Chain.TokenManager.GetEthTokenInputPrice(limitTokenInput.Address, 1);
                            limitTokenInput.Price = tokenPrice.RoundAwayFromZero(10);
                            limitTokenInput.Balance = await Chain.WalletManager.GetTokenBalance(Chain.WalletManager.Address(), limitTokenInput.Address, await Chain.TokenManager.GetAbi(limitTokenInput.Address));
                            _form1.Limit_ShowTokenInfo(limitTokenInput);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Timer_LimitTokenInfo: " + ex.Message);
                        }

                    }

                }
            }
            timer_LimitTokenInfo.Change(2500, Timeout.Infinite);
        }

        public int GetLimitOrderCount()
        {
            return limitOrders.Count();
        }

        public int GetUniqueTokenLimitCount()
        {
            return limitTokens.Count();
        }

        public int GetMaxLimitOrderCount()
        {
            return maxLimitOrderCount;
        }

        public int GetMaxUniqueTokenLimitCount()
        {
            return maxUniqueTokenLimitCount;
        }

        public void SetLimitOrderStatus(Trade.LimitOrder limitOrder, bool process)
        {
            Color background = Color.White;

            if (process)
            {
                limitOrder.Process = true;
                background = Color.FromArgb(254, 213, 86);
            }
            else
            {
                limitOrder.Process = false;
                background = Color.FromArgb(219, 73, 73); ;
            }
            _form1.LimitListView_UpdateTokenColor(limitOrder.ID, background);
        }

        public async Task<dynamic> NewLimitOrder(string tokenAddress, string type, BigDecimal price, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic response = new ExpandoObject();

            if (!await Chain.TokenManager.IsToken(tokenAddress))
            {
                response.Error = true;
                response.Message = "Invalid token address";
                return response;
            }

            if (limitOrders.Count() >= maxLimitOrderCount)
            {
                response.Error = true;
                response.Message = "There can be a maximum of " + maxLimitOrderCount.ToString() + " limit orders.";
                return response;
            }

            Trade.OrderToken token = null;


            if (!limitTokens.Exists(x => x.Address == tokenAddress))
            {
                if (limitTokens.Count() >= maxUniqueTokenLimitCount)
                {
                    response.Error = true;
                    response.Message = "There can be a maximum of " + maxUniqueTokenLimitCount.ToString() + " unique token for limit.";
                    return response;
                }
                if (limitTokenInput.Address == tokenAddress)
                {
                    limitTokens.Add(limitTokenInput);
                    token = limitTokenInput;
                }
                else
                {
                    Trade.OrderToken newToken = await Chain.TokenManager.GetTokenInfo(Chain.WalletManager.Address(), tokenAddress);
                    limitTokens.Add(newToken);
                    token = newToken;
                }
            }
            else
            {
                foreach (Trade.OrderToken tokenLocal in limitTokens)
                {
                    if (tokenLocal.Address == tokenAddress)
                    {
                        token = tokenLocal;
                        break;
                    }
                }
            }

            Trade.LimitOrder limitOrder = new Trade.LimitOrder();
            limitOrder.Symbol = token.Symbol;
            limitOrder.Type = type;
            limitOrder.Price = price;
            limitOrder.WalletAddress = Chain.WalletManager.Address();
            limitOrder.InputAddress = Chain.ChainManager.Token().Address;
            limitOrder.OutputAddress = tokenAddress;
            limitOrder.Quantity = quantity;
            limitOrder.Slippage = slippage;
            limitOrder.GasPrice = gasPrice;

            limitOrders.Add(limitOrder);

            response.Error = false;
            response.Token = token;
            response.Order = limitOrder;

            return response;
        }

        public void DeleteLimitOrder(int ID, string address)
        {
            int sameTokenCount = 0;
            foreach (Trade.LimitOrder limitOrder in limitOrders.orders.ToList())
            {
                if (limitOrder.OutputAddress == address) sameTokenCount++;

                if (limitOrder.ID == ID)
                {
                    _form1.LimitListView_RemoveItem(ID.ToString());
                    limitOrders.Remove(limitOrder);
                    sameTokenCount--;
                }
            }

            if (sameTokenCount == 0)
            {
                foreach (Trade.OrderToken tokenLocal in limitTokens.ToList())
                {
                    if (tokenLocal.Address == address)
                    {
                        limitTokens.Remove(tokenLocal);
                        break;
                    }
                }
            }
        }

        void DeleteAllLimitOrder()
        {
            foreach (Trade.LimitOrder limitOrder in limitOrders.orders.ToList())
            {
                DeleteLimitOrder(limitOrder.ID, limitOrder.OutputAddress);
            }
        }

        async Task LimitTrade(Trade.LimitOrder limitOrder, BigDecimal tokenPrice)
        {
            if (limitOrder.Type == "Buy")
            {
                SetLimitOrderStatus(limitOrder, true);

                dynamic transactionReceipt = await Trade.TradeManager.MakeTradeInput(
                    Chain.WalletManager.Address(),
                    Chain.ChainManager.Token().Address,
                    limitOrder.OutputAddress,
                    limitOrder.Quantity,
                    limitOrder.Slippage,
                    limitOrder.GasPrice
                );

                dynamic response = await Trade.TradeManager.CheckTransactionStatus(transactionReceipt);

                if (!response.Error)
                {
                    DeleteLimitOrder(limitOrder.ID, Chain.WalletManager.Address());
                    System.Diagnostics.Debug.WriteLine("\n- BUY | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Buy Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    SetLimitOrderStatus(limitOrder, false);
                    limitOrder.ErrorCount--;

                    if (limitOrder.ErrorCount <= 0)
                    {
                        DeleteLimitOrder(limitOrder.ID, Chain.WalletManager.Address());
                    }
                }
            }
            else if (limitOrder.Type == "Sell")
            {
                SetLimitOrderStatus(limitOrder, true);

                BigDecimal approvedAmount = await Chain.TokenManager.CheckApprove(Chain.WalletManager.Address(), limitOrder.OutputAddress, await Chain.TokenManager.GetAbi(limitOrder.OutputAddress));
                if (approvedAmount < limitOrder.Quantity)
                {
                    System.Diagnostics.Debug.WriteLine("\n- Check Approve | " + limitOrder.Symbol);
                    dynamic approveResponse = await Trade.TradeManager.Approve(limitOrder.OutputAddress, limitOrder.Quantity);

                    if (!approveResponse.Error)
                    {
                        System.Diagnostics.Debug.WriteLine("- Approve False | " + limitOrder.Symbol);
                        SetLimitOrderStatus(limitOrder, false);
                        limitOrder.ErrorCount--;

                        if (limitOrder.ErrorCount <= 0)
                        {
                            DeleteLimitOrder(limitOrder.ID, Chain.WalletManager.Address());
                        }
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("- Approve True | " + limitOrder.Symbol);
                    }
                }

                    dynamic transactionReceipt = await Trade.TradeManager.MakeTradeInput(
                    Chain.WalletManager.Address(),
                    limitOrder.OutputAddress,
                    Chain.ChainManager.Token().Address,
                    limitOrder.Quantity,
                    limitOrder.Slippage,
                    limitOrder.GasPrice
                    );

                dynamic response = await Trade.TradeManager.CheckTransactionStatus(transactionReceipt);

                if (!response.Error)
                {
                    DeleteLimitOrder(limitOrder.ID, Chain.WalletManager.Address());
                    System.Diagnostics.Debug.WriteLine("\n- SELL | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Sell Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    SetLimitOrderStatus(limitOrder, false);
                    limitOrder.ErrorCount--;

                    System.Diagnostics.Debug.WriteLine("- Error Count: | " + limitOrder.ErrorCount.ToString());

                    if (limitOrder.ErrorCount <= 0)
                    {
                        DeleteLimitOrder(limitOrder.ID, Chain.WalletManager.Address());
                    }
                }
            }
        }

        async void Timer_LimitOrder()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                foreach (Trade.OrderToken limitToken in limitTokens.ToList())
                {
                    BigDecimal tokenPrice = Chain.ChainManager.Token().Price / await Chain.TokenManager.GetEthTokenInputPrice(limitToken.Address, 1);

                    limitToken.Price = tokenPrice;
                    limitToken.Balance = await Chain.WalletManager.GetTokenBalance(Chain.WalletManager.Address(), limitToken.Address, await Chain.TokenManager.GetAbi(limitToken.Address));

                    // limitToken.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, limitToken.Address, await web3.GetTokenAbi(limitToken.Address));
                    _form1.LimitListView_UpdateTokenPrice(limitToken.Address, tokenPrice);

                    foreach (Trade.LimitOrder limitOrder in limitOrders.orders.ToList())
                    {
                        if (!limitOrder.Process && limitOrder.OutputAddress == limitToken.Address)
                        {
                            if (limitOrder.Type == "Buy" && limitOrder.Price >= tokenPrice)
                            {
                                System.Diagnostics.Debug.WriteLine("-------- Worker Buy");

                                LimitTrade(limitOrder, tokenPrice);
                            }
                            else if (limitOrder.Type == "Sell" && limitOrder.Price <= tokenPrice)
                            {
                                if (limitToken.Balance > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("-------- Worker Sell");

                                    LimitTrade(limitOrder, tokenPrice);
                                }
                            }
                        }
                    }
                }
            }
            //Thread.Sleep(1000);
            timer_LimitOrder.Change(1000, Timeout.Infinite);
        }
    }
}
