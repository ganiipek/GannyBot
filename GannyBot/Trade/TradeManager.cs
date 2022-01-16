using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;

using Newtonsoft.Json;

using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using Nethereum.StandardTokenEIP20.ContractDefinition;

namespace GannyBot.Trade
{
    internal static class TradeManager
    {
        static async Task<dynamic> GetEstimated(dynamic handler, dynamic transfer)
        {
            dynamic response = new ExpandoObject();
            
            try
            { 
                dynamic estimate = await handler.EstimateGasAsync(Chain.RouterManager.Address(), transfer);
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

        public static int GetTxGas(dynamic estimatedGas)
        {
            return (int)estimatedGas.Gas * 2;
        }

        static System.Numerics.BigInteger GetTxDeadline()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + 1200;
        }

        static async Task<dynamic> GetTxNonce()
        {
            return await Chain.WalletManager.GetNonce();
        }

        public static async Task<dynamic> Approve(string tokenAddress, BigDecimal quantity)
        {
            var approveHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<ApproveFunction>();
            var approveDTO = new ApproveFunction()
            {
                Spender = Chain.RouterManager.Address(),
                Value = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether)
            };

            return await BuildAndSendTx(tokenAddress, approveHandler, approveDTO);
        }

        public static async Task<dynamic> CheckTradeInput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic responseMessage = new ExpandoObject();
            try
            {
                dynamic swapHandler;
                dynamic swapDTO;

                if (inputTokenAddress == Chain.ChainManager.Token().Address)
                {
                    (swapHandler, swapDTO) = await EthToTokenSwapInput(walletAddress, outputTokenAddress, quantity, slippage, gasPrice);
                }
                else
                {
                    string inputTokenABI = await Chain.TokenManager.GetAbi(inputTokenAddress);
                    decimal balance = await Chain.WalletManager.GetTokenBalance(walletAddress, inputTokenAddress, inputTokenABI);

                    // if (balance < quantity)  hata

                    if (outputTokenAddress == Chain.ChainManager.Token().Address)
                    {
                        (swapHandler, swapDTO) = await TokenToEthSwapInput(walletAddress, inputTokenAddress, quantity, slippage, gasPrice);
                    }
                    else
                    {
                        (swapHandler, swapDTO) = await TokenToTokenSwapInput(walletAddress, inputTokenAddress, outputTokenAddress, quantity, slippage, gasPrice);
                    }
                }

                dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
                //string json = JsonConvert.SerializeObject(estimatedGas);
                //System.Diagnostics.Debug.WriteLine(json);

                if (estimatedGas.Error) return estimatedGas;
                swapDTO.Gas = GetTxGas(estimatedGas);

                responseMessage.Error = false;
                responseMessage.swapHandler = swapHandler;
                responseMessage.swapDTO = swapDTO;
                return responseMessage;
            }
            catch (Exception ex)
            {
                responseMessage.Error = true;
                responseMessage.Message = "asdada:" + ex.Message;
                return responseMessage;
            }
        }

        public static async Task<dynamic> MakeTradeInput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            dynamic response = await CheckTradeInput(walletAddress, inputTokenAddress, outputTokenAddress, quantity, slippage, gasPrice);
            if(response.Error) return response;

