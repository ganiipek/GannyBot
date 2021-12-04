using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace GannyBot.Chain
{
    internal static class WalletManager
    {
        public static Database.DatabaseManager database = new Database.DatabaseManager();

        public static bool Set(string address, string privateKey, Int32 ChainID)
        {
            if (Check(address, privateKey, ChainID))
            {
                Wallet.Address = address;
                Wallet.PrivateKey = privateKey;

                return true;
            }
            return false;
        }

        public static bool Check(string address, string privateKey, Int32 ChainID)
        {
            Account local_account = new Account(privateKey, ChainID);

            if (local_account.Address == address)
            {
                return true;
            }
            return false;
        }

        public static bool CheckWalletAddress(string walletAddress)
        {
            return Web3.IsChecksumAddress(walletAddress);
        }

        public static string Address()
        {
            return Wallet.Address;
        }

        public static string Key()
        {
            return Wallet.PrivateKey;
        }

        public static async Task<decimal> GetWETHBalance(string address)
        {
            HexBigInteger weiBalance = await Web3Manager.Web3().Eth.GetBalance.SendRequestAsync(address);
            return Web3.Convert.FromWei(weiBalance.Value);
        }

        public static async Task<decimal> GetTokenBalance(string walletAddress, string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = Web3Manager.Web3().Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function balanceFunction = contract.GetFunction("balanceOf");
            Nethereum.Contracts.Function decimalFunction = contract.GetFunction("decimals");

            System.Numerics.BigInteger balance = await balanceFunction.CallAsync<System.Numerics.BigInteger>(walletAddress);
            int decimals = await decimalFunction.CallAsync<int>();
            return (decimal)((double)balance / Math.Pow(10, decimals));
        }

        public static async Task<HexBigInteger> GetNonce()
        {
            return await Web3Manager.Web3().Eth.Transactions.GetTransactionCount.SendRequestAsync(Web3Manager.Account().Address, BlockParameter.CreatePending());
        }

    }
}
