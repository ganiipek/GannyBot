using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Dynamic;

using Newtonsoft.Json;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using Nethereum.Hex.HexTypes;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.NonceServices;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;

namespace GannyBot
{
    enum chain
    {
        binance_smart_chain,
        binance_smart_chain_test
    }
    
    class BlockChain
    {
        public Web3 web3;
        Account account;
        public string WALLET_ADDRESS;
        public string WALLET_PRIVATE_KEY;

        string BSC_API_URL = "https://api.bscscan.com/api";
        string BSC_API_KEY = "ZVIBVJRNZ85H69RATX35JAHGFGA8YZ3YGP";

        chain selectedChain;
        Nethereum.Contracts.Contract routerContract;

        public bool Start()
        {
            if (Properties.Settings.Default.chain == "Binance Smart Chain") selectedChain = chain.binance_smart_chain;
            else if (Properties.Settings.Default.chain == "Binance Smart Chain TestNet") selectedChain = chain.binance_smart_chain_test;

            if (!Web3.IsChecksumAddress(WALLET_ADDRESS)) return false;

            account = new Account(WALLET_PRIVATE_KEY, GetChainID());
            web3 = new Web3(account, GetChainRPC());

            web3.TransactionManager.UseLegacyAsDefault = true;

            account.NonceService = new InMemoryNonceService(account.Address, web3.Client);
            routerContract = GetRouterContract();

            return true;
        }

        public void StartOnlyChain()
        {
            web3 = new Web3(GetChainRPC());
        }
        /**********************************************************/
        /************************** MAIN **************************/
        /**********************************************************/
        public string GetChainRPC()
        {
            string rpc = "";
            if (selectedChain == chain.binance_smart_chain)             rpc = "https://bsc-dataseed1.binance.org:443";
            else if (selectedChain == chain.binance_smart_chain_test)   rpc = "https://data-seed-prebsc-1-s1.binance.org:8545/";

            return rpc;
        }

        public Int32 GetChainID()
        {
            Int32 ID = 0;
            if (selectedChain == chain.binance_smart_chain) ID = 0x38;
            else if (selectedChain == chain.binance_smart_chain_test) ID = 0x61;

            return ID;
        }

        public string GetChainName()
        {
            string name = "";
            if (selectedChain == chain.binance_smart_chain) name = "Smart Chain";
            else if (selectedChain == chain.binance_smart_chain_test) name = "Smart Chain Test";

            return name;
        }

        public string GetExplorerURL()
        {
            string url = "";
            if (selectedChain == chain.binance_smart_chain) url = "https://bscscan.com/";
            else if (selectedChain == chain.binance_smart_chain_test) url = "https://testnet.bscscan.com/";

            return url;
        }

        public string GetMainTokenName()
        {
            string name = "";
            if (selectedChain == chain.binance_smart_chain) name = "BNB";
            else if (selectedChain == chain.binance_smart_chain_test) name = "BNB";

            return name;
        }

        public async Task<HexBigInteger> GetBlockNumber()
        {
            return await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        }

        public string GetWETHAddress()
        {
            string address = "";

            if (selectedChain == chain.binance_smart_chain) address = "0xbb4cdb9cbd36b01bd1cbaebf2de08d9173bc095c";
            else if (selectedChain == chain.binance_smart_chain_test) address = "0xae13d989dac2f0debff460ac112a837c89baa7cd";

            return address;
        }

        public string GetUSDTAddress()
        {
            string address = "";

            if (selectedChain == chain.binance_smart_chain) address = "0x55d398326f99059ff775485246999027b3197955";
            else if (selectedChain == chain.binance_smart_chain_test) address = "0x337610d27c682E347C9cD60BD4b3b107C9d34dDd";

            return address;
        }

        public string GetRouterAddress()
        {
            string address = "";

            if (selectedChain == chain.binance_smart_chain) address = "0x10ED43C718714eb63d5aA57B78B54704E256024E";
            else if (selectedChain == chain.binance_smart_chain_test) address = "0xD99D1c33F9fC3444f8101754aBC46c52416550D1";

            return address;
        }