            return await BuildAndSendTx(Chain.RouterManager.Address(), response.swapHandler, response.swapDTO);
        }

        public static async Task<dynamic> MakeTradeOutput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            if (inputTokenAddress == Chain.ChainManager.Token().Address)
            {
                decimal balance = await Chain.WalletManager.GetWETHBalance(walletAddress);
                BigDecimal need = await Chain.TokenManager.GetEthTokenOutputPrice(outputTokenAddress, quantity);

                // if (balance < need) hata

                return await EthToTokenSwapOutput(walletAddress, outputTokenAddress, quantity, slippage, gasPrice);
            }
            else
            {
                if (outputTokenAddress == Chain.ChainManager.Token().Address)
                {
                    return await TokenToEthSwapOutput(walletAddress, inputTokenAddress, quantity, slippage, gasPrice);
                }
                else
                {
                    return await TokenToTokenSwapOutput(walletAddress, inputTokenAddress, outputTokenAddress, quantity, slippage, gasPrice);
                }
            }
        }

        static async Task<dynamic> BuildAndSendTx(string contractAddress, dynamic swapHandler, dynamic swapDTO)
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

        static async Task<(dynamic swapHandler, dynamic swapDTO)> EthToTokenSwapInput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await Chain.TokenManager.GetEthTokenInputPrice(tokenAddress, quantity);

            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapExactETHForTokensSupportingFeeOnTransferTokensFunction>();
            var swapDTO = new SwapExactETHForTokensSupportingFeeOnTransferTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountOutMin = await Chain.TokenManager.GetEthToWei(tokenAddress, amountOutMin),
                Path = new List<string>
                {
                    Chain.ChainManager.Token().Address,
                    tokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei),
                Nonce = await GetTxNonce()
            };

            return (swapHandler, swapDTO);
        }

        static async Task<(dynamic swapHandler, dynamic swapDTO)> TokenToEthSwapInput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await Chain.TokenManager.GetTokenEthInputPrice(tokenAddress, quantity);

            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapExactTokensForETHSupportingFeeOnTransferTokensFunction>();
            var swapDTO = new SwapExactTokensForETHSupportingFeeOnTransferTokensFunction()
            {
                AmountIn = await Chain.TokenManager.GetEthToWei(tokenAddress, quantity),
                AmountOutMin = Web3.Convert.ToWei(amountOutMin, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    tokenAddress,
                    Chain.ChainManager.Token().Address
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei),
                Nonce = await GetTxNonce()
            };

            return (swapHandler, swapDTO);
        }

        static async Task<(dynamic swapHandler, dynamic swapDTO)> TokenToTokenSwapInput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal minTokensBought = ((100 - slippage) / 100) * await Chain.TokenManager.GetTokenTokenInputPrice(inputTokenAddress, outputTokenAddress, quantity);

            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapExactTokensForTokensFunction>();
            var swapDTO = new SwapExactTokensForTokensFunction()
            {
                AmountIn = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountOutMin = Web3.Convert.ToWei(minTokensBought, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    inputTokenAddress,
                    Chain.ChainManager.Token().Address,
                    outputTokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            return (swapHandler, swapDTO);
        }

        static async Task<dynamic> EthToTokenSwapOutput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal eth_qty = await Chain.TokenManager.GetEthTokenOutputPrice(tokenAddress, quantity);
            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapETHForExactTokensFunction>();
            var swapDTO = new SwapETHForExactTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(eth_qty, UnitConversion.EthUnit.Ether),
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    Chain.ChainManager.Token().Address,
                    tokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            swapDTO.Gas = GetTxGas(estimatedGas);

            return await BuildAndSendTx(Chain.RouterManager.Address(), swapHandler, swapDTO);
        }

        static async Task<dynamic> TokenToEthSwapOutput(string walletAddress, string tokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal maxTokens = ((100 + slippage) / 100) * await Chain.TokenManager.GetTokenEthOutputPrice(tokenAddress, quantity);
            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapTokensForExactETHFunction>();
            var swapDTO = new SwapTokensForExactETHFunction()
            {
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountInMax = Web3.Convert.ToWei(maxTokens, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    tokenAddress,
                    Chain.ChainManager.Token().Address
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            swapDTO.Gas = GetTxGas(estimatedGas);

            return await BuildAndSendTx(Chain.RouterManager.Address(), swapHandler, swapDTO);
        }

        static async Task<dynamic> TokenToTokenSwapOutput(string walletAddress, string inputTokenAddress, string outputTokenAddress, BigDecimal quantity, decimal slippage, int gasPrice)
        {
            BigDecimal amountInMax = ((100 + slippage) / 100) * await Chain.TokenManager.GetTokenTokenOutputPrice(inputTokenAddress, outputTokenAddress, quantity);

            var swapHandler = Chain.Web3Manager.Web3().Eth.GetContractTransactionHandler<SwapTokensForExactTokensFunction>();
            var swapDTO = new SwapTokensForExactTokensFunction()
            {
                AmountOut = Web3.Convert.ToWei(quantity, UnitConversion.EthUnit.Ether),
                AmountInMax = Web3.Convert.ToWei(amountInMax, UnitConversion.EthUnit.Ether),
                Path = new List<string>
                {
                    inputTokenAddress,
                    Chain.ChainManager.Token().Address,
                    outputTokenAddress
                },
                To = walletAddress,
                Deadline = GetTxDeadline(),
                GasPrice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei)
            };

            dynamic estimatedGas = await GetEstimated(swapHandler, swapDTO);
            if (estimatedGas.Error) return estimatedGas;

            swapDTO.Gas = GetTxGas(estimatedGas);

            return await BuildAndSendTx(Chain.RouterManager.Address(), swapHandler, swapDTO);
        }

        public static async Task<dynamic> GetTransactionDetails(string transactionHash)
        {
            var transactionReceipt = await Chain.Web3Manager.Web3().Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            var transaction = await Chain.Web3Manager.Web3().Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);

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

        public static async Task<dynamic> CheckTransactionStatus(dynamic transactionReceipt)
        {
            dynamic response = new ExpandoObject();

            if (transactionReceipt.Error)
            {
                response.Error = true;
                response.Message = transactionReceipt.Message;
            }
            else
            {
                dynamic transactionDetails = await GetTransactionDetails(transactionReceipt.TransactionHash);
                response.Error = false;
                response.TransactionDetails = transactionDetails;
            }
            return response;
        }

        public static async Task<BigDecimal> GetEthToTokenMinimumReceived(string tokenAddress, BigDecimal price, BigDecimal quantity, decimal slippage)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await Chain.TokenManager.GetEthTokenInputPrice(tokenAddress, quantity);
            return amountOutMin;
        }

        public static async Task<BigDecimal> GetTokenToEthMinimumReceived(string tokenAddress, BigDecimal price, BigDecimal quantity, decimal slippage)
        {
            BigDecimal amountOutMin = ((100 - slippage) / 100) * await Chain.TokenManager.GetTokenEthInputPrice(tokenAddress, quantity);
            return amountOutMin;
        }
    }
}
