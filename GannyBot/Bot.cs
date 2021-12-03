using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Dynamic;
using System.Drawing;

using Newtonsoft.Json;

using Nethereum.Util;

namespace GannyBot
{
    internal class BotManager
    {
        public static Form1 _form;
        public string WALLET_ADDRESS = Properties.Settings.Default.wallet_address;
        public string WALLET_PRIVATE_KEY = Properties.Settings.Default.wallet_private_key;

        public BlockChain web3 = new BlockChain();
        List<Token> walletTokens = new List<Token>();
        List<Token> limitTokens = new List<Token>();
        Trade.LimitOrderList limitOrders = new Trade.LimitOrderList();

        BigDecimal priceETH = 0;

        Token marketTokenInput = new Token();
        Token limitTokenInput = new Token();

        System.Threading.Timer timer_ETHPrice;
        System.Threading.Timer timer1;
        System.Threading.Timer timer_wallet;
        System.Threading.Timer timer_MarketTokenInfo;
        System.Threading.Timer timer_LimitTokenInfo;

        string wallet_LastSelectedTokenAddress = null;
        int maxLimitOrderCount = 20;
        int maxUniqueTokenLimitCount = 5;

        public bool Initialize()
        {
            web3.WALLET_ADDRESS = Properties.Settings.Default.wallet_address;
            web3.WALLET_PRIVATE_KEY = Properties.Settings.Default.wallet_private_key;

            if (web3.Start())
            {
                DeleteAllLimitOrder();

                timer_ETHPrice = new System.Threading.Timer(_ => Timer_ETHPrice(), null, 0, Timeout.Infinite);
                timer1 = new System.Threading.Timer(_ => timer1_Tick(), null, 1000, Timeout.Infinite);
                timer_wallet = new System.Threading.Timer(_ => timer_wallet_Tick(), null, 2500, Timeout.Infinite);
                timer_MarketTokenInfo = new System.Threading.Timer(_ => Timer_MarketTokenInfo(), null, 2500, Timeout.Infinite);
                timer_LimitTokenInfo = new System.Threading.Timer(_ => Timer_LimitTokenInfo(), null, 2500, Timeout.Infinite);
                return true;
            }
            else
            {
                return false;
            }
        }

        async Task<BigDecimal> GetWETHPrice_Router()
        {
            return await web3.GetEthTokenInputPrice(web3.GetUSDTAddress(), 1);
        }

        async Task<decimal> GetWETHPrice_Api()
        {
            HttpClient client = new HttpClient();

            string API_ENDPOINT = "https://api.binance.com/api/v3/ticker/price?symbol=BNBUSDT";

            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);
            string contentString = await response.Content.ReadAsStringAsync();
            dynamic parsedJson = JsonConvert.DeserializeObject(contentString);
            return Convert.ToDecimal(parsedJson["price"]);
            
        }

        async public Task<BigDecimal> GetTokenPrice(string tokenAddress)
        {
            return await web3.GetTokenTokenInputPrice(web3.GetUSDTAddress(), tokenAddress, 1);
        }

        async public Task<Token> GetTokenInfo(string walletAddress, string tokenAddress)
        {
            string tokenAbi = await web3.GetTokenAbi(tokenAddress);

            Token token = new Token();
            token.Name = await web3.GetTokenName(tokenAddress, tokenAbi);
            token.Symbol = await web3.GetTokenSymbol(tokenAddress, tokenAbi);
            token.Address = tokenAddress;
            token.Decimals = await web3.GetTokenDecimals(tokenAddress, tokenAbi);
            token.Balance = await web3.GetTokenBalance(walletAddress, tokenAddress, tokenAbi);
            token.Abi = tokenAbi;
            token.Approved = await web3.CheckApprove(walletAddress, tokenAddress, token.Abi);

            return token;
        }

        async void Timer_ETHPrice()
        {
            priceETH = await GetWETHPrice_Router();
            priceETH = priceETH.RoundAwayFromZero(2);

            _form.ShowMainTokenPrice(priceETH);

            timer_ETHPrice.Change(2500, Timeout.Infinite);
        }

        /**********************************************************/
        /************************ MARKET **************************/
        /**********************************************************/
        async public void Market_AddTokenInfo(string tokenAddress)
        {
            if(marketTokenInput.Address != tokenAddress)
            {
                if(await web3.CheckTokenAddress(tokenAddress))
                { 
                    marketTokenInput = await GetTokenInfo(WALLET_ADDRESS, tokenAddress);
                    _form.Market_ShowTokenInfo(marketTokenInput);
                }
            }
        }

