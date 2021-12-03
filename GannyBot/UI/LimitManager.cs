using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.UI
{
    internal class LimitManager
    {
        List<Trade.OrderToken> limitTokens = new List<Trade.OrderToken>();
        Trade.OrderToken limitTokenInput = new Trade.OrderToken();

        async public void Limit_AddTokenInfo(string tokenAddress)
        {
            if (limitTokenInput.Address != tokenAddress)
            {
                if (await Chain.TokenManager.IsToken(tokenAddress))
                {
                    limitTokenInput = await Chain.TokenManager.GetTokenInfo(Chain.WalletManager.Address(), tokenAddress);
                    Form1.Limit_ShowTokenInfo(limitTokenInput);
                }
            }
        }

        async void Timer_LimitTokenInfo()
        {
            bool isTokenListed = false;
            if (limitTokenInput.Address != null && Form1.CheckSelectedMainTab("mainTab_limit"))
            {
                foreach (Trade.OrderToken limitToken in limitTokens.ToList())
                {
                    if (limitToken.Address == limitTokenInput.Address)
                    {
                        Form1.Limit_ShowTokenInfo(limitToken);
                        isTokenListed = true;
                    }
                }

                if (!isTokenListed)
                {
                    BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(limitTokenInput.Address, 1);
                    limitTokenInput.Price = tokenPrice.RoundAwayFromZero(10);
                    limitTokenInput.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, limitTokenInput.Address, await web3.GetTokenAbi(limitTokenInput.Address));
                    Form1.Limit_ShowTokenInfo(limitTokenInput);
                }

            }
            //Thread.Sleep(2500);
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
            Form1.ListView2_UpdateTokenColor(limitOrder.ID, background);
        }

        public async Task<dynamic> NewLimitOrder(string tokenAddress, string type, BigDecimal price, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic response = new ExpandoObject();

            if (!await web3.CheckTokenAddress(tokenAddress))
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

            Token token = null;


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
                    Token newToken = await GetTokenInfo(WALLET_ADDRESS, tokenAddress);
                    limitTokens.Add(newToken);
                    token = newToken;
                }
            }
            else
            {
                foreach (Token tokenLocal in limitTokens)
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
            limitOrder.WalletAddress = WALLET_ADDRESS;
            limitOrder.InputAddress = web3.GetWETHAddress();
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
                    Form1.ListView2_RemoveItem(ID.ToString());
                    limitOrders.Remove(limitOrder);
                    sameTokenCount--;
                }
            }

            if (sameTokenCount == 0)
            {
                foreach (Token tokenLocal in limitTokens.ToList())
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
                dynamic response = await BuyToken(limitOrder.OutputAddress, limitOrder.Quantity, limitOrder.Slippage, limitOrder.GasPrice);

                if (!response.Error)
                {
                    DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    System.Diagnostics.Debug.WriteLine("\n- BUY | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Buy Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    SetLimitOrderStatus(limitOrder, false);
                    limitOrder.ErrorCount--;

                    if (limitOrder.ErrorCount <= 0)
                    {
                        DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    }
                }
            }
            else if (limitOrder.Type == "Sell")
            {
                SetLimitOrderStatus(limitOrder, true);

                BigDecimal approvedAmount = await web3.CheckApprove(WALLET_ADDRESS, limitOrder.OutputAddress, await web3.GetTokenAbi(limitOrder.OutputAddress));
                if (approvedAmount < limitOrder.Quantity)
                {
                    System.Diagnostics.Debug.WriteLine("\n- Check Approve | " + limitOrder.Symbol);
                    dynamic approveResponse = await ApproveToken(limitOrder.OutputAddress, limitOrder.Quantity);

                    if (!approveResponse.Error)
                    {
                        System.Diagnostics.Debug.WriteLine("- Approve False | " + limitOrder.Symbol);
                        SetLimitOrderStatus(limitOrder, false);
                        limitOrder.ErrorCount--;

                        if (limitOrder.ErrorCount <= 0)
                        {
                            DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                        }
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("- Approve True | " + limitOrder.Symbol);
                    }
                }

                dynamic response = await SellToken(limitOrder.OutputAddress, limitOrder.Quantity, limitOrder.Slippage, limitOrder.GasPrice);

                if (!response.Error)
                {
                    DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
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
                        DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    }
                }
            }
        }

        async void timer1_Tick()
        {
            foreach (Token limitToken in limitTokens.ToList())
            {
                BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(limitToken.Address, 1);

                limitToken.Price = tokenPrice;
                limitToken.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, limitToken.Address, await web3.GetTokenAbi(limitToken.Address));

                // limitToken.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, limitToken.Address, await web3.GetTokenAbi(limitToken.Address));
                Form1.ListView2_UpdateTokenPrice(limitToken.Address, tokenPrice);

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


            Thread.Sleep(1000);
            timer1.Change(1000, Timeout.Infinite);
        }
    }
}
