using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.NonceServices;

namespace GannyBot.Chain
{
    internal static class Web3Manager
    {
        public static Form1 _form1;

        static Web3Base web3 = new Web3Base();

        public static void Start()
        {
            web3.Account = new Account(Wallet.PrivateKey, ChainManager.ChainID());

            web3.Web3 = new Web3(web3.Account, ChainManager.RPCUrl());

            web3.Web3.TransactionManager.UseLegacyAsDefault = true;

            web3.Account.NonceService = new InMemoryNonceService(web3.Account.Address, web3.Web3.Client);

            web3.Connected = true;
        }

        public static Nethereum.Web3.Web3 Web3()
        {
            return web3.Web3;
        }

        public static dynamic Account()
        {
            return web3.Account;
        }

        public static bool IsConnected()
        {
            return web3.Connected;
        }

        public async static Task<bool> CheckConnection()
        {
            try
            {
                await WalletManager.GetWETHBalance(WalletManager.Address());
                web3.Connected = true;
            }
            catch (Exception ex)
            {
                web3.Connected = false;
            }

            _form1.SetWeb3Status();
            return web3.Connected;
        }
    }
}