        async void Timer_MarketTokenInfo()
        {
            if (marketTokenInput.Address != null && _form.CheckSelectedMainTab("mainTab_market"))
            {
                BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(marketTokenInput.Address, 1);
                marketTokenInput.Price = tokenPrice.RoundAwayFromZero(10);
                marketTokenInput.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, marketTokenInput.Address, await web3.GetTokenAbi(marketTokenInput.Address));
                _form.Market_ShowTokenInfo(marketTokenInput);

                string type = _form.Market_GetMarketType();
                decimal amount = _form.Market_GetInputValue();
                decimal slippage = _form.Market_GetSlippage();

                if (amount != 0)
                {
                    if (type == "Buy")
                    {
                        try
                        {
                            BigDecimal minimumReceived = await GetEthToTokenMinimumReceived(marketTokenInput.Address, tokenPrice, (BigDecimal)amount, slippage);
                            _form.Market_SetMinimumReceived(minimumReceived.RoundAwayFromZero(5));
                        }
                        catch { }
                    }
                    else if (type == "Sell")
                    {
                        try
                        {
                            BigDecimal minimumReceived = await GetTokenToEthMinimumReceived(marketTokenInput.Address, tokenPrice, (BigDecimal)amount, slippage);
                            _form.Market_SetMinimumReceived(minimumReceived.RoundAwayFromZero(5));
                        }
                        catch { }
                    }
                }
            }
            //Thread.Sleep(2500);
            timer_MarketTokenInfo.Change(2500, Timeout.Infinite);
        }

        /**********************************************************/
        /************************ TRADE ***************************/
        /**********************************************************/
        public async Task<BigDecimal> GetEthToTokenMinimumReceived(string tokenAddress, BigDecimal price, BigDecimal quantity, decimal slippage)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await web3.GetEthTokenInputPrice(tokenAddress, quantity);
            return amountOutMin;
        }

        public async Task<BigDecimal> GetTokenToEthMinimumReceived(string tokenAddress, BigDecimal price, BigDecimal quantity, decimal slippage)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await web3.GetTokenEthInputPrice(tokenAddress, quantity);
            return amountOutMin;
        }

        async Task<dynamic> CheckTransactionStatus(dynamic transactionReceipt)
        {
            dynamic response = new ExpandoObject();

            if (transactionReceipt.Error)
            {
                response.Error = true;
                response.Message = transactionReceipt.Message;
            }
            else
            {
                dynamic transactionDetails = await web3.GetTransactionDetails(transactionReceipt.TransactionHash);
                response.Error = false;
                response.TransactionDetails = transactionDetails;
            }
            return response;
        }

        async public Task<dynamic> BuyToken(string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic transactionReceipt = await web3.MakeTradeInput(
                web3.WALLET_ADDRESS,
                web3.GetWETHAddress(),
                tokenAddress,
                quantity,
                slippage,
                gasPrice
            );

            return await CheckTransactionStatus(transactionReceipt);
        }

        async public Task<dynamic> SellToken(string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic transactionReceipt = await web3.MakeTradeInput(
                web3.WALLET_ADDRESS,
                tokenAddress,
                web3.GetWETHAddress(),
                quantity,
                slippage,
                gasPrice
                );

            return await CheckTransactionStatus(transactionReceipt);
        }

        async public Task<dynamic> ApproveToken(string tokenAddress, BigDecimal quantity)
        {
            dynamic transactionReceipt = await web3.Approve(
                            tokenAddress,
                            quantity
                        );

            return await CheckTransactionStatus(transactionReceipt);
        }

        /**********************************************************/
        /************************ WALLET **************************/
        /**********************************************************/
        #region WALLET
        async public Task<dynamic> wallet_AddToken(string tokenAddress)
        {
            dynamic response = new ExpandoObject();
            if (!await web3.CheckTokenAddress(tokenAddress))
            {
                response.Error = true;
                response.Message = "Invalid token address";
                return response;
            }

            Token token = await GetTokenInfo(WALLET_ADDRESS, tokenAddress);
            walletTokens.Add(token);

            response.Error = false;
            response.Token = token;
            return response;
        }

        public void wallet_RemoveToken(string tokenAddress)
        {
            foreach (Token token in walletTokens)
            {
                if(token.Address == tokenAddress)
                {
                    walletTokens.Remove(token);
                    break;
                }
            }
        }

        async void timer_wallet_Tick()
        {
            if (walletTokens.Count > 0 && _form.CheckSelectedMainTab("mainTab_wallet"))
            {
                string address = _form.wallet_ListViewSelectedItem();

                if (address != null || wallet_LastSelectedTokenAddress != null)
                {
                    if (address == null) address = wallet_LastSelectedTokenAddress;
                    wallet_LastSelectedTokenAddress = address;

                    foreach (Token token in walletTokens.ToList())
                    {
                        if (token.Address == address)
                        {
                            BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(token.Address, 1);
                            
                            token.Price = tokenPrice.RoundAwayFromZero(10);
                            _form.wallet_ShowTokenInfo(token);
                            break;
                        }
                    }
                }
            }
            Thread.Sleep(1000);
            timer_wallet.Change(1000, Timeout.Infinite);
        }

        #endregion
        /**********************************************************/
        /************************* LIMIT **************************/
        /**********************************************************/
        #region Limit
        async public void Limit_AddTokenInfo(string tokenAddress)
        {
            if (limitTokenInput.Address != tokenAddress)
            {
                if (await web3.CheckTokenAddress(tokenAddress))
                {
                    limitTokenInput = await GetTokenInfo(WALLET_ADDRESS, tokenAddress);
                    _form.Limit_ShowTokenInfo(limitTokenInput);
                }
            }
        }

        async void Timer_LimitTokenInfo()
        {
            bool isTokenListed = false;
            if (limitTokenInput.Address != null && _form.CheckSelectedMainTab("mainTab_limit"))
            {
                foreach (Token limitToken in limitTokens.ToList())
                {
                    if(limitToken.Address == limitTokenInput.Address)
                    {
                        _form.Limit_ShowTokenInfo(limitToken);
                        isTokenListed = true;
                    }
                }
                
                if (!isTokenListed)
                {
                    BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(limitTokenInput.Address, 1);
                    limitTokenInput.Price = tokenPrice.RoundAwayFromZero(10);
                    limitTokenInput.Balance = await web3.GetTokenBalance(WALLET_ADDRESS, limitTokenInput.Address, await web3.GetTokenAbi(limitTokenInput.Address));
                    _form.Limit_ShowTokenInfo(limitTokenInput);
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
            _form.ListView2_UpdateTokenColor(limitOrder.ID, background);
        }

        public async Task<dynamic> NewLimitOrder(string tokenAddress, string type, BigDecimal price, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic response = new ExpandoObject();

            if (! await web3.CheckTokenAddress(tokenAddress))
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
                    _form.ListView2_RemoveItem(ID.ToString());
                    limitOrders.Remove(limitOrder);
                    sameTokenCount--;
                }
            }

            if(sameTokenCount == 0)
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

                if(!response.Error)
                {
                    DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    System.Diagnostics.Debug.WriteLine("\n- BUY | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Buy Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    SetLimitOrderStatus(limitOrder, false);
                    limitOrder.ErrorCount--;

                    if(limitOrder.ErrorCount <= 0)
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

                    if(!approveResponse.Error)
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
                _form.ListView2_UpdateTokenPrice(limitToken.Address, tokenPrice);

                foreach (Trade.LimitOrder limitOrder in limitOrders.orders.ToList())
                {
                    if(!limitOrder.Process && limitOrder.OutputAddress == limitToken.Address)
                    {
                        if(limitOrder.Type == "Buy" && limitOrder.Price >= tokenPrice)
                        {
                            System.Diagnostics.Debug.WriteLine("-------- Worker Buy");

                            LimitTrade(limitOrder, tokenPrice);
                        }
                        else if(limitOrder.Type == "Sell" && limitOrder.Price <= tokenPrice)
                        {
                            if(limitToken.Balance > 0)
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
        #endregion
    }

    class Decorator : BotManager
    {
        readonly BotManager _bot;
        public Decorator(BotManager bot)
        {
            _bot = bot;
        }
    }

    class BotDecorator : Decorator
    {
        readonly BotManager _bot;
        public BotDecorator(BotManager bot) : base(bot)
        {
            _bot = bot;
        }
    }
}
