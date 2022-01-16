using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.UI
{
    internal class MarketManager
    {
        public static Form1 _form1;

        Trade.OrderToken marketTokenInput = new Trade.OrderToken();

        System.Threading.Timer timer_MarketTokenInfo;

        public MarketManager()
        {
            timer_MarketTokenInfo = new System.Threading.Timer(_ => Timer_MarketTokenInfo(), null, 2500, Timeout.Infinite);
        }

        public async void AddToken(string tokenAddress)
        {
            if (marketTokenInput.Address != tokenAddress)
            {
                try
                {
                    if (await Chain.TokenManager.IsToken(tokenAddress))
                    {
                        marketTokenInput = await Chain.TokenManager.GetTokenInfo(Chain.WalletManager.Address(), tokenAddress);
                        _form1.Market_ShowTokenInfo(marketTokenInput);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("UI.MarketManager (AddToken): " + ex.Message);
                    _form1.Market_ShowError("Tekrar dene");
                }
            }
        }

        public async void UpdateApprove(string tokenAddress)
        {
            if (marketTokenInput.Address == tokenAddress)
            {
                try
                {
                    marketTokenInput.Approved = await Chain.TokenManager.CheckApprove(Chain.WalletManager.Address(), tokenAddress, await Chain.TokenManager.GetAbi(Chain.WalletManager.Address()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("UI.MarketManager (UpdateApprove): " + ex.Message);
                }
            }
        }

        public async Task<dynamic> BuyToken(string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            _form1.Market_ShowTransactionProcess(type: 0);

            dynamic transactionReceipt = await Trade.TradeManager.MakeTradeInput(
                Chain.WalletManager.Address(),
                Chain.ChainManager.Token().Address,
                tokenAddress,
                quantity,
                slippage,
                gasPrice
                );

            _form1.Market_ShowTransactionProcess(type: 1);

            return await Trade.TradeManager.CheckTransactionStatus(transactionReceipt);
        }

        public async Task<dynamic> SellToken(string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            _form1.Market_ShowTransactionProcess(type: 0);

            dynamic transactionReceipt = await Trade.TradeManager.MakeTradeInput(
                Chain.WalletManager.Address(),
                tokenAddress,
                Chain.ChainManager.Token().Address,
                quantity,
                slippage,
                gasPrice
                );

            _form1.Market_ShowTransactionProcess(type: 1);

            return await Trade.TradeManager.CheckTransactionStatus(transactionReceipt);
        }

        async void Timer_MarketTokenInfo()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                if (marketTokenInput.Address != null && _form1.CheckSelectedMainTab("mainTab_market"))
                {
                    try
                    {
                        BigDecimal tokenPrice = Chain.ChainManager.Token().Price / await Chain.TokenManager.GetEthTokenInputPrice(marketTokenInput.Address, 1);
                        marketTokenInput.Price = tokenPrice.RoundAwayFromZero(10);
                        marketTokenInput.Balance = await Chain.WalletManager.GetTokenBalance(Chain.WalletManager.Address(), marketTokenInput.Address, await Chain.TokenManager.GetAbi(marketTokenInput.Address));
                        _form1.Market_ShowTokenInfo(marketTokenInput);

                        string type = _form1.Market_GetMarketType();
                        decimal amount = _form1.Market_GetInputValue();
                        decimal slippage = _form1.Market_GetSlippage();

                        if (amount != 0)
                        {
                            if (type == "Buy")
                            {
                                try
                                {
                                    BigDecimal minimumReceived = await Trade.TradeManager.GetEthToTokenMinimumReceived(marketTokenInput.Address, tokenPrice, (BigDecimal)amount, slippage);
                                    _form1.Market_SetMinimumReceived(minimumReceived.RoundAwayFromZero(5));
                                }
                                catch { }
                            }
                            else if (type == "Sell")
                            {
                                try
                                {
                                    BigDecimal minimumReceived = await Trade.TradeManager.GetTokenToEthMinimumReceived(marketTokenInput.Address, tokenPrice, (BigDecimal)amount, slippage);
                                    _form1.Market_SetMinimumReceived(minimumReceived.RoundAwayFromZero(5));
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("UI.MarketManager (Timer_MarketTokenInfo): " + ex.Message);
                    }
                }
            }
            //Thread.Sleep(2500);
            timer_MarketTokenInfo.Change(2500, Timeout.Infinite);
        }
    }
}
