using Nethereum.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GannyBot.UI
{
    internal class UIManager
    {
        public static Form1 _form1;
        public static bool LOGIN = false;
        int loginTryLimit = 3;

        LoginForm loginForm = new LoginForm();

        System.Threading.Timer timer_WETHPrice;
        System.Threading.Timer timer_Socket;

        public static ClientSocket clientSocket;

        

        public static Database.DatabaseManager database = new Database.DatabaseManager();

        public WalletManager walletManager = new WalletManager();
        public MarketManager marketManager = new MarketManager();
        public LimitManager limitManager = new LimitManager();

        public UIManager()
        {
            ConnectSocket();
            Login();

            if (!LOGIN) _form1.Exit();

            Chain.ChainManager.Select(Chain.chain.binance_smart_chain);
            
            dynamic accounts = database.GetAccount();

            if(accounts.Count > 0)
            {
                string KeyString = Security.CryptManager.GenerateAPassKey("SS2131asdajn1!^!'ÇDASD!^1231231231");
                string DecryptedPassword = Security.CryptManager.Decrypt(accounts.First.key.ToString(), KeyString);

                Chain.WalletManager.Set(
                    accounts.First.address.ToString(),
                    DecryptedPassword,
                Chain.ChainManager.ChainID()
                );
            }

            if(Chain.WalletManager.Address() != null)
            {
                Chain.Web3Manager.Start();
                Chain.RouterManager.Select(Chain.router.PancakeSwapV2);
            }
            
            _form1.SetWeb3Status();

            
            timer_WETHPrice = new System.Threading.Timer(_ => Timer_WETHPrice(), null, 0, Timeout.Infinite);
            timer_Socket = new System.Threading.Timer(_ => Timer_Socket(), null, 5000, Timeout.Infinite);
        }

        bool ConnectSocket()
        {
            int port = 3131;
            Console.WriteLine("-----------------------------");
            Console.WriteLine(string.Format("Client Başlatıldı. Port: {0}", port));
            Console.WriteLine("-----------------------------");

            clientSocket = new ClientSocket(new IPEndPoint(IPAddress.Parse("51.81.155.12"), 3131));

            return clientSocket.Start();
        }

        void Login()
        {
            if (clientSocket.IsConnected())
            {
                loginForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Sunucuya bağlanılmıyor");
                _form1.Exit();
            }
        }

        void Timer_Socket()
        {
            if (!clientSocket.IsConnected())
            {
                if(loginTryLimit == 0)
                {
                    _form1.Exit();
                }

                Console.WriteLine("Socket Connection: " + clientSocket.IsConnected().ToString());
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine("Remaining to try again: " + i.ToString() + "s");
                    Thread.Sleep(1000);
                }

                if (ConnectSocket())
                {
                    dynamic login = Security.LoginManager.Login(Security.User.Email, Security.User.Password);
                    if(login.error) _form1.Exit();
                    loginTryLimit = 3;
                }
                else loginTryLimit--;
            }
            timer_Socket.Change(5000, Timeout.Infinite);
        }

        async Task<BigDecimal> GetWETHPrice_Router()
        {
            return await Chain.TokenManager.GetEthTokenInputPrice(Chain.ChainManager.USDTToken().Address, 1);
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

        async void Timer_WETHPrice()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                BigDecimal priceETH;
                BigDecimal balanceETH;

                try
                {
                    priceETH = await GetWETHPrice_Router();
                    priceETH = priceETH.RoundAwayFromZero(2);

                    Chain.ChainManager.UpdateWETHTokenPrice(priceETH);
                    _form1.ShowWETHPrice(priceETH);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Timer_WETHPrice: " + ex.Message);
                }


                try
                {
                    balanceETH = await Chain.WalletManager.GetWETHBalance(Chain.WalletManager.Address());
                    Chain.ChainManager.UpdateWETHTokenBalance(balanceETH);
                    _form1.ShowWalletBalance(balanceETH);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Timer_WETHPrice: " + ex.Message);
                }
            }
            timer_WETHPrice.Change(5000, Timeout.Infinite);
        }

    }
}
