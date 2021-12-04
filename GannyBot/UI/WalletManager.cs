using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GannyBot.UI
{
    class WalletManager
    {
        public static Form1 _form1;

        List<Chain.Token> walletTokens = new List<Chain.Token>();
        string wallet_LastSelectedTokenAddress = null;

        System.Threading.Timer timer_wallet;

        public WalletManager()
        {
            timer_wallet = new System.Threading.Timer(_ => timer_wallet_Tick(), null, 2500, Timeout.Infinite);

            ReloadToken();
        }

        void ReloadToken()
        {
            dynamic dbWalletTokens = UIManager.database.GetWalletTokens();

            foreach (var dbToken in dbWalletTokens)
            {
                Chain.Token token = new Chain.Token
                {
                    Name = dbToken.name.ToString(),
                    Symbol = dbToken.symbol.ToString(),
                    Address = dbToken.address.ToString(),
                    Decimals = dbToken.decimals,
                    Abi = dbToken.abi.ToString()
                };

                walletTokens.Add(token);
                _form1.Wallet_ListViewAddItem(token);
            }
        }

        public async Task<dynamic> AddToken(string tokenAddress)
        {
            dynamic response = new ExpandoObject();
            if (!await Chain.TokenManager.IsToken(tokenAddress))
            {
                response.Error = true;
                response.Message = "Invalid token address";
                return response;
            }
            else if (walletTokens.Exists(x => x.Address == tokenAddress))
            {
                response.Error = true;
                response.Message = "Token zaten ekli";
                return response;
            }

            Trade.OrderToken token = await Chain.TokenManager.GetTokenInfo(Chain.WalletManager.Address(), tokenAddress);
            walletTokens.Add(token);

            response.Error = false;
            response.Token = token;
            return response;
        }

        public void RemoveToken(string tokenAddress)
        {
            foreach (Trade.OrderToken token in walletTokens)
            {
                if (token.Address == tokenAddress)
                {
                    walletTokens.Remove(token);
                    break;
                }
            }
        }

        public void ShowSelectedToken(string address)
        {
            foreach (Chain.Token token in walletTokens.ToList())
            {
                if (token.Address == address)
                {
                    _form1.Wallet_ShowTokenInfo(token);
                    break;
                }
            }
        }

        async void timer_wallet_Tick()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                if (walletTokens.Count > 0 && _form1.CheckSelectedMainTab("mainTab_wallet"))
                {
                    string address = _form1.Wallet_ListViewSelectedItem();

                    if (address != null || wallet_LastSelectedTokenAddress != null)
                    {
                        if (address == null) address = wallet_LastSelectedTokenAddress;
                        wallet_LastSelectedTokenAddress = address;

                        foreach (Chain.Token token in walletTokens.ToList())
                        {
                            if (token.Address == address)
                            {
                                try
                                {
                                    BigDecimal tokenPrice = Chain.ChainManager.Token().Price / await Chain.TokenManager.GetEthTokenInputPrice(token.Address, 1);

                                    token.Price = tokenPrice.RoundAwayFromZero(10);
                                    token.Balance = await Chain.WalletManager.GetTokenBalance(Chain.WalletManager.Address(), token.Address, token.Abi);
                                    _form1.Wallet_ShowTokenInfo(token);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("timer_wallet_Tick: " + ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            Thread.Sleep(1000);
            timer_wallet.Change(1000, Timeout.Infinite);
        }
    }
}
