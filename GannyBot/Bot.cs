using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Dynamic;

using Newtonsoft.Json;

using Nethereum.Util;

namespace GannyBot
{
    public class Token
    {
        public string Name;
        public string Symbol;
        public string Address;
        public int Decimals;
        public decimal Balance;
        public string Abi;
        public bool Approved;
        public dynamic Contract;
        public BigDecimal Price = 0;
    }

    public class LimitOrder
    {
        public int ID { get; set; }
        public string Address;
        public string Symbol;
        public string Type;
        public BigDecimal Price;
        public BigDecimal Quantity;
        public decimal Slippage;
        public int GasPrice;
        public DateTime Date;
        public bool Process;
        public int ErrorCount;
    }

    public class LimitOrderList
    {
        public readonly List<LimitOrder> orders;

        public LimitOrderList()
        {
            orders = new List<LimitOrder>();
        }

        public void Add(LimitOrder limitOrder)
        {
            limitOrder.ID = orders.Count > 0 ? orders.Max(x => x.ID) + 1 : 1;
            limitOrder.Date = DateTime.Now;
            limitOrder.Process = false;
            limitOrder.ErrorCount = 5;
            orders.Add(limitOrder);
        }

        public void Remove(LimitOrder limitOrder)
        {
            orders.Remove(limitOrder);
            // orders.ForEach((x) => { if (x.ID > limitOrder.ID) x.ID = x.ID - 1; });
        }

        public int Count()
        {
            return orders.Count;
        }
    }

    internal class Bot
    {
        public static Form1 _form;
        public string WALLET_ADDRESS = Properties.Settings.Default.wallet_address;
        public string WALLET_PRIVATE_KEY = Properties.Settings.Default.wallet_private_key;

        public BlockChain web3 = new BlockChain();
        List<Token> walletTokens = new List<Token>();
        List<Token> limitTokens = new List<Token>();
        LimitOrderList limitOrders = new LimitOrderList();

        System.Threading.Timer timer1;
        System.Threading.Timer timer_wallet;

        string wallet_LastSelectedTokenAddress = null;
        int maxLimitOrderCount = 20;
        int maxUniqueTokenLimitCount = 5;