        public string GetRouterABI()
        {
            return "[{'inputs':[{'internalType':'address','name':'_factory','type':'address'},{'internalType':'address','name':'_WETH','type':'address'}],'stateMutability':'nonpayable','type':'constructor'},{'inputs':[],'name':'WETH','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'address','name':'tokenA','type':'address'},{'internalType':'address','name':'tokenB','type':'address'},{'internalType':'uint256','name':'amountADesired','type':'uint256'},{'internalType':'uint256','name':'amountBDesired','type':'uint256'},{'internalType':'uint256','name':'amountAMin','type':'uint256'},{'internalType':'uint256','name':'amountBMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'addLiquidity','outputs':[{'internalType':'uint256','name':'amountA','type':'uint256'},{'internalType':'uint256','name':'amountB','type':'uint256'},{'internalType':'uint256','name':'liquidity','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'uint256','name':'amountTokenDesired','type':'uint256'},{'internalType':'uint256','name':'amountTokenMin','type':'uint256'},{'internalType':'uint256','name':'amountETHMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'addLiquidityETH','outputs':[{'internalType':'uint256','name':'amountToken','type':'uint256'},{'internalType':'uint256','name':'amountETH','type':'uint256'},{'internalType':'uint256','name':'liquidity','type':'uint256'}],'stateMutability':'payable','type':'function'},{'inputs':[],'name':'factory','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'},{'internalType':'uint256','name':'reserveIn','type':'uint256'},{'internalType':'uint256','name':'reserveOut','type':'uint256'}],'name':'getAmountIn','outputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'}],'stateMutability':'pure','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'uint256','name':'reserveIn','type':'uint256'},{'internalType':'uint256','name':'reserveOut','type':'uint256'}],'name':'getAmountOut','outputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'}],'stateMutability':'pure','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'}],'name':'getAmountsIn','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'}],'name':'getAmountsOut','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountA','type':'uint256'},{'internalType':'uint256','name':'reserveA','type':'uint256'},{'internalType':'uint256','name':'reserveB','type':'uint256'}],'name':'quote','outputs':[{'internalType':'uint256','name':'amountB','type':'uint256'}],'stateMutability':'pure','type':'function'},{'inputs':[{'internalType':'address','name':'tokenA','type':'address'},{'internalType':'address','name':'tokenB','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountAMin','type':'uint256'},{'internalType':'uint256','name':'amountBMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'removeLiquidity','outputs':[{'internalType':'uint256','name':'amountA','type':'uint256'},{'internalType':'uint256','name':'amountB','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountTokenMin','type':'uint256'},{'internalType':'uint256','name':'amountETHMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'removeLiquidityETH','outputs':[{'internalType':'uint256','name':'amountToken','type':'uint256'},{'internalType':'uint256','name':'amountETH','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountTokenMin','type':'uint256'},{'internalType':'uint256','name':'amountETHMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'removeLiquidityETHSupportingFeeOnTransferTokens','outputs':[{'internalType':'uint256','name':'amountETH','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountTokenMin','type':'uint256'},{'internalType':'uint256','name':'amountETHMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'},{'internalType':'bool','name':'approveMax','type':'bool'},{'internalType':'uint8','name':'v','type':'uint8'},{'internalType':'bytes32','name':'r','type':'bytes32'},{'internalType':'bytes32','name':'s','type':'bytes32'}],'name':'removeLiquidityETHWithPermit','outputs':[{'internalType':'uint256','name':'amountToken','type':'uint256'},{'internalType':'uint256','name':'amountETH','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountTokenMin','type':'uint256'},{'internalType':'uint256','name':'amountETHMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'},{'internalType':'bool','name':'approveMax','type':'bool'},{'internalType':'uint8','name':'v','type':'uint8'},{'internalType':'bytes32','name':'r','type':'bytes32'},{'internalType':'bytes32','name':'s','type':'bytes32'}],'name':'removeLiquidityETHWithPermitSupportingFeeOnTransferTokens','outputs':[{'internalType':'uint256','name':'amountETH','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'tokenA','type':'address'},{'internalType':'address','name':'tokenB','type':'address'},{'internalType':'uint256','name':'liquidity','type':'uint256'},{'internalType':'uint256','name':'amountAMin','type':'uint256'},{'internalType':'uint256','name':'amountBMin','type':'uint256'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'},{'internalType':'bool','name':'approveMax','type':'bool'},{'internalType':'uint8','name':'v','type':'uint8'},{'internalType':'bytes32','name':'r','type':'bytes32'},{'internalType':'bytes32','name':'s','type':'bytes32'}],'name':'removeLiquidityWithPermit','outputs':[{'internalType':'uint256','name':'amountA','type':'uint256'},{'internalType':'uint256','name':'amountB','type':'uint256'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapETHForExactTokens','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'payable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactETHForTokens','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'payable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactETHForTokensSupportingFeeOnTransferTokens','outputs':[],'stateMutability':'payable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactTokensForETH','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactTokensForETHSupportingFeeOnTransferTokens','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactTokensForTokens','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountIn','type':'uint256'},{'internalType':'uint256','name':'amountOutMin','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapExactTokensForTokensSupportingFeeOnTransferTokens','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'},{'internalType':'uint256','name':'amountInMax','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapTokensForExactETH','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'uint256','name':'amountOut','type':'uint256'},{'internalType':'uint256','name':'amountInMax','type':'uint256'},{'internalType':'address[]','name':'path','type':'address[]'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'deadline','type':'uint256'}],'name':'swapTokensForExactTokens','outputs':[{'internalType':'uint256[]','name':'amounts','type':'uint256[]'}],'stateMutability':'nonpayable','type':'function'},{'stateMutability':'payable','type':'receive'}]";
        }

