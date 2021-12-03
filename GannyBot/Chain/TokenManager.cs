using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;

using Newtonsoft.Json;

using Nethereum.Web3;
using Nethereum.Util;

namespace GannyBot.Chain
{
    internal static class TokenManager
    {
        public static async Task<bool> IsToken(string address)
        {
            try
            {
                string baytCode = await Web3Manager.Web3().Eth.GetCode.SendRequestAsync(address);
                //System.Diagnostics.Debug.WriteLine(baytCode);
                if (baytCode == "0x") return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public static async Task<string> GetName(string address, string ABI)
        {
            Nethereum.Contracts.Contract contract = Web3Manager.Web3().Eth.GetContract(ABI, address);
            Nethereum.Contracts.Function nameFunction = contract.GetFunction("name");
            return await nameFunction.CallAsync<string>();
        }

        public static async Task<string> GetSymbol(string address, string ABI)
        {
            Nethereum.Contracts.Contract contract = Web3Manager.Web3().Eth.GetContract(ABI, address);
            Nethereum.Contracts.Function symbolFunction = contract.GetFunction("symbol");
            return await symbolFunction.CallAsync<string>();
        }

        public static async Task<int> GetDecimals(string address, string ABI)
        {
            Nethereum.Contracts.Contract contract = Web3Manager.Web3().Eth.GetContract(ABI, address);
            Nethereum.Contracts.Function decimalsFunction = contract.GetFunction("decimals");
            return await decimalsFunction.CallAsync<int>();
        }

        public static async Task<dynamic> GetAbi(string address)
        {
            string BSC_API_URL = "https://api.bscscan.com/api";
            string BSC_API_KEY = "ZVIBVJRNZ85H69RATX35JAHGFGA8YZ3YGP";

            string STANDART_TOKEN_ABI = "[{'constant':true,'inputs':[],'name':'name','outputs':[{'name':'','type':'string'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_spender','type':'address'},{'name':'_value','type':'uint256'}],'name':'approve','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'totalSupply','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_from','type':'address'},{'name':'_to','type':'address'},{'name':'_value','type':'uint256'}],'name':'transferFrom','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'decimals','outputs':[{'name':'','type':'uint8'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_owner','type':'address'}],'name':'balanceOf','outputs':[{'name':'balance','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'symbol','outputs':[{'name':'','type':'string'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_to','type':'address'},{'name':'_value','type':'uint256'}],'name':'transfer','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[{'name':'_owner','type':'address'},{'name':'_spender','type':'address'}],'name':'allowance','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'payable':true,'stateMutability':'payable','type':'fallback'},{'anonymous':false,'inputs':[{'indexed':true,'name':'owner','type':'address'},{'indexed':true,'name':'spender','type':'address'},{'indexed':false,'name':'value','type':'uint256'}],'name':'Approval','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'from','type':'address'},{'indexed':true,'name':'to','type':'address'},{'indexed':false,'name':'value','type':'uint256'}],'name':'Transfer','type':'event'}]";

            return STANDART_TOKEN_ABI;

            HttpClient client = new HttpClient();

            string API_ENDPOINT = BSC_API_URL + "?module=contract&action=getabi&address=" + address + "&apikey=" + BSC_API_KEY;

            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);
            string contentString = await response.Content.ReadAsStringAsync();
            dynamic parsedJson = JsonConvert.DeserializeObject(contentString);
            string parsedString = parsedJson.ToString();
            return parsedJson;
        }

        public static async Task<BigDecimal> CheckApprove(string walletAddress, string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = Web3Manager.Web3().Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function allowanceFunction = contract.GetFunction("allowance");

            try
            {
                System.Numerics.BigInteger allowance = await allowanceFunction.CallAsync<System.Numerics.BigInteger>(walletAddress, RouterManager.Address());
                return Web3.Convert.FromWeiToBigDecimal(allowance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return 0;
            }
        }

        public static async Task<Trade.OrderToken> GetTokenInfo(string walletAddress, string tokenAddress)
        {
            string tokenAbi = await GetAbi(tokenAddress);

            Trade.OrderToken token = new Trade.OrderToken();
            token.Name = await GetName(tokenAddress, tokenAbi);
            token.Symbol = await GetSymbol(tokenAddress, tokenAbi);
            token.Address = tokenAddress;
            token.Decimals = await GetDecimals(tokenAddress, tokenAbi);
            token.Balance = await WalletManager.GetTokenBalance(walletAddress, tokenAddress, tokenAbi);
            token.Abi = tokenAbi;
            token.Approved = await CheckApprove(walletAddress, tokenAddress, token.Abi);

            return token;
        }

        public static async Task<BigDecimal> GetWeiToEth(string tokenAddress, System.Numerics.BigInteger weiQuantity)
        {
            BigDecimal ethQuantity;

            int decimals = await GetDecimals(tokenAddress, await GetAbi(tokenAddress));
            if (decimals != 18)
            {
                ethQuantity = (BigDecimal)weiQuantity / Math.Pow(10, decimals);
            }
            else
            {
                ethQuantity = Web3.Convert.FromWei(weiQuantity);
            }
            return ethQuantity;
        }

        public static async Task<System.Numerics.BigInteger> GetEthToWei(string tokenAddress, BigDecimal ethQuantity)
        {
            System.Numerics.BigInteger weiQuantity;

            int decimals = await GetDecimals(tokenAddress, await GetAbi(tokenAddress));
            if (decimals != 18)
            {
                weiQuantity = Web3.Convert.ToWei(ethQuantity) / (System.Numerics.BigInteger)Math.Pow(10, 18 - decimals);
            }
            else
            {
                weiQuantity = Web3.Convert.ToWei(ethQuantity);
            }

            return weiQuantity;
        }

        public static async Task<BigDecimal> GetEthTokenInputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountOutFunction = RouterManager.GetAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (Web3.Convert.ToWei(quantity), new List<string>
                            {
                                ChainManager.Token().Address,
                                tokenAddress
                            }
                );

            return await GetWeiToEth(tokenAddress, price[1]);
        }

        public static async Task<BigDecimal> GetTokenEthInputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountOutFunction = RouterManager.GetAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress, quantity),
                    new List<string>
                            {
                                tokenAddress,
                                ChainManager.Token().Address
                            }
                );