        public bool Initialize()
        {
            timer1 = new System.Threading.Timer(_ => timer1_Tick(), null, 1000, Timeout.Infinite);
            timer_wallet = new System.Threading.Timer(_ => timer_wallet_Tick(), null, 1000, Timeout.Infinite);

            DeleteAllLimitOrder();

            web3.WALLET_ADDRESS = Properties.Settings.Default.wallet_address;
            web3.WALLET_PRIVATE_KEY = Properties.Settings.Default.wallet_private_key;

            if (web3.Start()) return true;
            else return false;
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

        async private void timer_wallet_Tick()
        {
            if (walletTokens.Count > 0)
            {
                BigDecimal priceETH = await GetWETHPrice_Router();

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

                            if (token.Decimals != 18)
                            {
                                tokenPrice /= Math.Pow(10, 18 - token.Decimals);
                            }
                            
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
                Token newToken = await GetTokenInfo(WALLET_ADDRESS, tokenAddress);
                limitTokens.Add(newToken);
                token = newToken;
            }
            else
            {
                foreach (Token tokenLocal in limitTokens)
                {
                    if (tokenLocal.Address == tokenAddress)
                    {
                        if (limitTokens.Count() >= maxUniqueTokenLimitCount)
                        {
                            response.Error = true;
                            response.Message = "There can be a maximum of " + maxUniqueTokenLimitCount.ToString() + " unique token for limit.";
                            return response;
                        }

                        token = tokenLocal;
                        break;
                    }
                }
            }

            LimitOrder limitOrder = new LimitOrder();
            limitOrder.Address = tokenAddress;
            limitOrder.Symbol = token.Symbol;
            limitOrder.Type = type;
            limitOrder.Price = price;
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
            foreach (LimitOrder order in limitOrders.orders.ToList())
            {
                if (order.Address == address) sameTokenCount++;

                if (order.ID == ID)
                {
                    _form.ListView2_RemoveItem(ID.ToString());
                    limitOrders.Remove(order);
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

        private void DeleteAllLimitOrder()
        {
            foreach (LimitOrder limitOrder in limitOrders.orders.ToList())
            {
                DeleteLimitOrder(limitOrder.ID, limitOrder.Address);
            }
        }

        async public Task<bool> CheckLimitStatus(LimitOrder limitOrder, dynamic transactionReceipt)
        {
            if (transactionReceipt.Error)
            {
                System.Diagnostics.Debug.WriteLine("Error: " + transactionReceipt.Message);
                return false;
            }
            else
            {
                dynamic transactionDetails = await web3.GetTransactionDetails(transactionReceipt.TransactionHash);

                if (transactionDetails.Status == "Successful")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        async private Task LimitTrade(LimitOrder limitOrder, BigDecimal tokenPrice)
        {
            if (limitOrder.Type == "Buy")
            {
                // Buy
                dynamic transactionReceipt = await web3.MakeTradeInput(
                        web3.WALLET_ADDRESS,
                        web3.GetWETHAddress(),
                        limitOrder.Address,
                        limitOrder.Quantity,
                        limitOrder.Slippage,
                        limitOrder.GasPrice
                        );

                if(await CheckLimitStatus(limitOrder, transactionReceipt))
                {
                    DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    System.Diagnostics.Debug.WriteLine("\n- BUY | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Buy Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    limitOrder.Process = false;
                    limitOrder.ErrorCount--;

                    if(limitOrder.ErrorCount <= 0)
                    {
                        DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    }
                }
            }
            else if (limitOrder.Type == "Sell")
            {
                // Sell
                if(!await web3.CheckApprove(WALLET_ADDRESS, limitOrder.Address, await web3.GetTokenAbi(limitOrder.Address)))
                {
                    System.Diagnostics.Debug.WriteLine("\n- Check Approve | " + limitOrder.Symbol);
                    dynamic approveTransactionReceipt = await web3.Approve(
                            limitOrder.Address,
                            limitOrder.Quantity
                        );

                    if(!await CheckLimitStatus(limitOrder, approveTransactionReceipt))
                    {
                        System.Diagnostics.Debug.WriteLine("- Approve False | " + limitOrder.Symbol);
                        limitOrder.Process = false;
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

                System.Diagnostics.Debug.WriteLine("İşlem gönderildi | " + limitOrder.Symbol);
                dynamic transactionReceipt = await web3.MakeTradeInput(
                        web3.WALLET_ADDRESS,
                        limitOrder.Address,
                        web3.GetWETHAddress(),
                        limitOrder.Quantity,
                        limitOrder.Slippage,
                        limitOrder.GasPrice
                        );

                System.Diagnostics.Debug.WriteLine("İşlem alındı | " + limitOrder.Symbol);

                if (await CheckLimitStatus(limitOrder, transactionReceipt))
                {
                    DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    System.Diagnostics.Debug.WriteLine("\n- SELL | " + limitOrder.Symbol);
                    System.Diagnostics.Debug.WriteLine("- Sell Price: " + tokenPrice.ToString() + " | Emir: " + limitOrder.Price.ToString());
                }
                else
                {
                    limitOrder.Process = false;
                    limitOrder.ErrorCount--;

                    System.Diagnostics.Debug.WriteLine("- Error Count: | " + limitOrder.ErrorCount.ToString());

                    if (limitOrder.ErrorCount <= 0)
                    {
                        DeleteLimitOrder(limitOrder.ID, WALLET_ADDRESS);
                    }
                }
            }
        }

        async private void timer1_Tick()
        {
            if (limitTokens.Count > 0)
            {
                //decimal priceETH = await GetWETHPrice_Api();
                BigDecimal priceETH = await GetWETHPrice_Router();
                //System.Diagnostics.Debug.WriteLine("\npriceEth: " + priceETH.ToString());

                foreach (Token limitToken in limitTokens.ToList())
                {
                    BigDecimal tokenPrice = priceETH / await web3.GetEthTokenInputPrice(limitToken.Address, 1);

                    if (limitToken.Decimals != 18)
                    {
                        tokenPrice /= Math.Pow(10, 18 - limitToken.Decimals);
                    }

                    limitToken.Price = tokenPrice;
                    limitToken.Balance = web3.GetTokenBalance(WALLET_ADDRESS, limitToken.Address, await web3.GetTokenAbi(limitToken.Address));
                    _form.ListView2_UpdateTokenPrice(limitToken.Address, tokenPrice);

                    foreach (LimitOrder limitOrder in limitOrders.orders.ToList())
                    {
                        if(!limitOrder.Process && limitOrder.Address == limitToken.Address)
                        {
                            if(limitOrder.Type == "Buy" && limitOrder.Price >= tokenPrice)
                            {
                                limitOrder.Process = true;
                                System.Diagnostics.Debug.WriteLine("-------- Worker Buy");

                                LimitTrade(limitOrder, tokenPrice);
                            }
                            else if(limitOrder.Type == "Sell" && limitOrder.Price <= tokenPrice)
                            {
                                if(limitToken.Balance > 0)
                                {
                                    limitOrder.Process = true;
                                    System.Diagnostics.Debug.WriteLine("-------- Worker Sell");

                                    LimitTrade(limitOrder, tokenPrice);
                                }
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
}