        public Nethereum.Contracts.Contract GetRouterContract()
        {
            return web3.Eth.GetContract(GetRouterABI(), GetRouterAddress());
        }

        /**********************************************************/
        /******************** TOKEN INFORMATION *******************/
        /**********************************************************/
        public async Task<bool> CheckTokenAddress(string address)
        {
            try
            {
                string baytCode = await web3.Eth.GetCode.SendRequestAsync(address);
                //System.Diagnostics.Debug.WriteLine(baytCode);
                if (baytCode == "0x") return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public async Task<string> GetTokenName(string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = web3.Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function nameFunction = contract.GetFunction("name");
            return await nameFunction.CallAsync<string>();
        }

        public async Task<string> GetTokenSymbol(string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = web3.Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function symbolFunction = contract.GetFunction("symbol");
            return await symbolFunction.CallAsync<string>();
        }

        public async Task<int> GetTokenDecimals(string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = web3.Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function decimalsFunction = contract.GetFunction("decimals");
            return await decimalsFunction.CallAsync<int>();
        }

        public async Task<dynamic> GetTokenAbi(string token_address)
        {
            string STANDART_TOKEN_ABI = "[{'constant':true,'inputs':[],'name':'name','outputs':[{'name':'','type':'string'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_spender','type':'address'},{'name':'_value','type':'uint256'}],'name':'approve','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'totalSupply','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_from','type':'address'},{'name':'_to','type':'address'},{'name':'_value','type':'uint256'}],'name':'transferFrom','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[],'name':'decimals','outputs':[{'name':'','type':'uint8'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[{'name':'_owner','type':'address'}],'name':'balanceOf','outputs':[{'name':'balance','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':true,'inputs':[],'name':'symbol','outputs':[{'name':'','type':'string'}],'payable':false,'stateMutability':'view','type':'function'},{'constant':false,'inputs':[{'name':'_to','type':'address'},{'name':'_value','type':'uint256'}],'name':'transfer','outputs':[{'name':'','type':'bool'}],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':true,'inputs':[{'name':'_owner','type':'address'},{'name':'_spender','type':'address'}],'name':'allowance','outputs':[{'name':'','type':'uint256'}],'payable':false,'stateMutability':'view','type':'function'},{'payable':true,'stateMutability':'payable','type':'fallback'},{'anonymous':false,'inputs':[{'indexed':true,'name':'owner','type':'address'},{'indexed':true,'name':'spender','type':'address'},{'indexed':false,'name':'value','type':'uint256'}],'name':'Approval','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'from','type':'address'},{'indexed':true,'name':'to','type':'address'},{'indexed':false,'name':'value','type':'uint256'}],'name':'Transfer','type':'event'}]";

            return STANDART_TOKEN_ABI;

            HttpClient client = new HttpClient();

            string API_ENDPOINT = BSC_API_URL + "?module=contract&action=getabi&address=" + token_address + "&apikey=" + BSC_API_KEY;

            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);
            string contentString = await response.Content.ReadAsStringAsync();
            dynamic parsedJson = JsonConvert.DeserializeObject(contentString);
            string parsedString = parsedJson.ToString();
            return parsedJson;
        }

        /**********************************************************/
        /************************* WALLET *************************/
        /**********************************************************/
        public bool CheckWalletAddress(string walletAddress)
        {
            return Web3.IsChecksumAddress(walletAddress);
        }

        public bool ControlPrivateKey(string walletAddress, string privateKey)
        {
            Account local_account = new Account(privateKey, GetChainID());

            if (local_account.Address == walletAddress)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<decimal> GetEthBalance(string walletAddress)
        {
            HexBigInteger weiBalance = await web3.Eth.GetBalance.SendRequestAsync(walletAddress);
            return Web3.Convert.FromWei(weiBalance.Value);
        }

        public async Task<decimal> GetTokenBalance(string walletAddress, string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = web3.Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function balanceFunction = contract.GetFunction("balanceOf");
            Nethereum.Contracts.Function decimalFunction = contract.GetFunction("decimals");

            System.Numerics.BigInteger balance = await balanceFunction.CallAsync<System.Numerics.BigInteger>(walletAddress);
            int decimals = await decimalFunction.CallAsync<int>();
            return (decimal)((double)balance / Math.Pow(10, decimals));
        }

        /**********************************************************/
        /************************* APPROVE ************************/
        /**********************************************************/
        public async Task<BigDecimal> CheckApprove(string walletAddress, string tokenAddress, string tokenABI)
        {
            Nethereum.Contracts.Contract contract = web3.Eth.GetContract(tokenABI, tokenAddress);
            Nethereum.Contracts.Function allowanceFunction = contract.GetFunction("allowance");

            try
            {
                System.Numerics.BigInteger allowance = await allowanceFunction.CallAsync<System.Numerics.BigInteger>(walletAddress, GetRouterAddress());
                return Web3.Convert.FromWeiToBigDecimal(allowance);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<dynamic> Approve(string tokenAddress, BigDecimal quantity)
        {
            var approveHandler = web3.Eth.GetContractTransactionHandler<ApproveFunction>();
            var approveDTO = new ApproveFunction()
            {
                Spender = GetRouterAddress(),
                Value = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether)
            };

            return await BuildAndSendTx(tokenAddress, approveHandler, approveDTO);
        }

        /**********************************************************/
        /************************* MARKET *************************/
        /**********************************************************/
        #region MARKET 
        Nethereum.Contracts.Function getAmountInFunction()
        {
            return routerContract.GetFunction("getAmountsIn");
        }

        Nethereum.Contracts.Function getAmountOutFunction()
        {
            return routerContract.GetFunction("getAmountsOut");
        }

        async Task<BigDecimal> GetWeiToEth(string tokenAddress, System.Numerics.BigInteger weiQuantity)
        {
            BigDecimal ethQuantity;

            int decimals = await GetTokenDecimals(tokenAddress, await GetTokenAbi(tokenAddress));
            if (decimals != 18)
            {
                ethQuantity = (BigDecimal) weiQuantity / Math.Pow(10, decimals);
            }
            else
            {
                ethQuantity = Web3.Convert.FromWei(weiQuantity);
            }
            return ethQuantity;
        }

        async Task<System.Numerics.BigInteger> GetEthToWei(string tokenAddress, BigDecimal ethQuantity)
        {
            System.Numerics.BigInteger weiQuantity;

            int decimals = await GetTokenDecimals(tokenAddress, await GetTokenAbi(tokenAddress));
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

        public async Task<BigDecimal> GetEthTokenInputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountOutFunction = getAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (Web3.Convert.ToWei(quantity), new List<string>
                            {
                                GetWETHAddress(),
                                tokenAddress
                            }
                );

            return await GetWeiToEth(tokenAddress, price[1]);
        }

        public async Task<BigDecimal> GetTokenEthInputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountOutFunction = getAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress, quantity), 
                    new List<string>
                            {
                                tokenAddress,
                                GetWETHAddress()
                            }
                );

            return Web3.Convert.FromWei(price[1]);
        }