            return Web3.Convert.FromWei(price[1]);
        }

        public static async Task<BigDecimal> GetTokenTokenInputPrice(string tokenAddress1, string tokenAddress2, BigDecimal quantity)
        {
            dynamic amountOutFunction = RouterManager.GetAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress1, quantity),
                    new List<string>
                            {
                                tokenAddress1,
                                ChainManager.Token().Address,
                                tokenAddress2
                            }
                );

            return await GetWeiToEth(tokenAddress2, price[1]);
        }

        public static async Task<BigDecimal> GetEthTokenOutputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountInFunction = RouterManager.GetAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress, quantity), new List<string>
                            {
                                ChainManager.Token().Address,
                                tokenAddress
                            }
                );

            return Web3.Convert.FromWei(price[0]);
        }

        public static async Task<BigDecimal> GetTokenEthOutputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountInFunction = RouterManager.GetAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (Web3.Convert.ToWei(quantity), new List<string>
                            {
                                tokenAddress,
                                ChainManager.Token().Address
                            }
                );

            return await GetWeiToEth(tokenAddress, price[0]);
        }

        public static async Task<BigDecimal> GetTokenTokenOutputPrice(string tokenAddress1, string tokenAddress2, BigDecimal quantity)
        {
            dynamic amountInFunction = RouterManager.GetAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress2, quantity), new List<string>
                            {
                                tokenAddress1,
                                ChainManager.Token().Address,
                                tokenAddress2
                            }
                );

            return await GetWeiToEth(tokenAddress1, price[0]);
        }

    }
}