        public async Task<BigDecimal> GetTokenTokenInputPrice(string tokenAddress1, string tokenAddress2, BigDecimal quantity)
        {
            dynamic amountOutFunction = getAmountOutFunction();
            List<System.Numerics.BigInteger> price = await amountOutFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress1, quantity), 
                    new List<string>
                            {
                                tokenAddress1,
                                GetWETHAddress(),
                                tokenAddress2
                            }
                );

            return await GetWeiToEth(tokenAddress2, price[1]);
        }

        public async Task<BigDecimal> GetEthTokenOutputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountInFunction = getAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress, quantity), new List<string>
                            {
                                GetWETHAddress(),
                                tokenAddress
                            }
                );

            return Web3.Convert.FromWei(price[0]);
        }

        public async Task<BigDecimal> GetTokenEthOutputPrice(string tokenAddress, BigDecimal quantity)
        {
            dynamic amountInFunction = getAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (Web3.Convert.ToWei(quantity), new List<string>
                            {
                                tokenAddress,
                                GetWETHAddress()
                            }
                );

            return await GetWeiToEth(tokenAddress, price[0]);
        }

        public async Task<BigDecimal> GetTokenTokenOutputPrice(string tokenAddress1, string tokenAddress2, BigDecimal quantity)
        {
            dynamic amountInFunction = getAmountInFunction();
            List<System.Numerics.BigInteger> price = await amountInFunction.CallAsync<List<System.Numerics.BigInteger>>
                (await GetEthToWei(tokenAddress2, quantity), new List<string>
                            {
                                tokenAddress1,
                                GetWETHAddress(),
                                tokenAddress2
                            }
                );

            return await GetWeiToEth(tokenAddress1, price[0]);
        }

        #endregion
        /**********************************************************/
        /************************* TRADE **************************/
        /**********************************************************/
        async Task<dynamic> GetEstimated(dynamic handler, dynamic transfer)
        {
            dynamic response = new ExpandoObject();

            try
            {
                dynamic estimate = await handler.EstimateGasAsync(GetRouterAddress(), transfer);
                string str = estimate.ToString();

                response.Error = false;
                response.Gas = estimate.Value;
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.Message = ex.Message;
            }
            
            return response;
        }

        public async Task<dynamic> MakeTradeInput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            try
            {
                if (inputTokenAddress == GetWETHAddress())
                {
                    return await EthToTokenSwapInput(walletAddress, outputTokenAddress, quantity, slippage, gasPrice);
                }
                else
                {
                    string inputTokenABI = await GetTokenAbi(inputTokenAddress);
                    decimal balance = await GetTokenBalance(walletAddress, inputTokenAddress, inputTokenABI);

                    // if (balance < quantity)  hata

                    if (outputTokenAddress == GetWETHAddress())
                    {
                        return await TokenToEthSwapInput(walletAddress, inputTokenAddress, quantity, slippage, gasPrice);
                    }
                    else
                    {
                        return await TokenToTokenSwapInput(walletAddress, inputTokenAddress, outputTokenAddress, quantity, slippage, gasPrice);
                    }
                }
            }
            catch(Exception ex)
            {
                dynamic responseMessage = new ExpandoObject();
                responseMessage.Error = true;
                responseMessage.Message = ex.Message;
                return responseMessage;
            }
        }

        public async Task<dynamic> MakeTradeOutput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            if (inputTokenAddress == GetWETHAddress())
            {
                decimal balance = await GetEthBalance(walletAddress);
                BigDecimal need = await GetEthTokenOutputPrice(outputTokenAddress, quantity);

                // if (balance < need) hata

                return await EthToTokenSwapOutput(walletAddress, outputTokenAddress, quantity, slippage, gasPrice);
            }
            else
            {
                if (outputTokenAddress == GetWETHAddress())
                {
                    return await TokenToEthSwapOutput(walletAddress, inputTokenAddress, quantity, slippage, gasPrice);
                }
                else
                {
                    return await TokenToTokenSwapOutput(walletAddress, inputTokenAddress, outputTokenAddress, quantity, slippage, gasPrice);
                }
            }
        }
        
        async Task<dynamic> EthToTokenSwapInput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await GetEthTokenInputPrice(tokenAddress, quantity);

            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapExactETHForTokensSupportingFeeOnTransferTokensFunction>();
            var swapDTO = new SwapExactETHForTokensSupportingFeeOnTransferTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountOutMin = await GetEthToWei(tokenAddress, amountOutMin),
                Path = new List<string>
                {
                    GetWETHAddress(),
                    tokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei),
                Nonce = await GetTxNonce()
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);

            if (estimatedGas.Error) return estimatedGas;
            swapDTO.Gas = estimatedGas.Gas * 2;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        async Task<dynamic> TokenToEthSwapInput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await GetTokenEthInputPrice(tokenAddress, quantity);
            
            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapExactTokensForETHSupportingFeeOnTransferTokensFunction>();
            var swapDTO = new SwapExactTokensForETHSupportingFeeOnTransferTokensFunction()
            {
                AmountIn = await GetEthToWei(tokenAddress, quantity),
                AmountOutMin = Web3.Convert.ToWei(amountOutMin, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    tokenAddress,
                    GetWETHAddress()
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei),
                Nonce = await GetTxNonce()
            };
            
            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);

            string json = JsonConvert.SerializeObject(estimatedGas);
            System.Diagnostics.Debug.WriteLine(json);

            if (estimatedGas.Error) return estimatedGas;
            swapDTO.Gas = estimatedGas.Gas * 2;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        async Task<dynamic> TokenToTokenSwapInput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal minTokensBought = ((100 - slippage) / 100) * await GetTokenTokenInputPrice(inputTokenAddress, outputTokenAddress, quantity);

            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapExactTokensForTokensFunction>();
            var swapDTO = new SwapExactTokensForTokensFunction()
            {
                AmountIn = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountOutMin = Web3.Convert.ToWei(minTokensBought, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    inputTokenAddress,
                    GetWETHAddress(),
                    outputTokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                Gas = GetTxGas(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        async Task<dynamic> EthToTokenSwapOutput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal eth_qty = await GetEthTokenOutputPrice(tokenAddress, quantity);
            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapETHForExactTokensFunction>();
            var swapDTO = new SwapETHForExactTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(eth_qty, UnitConversion.EthUnit.Ether),
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    GetWETHAddress(),
                    tokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                Gas = GetTxGas(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        async Task<dynamic> TokenToEthSwapOutput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal maxTokens = ((100 + slippage) / 100) * await GetTokenEthOutputPrice(tokenAddress, quantity);
            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapTokensForExactETHFunction>();
            var swapDTO = new SwapTokensForExactETHFunction()
            {
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountInMax = Web3.Convert.ToWei(maxTokens, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    tokenAddress,
                    GetWETHAddress()
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                Gas = GetTxGas(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        async Task<dynamic> TokenToTokenSwapOutput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountInMax = ((100 + slippage) / 100) * await GetTokenTokenOutputPrice(inputTokenAddress, outputTokenAddress, quantity);

            var swapHandler = web3.Eth.GetContractTransactionHandler<SwapTokensForExactTokensFunction>();
            var swapDTO = new SwapTokensForExactTokensFunction()
            {
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountInMax = Web3.Convert.ToWei(amountInMax, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    inputTokenAddress,
                    GetWETHAddress(),
                    outputTokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                Gas = GetTxGas(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            return await BuildAndSendTx(GetRouterAddress(), swapHandler, swapDTO);
        }

        /**********************************************************/
        /************************ TX UTILS ************************/
        /**********************************************************/
        System.Numerics.BigInteger GetTxDeadline()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + 1200;
        }

        System.Numerics.BigInteger GetTxGas()
        {
            return 1000000;
        }

        async Task<dynamic>GetTxNonce()
        {
            return await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address, BlockParameter.CreatePending());
        }

        async Task<dynamic> BuildAndSendTx(string contractAddress, dynamic swapHandler, dynamic swapDTO)
        {
            dynamic responseMessage = new ExpandoObject();
            try
            {
                var transactionSwapReceipt = await swapHandler.SendRequestAndWaitForReceiptAsync(contractAddress, swapDTO);

                responseMessage.Error = false;
                responseMessage.Status = transactionSwapReceipt.Status.Value;
                responseMessage.Message = "";
                responseMessage.TransactionHash = transactionSwapReceipt.TransactionHash;
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(error.Message);
                responseMessage.Error = true;
                responseMessage.Message = error.Message;
            }

            return responseMessage;
        }

        public async Task<dynamic> GetTransactionDetails(string transactionHash)
        {
            var transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            var transaction = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);

            //string str = JsonConvert.SerializeObject(transactionReceipt);
            //System.Diagnostics.Debug.WriteLine(str);

            //string str2 = JsonConvert.SerializeObject(transaction);
            //System.Diagnostics.Debug.WriteLine(str2);

            dynamic transactionDetails = new ExpandoObject();
            transactionDetails.Hash = transactionHash;

            if (transactionReceipt.Status.ToString() == "1") transactionDetails.Status = "Successful";
            else transactionDetails.Status = "Failed";

            transactionDetails.From = transaction.From;
            transactionDetails.To = transaction.To;
            transactionDetails.GasLimit = transaction.Gas.ToString();
            transactionDetails.GasUsed = transactionReceipt.GasUsed.ToString();
            transactionDetails.GasPrice = Web3.Convert.FromWei(transaction.GasPrice, UnitConversion.EthUnit.Gwei).ToString();
            transactionDetails.TotalFee = Web3.Convert.FromWei(transactionReceipt.GasUsed.ToUlong() * transaction.GasPrice.ToUlong(), Nethereum.Util.UnitConversion.EthUnit.Ether).ToString();
            transactionDetails.Value = Web3.Convert.FromWei(transaction.Value.ToUlong(), UnitConversion.EthUnit.Ether).ToString();

            return transactionDetails;
        }
    }
}