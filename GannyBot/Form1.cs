using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using Newtonsoft.Json;

using Nethereum.Util;
using System.Threading;

namespace GannyBot
{
    public partial class Form1 : Form
    {
        public bool LOGIN = false;
        //public Database db = new Database();
        BotManager Bot = new BotManager();
        public static ClientSocket clientSocket;
        LoginForm loginForm = new LoginForm();

        System.Threading.Timer timer_Socket;

        public Form1()
        {
            // db.Initialize();

            BotManager._form = this;
            Form2._form1 = this;
            LoginForm._form1 = this;

            InitializeComponent();
            
            ConnectSocket();

            if (!LOGIN)
            {
                Exit();
            }
            
            Web3Initialize();

            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;

            groupBox6.Text = "Active Limit Order (" + Bot.GetLimitOrderCount().ToString() + "/" + Bot.GetMaxLimitOrderCount().ToString() + ") | Unique Token (" + Bot.GetUniqueTokenLimitCount() + "/" + Bot.GetMaxUniqueTokenLimitCount() + ")";
            label40.Text = Bot.web3.GetMainTokenName()+":";

            timer_Socket = new System.Threading.Timer(_ => Timer_Socket(), null, 0, Timeout.Infinite);

            //foreach(Token token in db.GetWalletTokenList())
            //{
            //    string[] row = { token.Name, token.Symbol, token.Address };

            //    var listViewItem = new ListViewItem(row);
            //    listView1.Items.Add(listViewItem);
            //}

            SetToolTip();
        }

        void SetToolTip()
        {
            ToolTip toolTip1 = new ToolTip();

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            toolTip1.SetToolTip(this.label70, "Deneme");
        }

        void Timer_Socket()
        {
            System.Diagnostics.Debug.WriteLine("Socket Connection :" + clientSocket.IsConnected().ToString());
            if (!clientSocket.IsConnected())
            {
                clientSocket = new ClientSocket(new IPEndPoint(IPAddress.Parse("51.81.155.12"), 3131));
                clientSocket.Start();
            }
            timer_Socket.Change(5000, Timeout.Infinite);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            clientSocket.Close();
        }

        void Exit()
        {
            Application.Exit();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        void ConnectSocket()
        {
            int port = 3131;
            Console.WriteLine(string.Format("Client Başlatıldı. Port: {0}", port));
            Console.WriteLine("-----------------------------");

            clientSocket = new ClientSocket(new IPEndPoint(IPAddress.Parse("51.81.155.12"), 3131));

            if (clientSocket.Start())
            {
                loginForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("Sunucuya bağlanılmıyor");
                Exit();
            }
        }

        public bool Web3Initialize()
        {
            Bot.WALLET_ADDRESS = Properties.Settings.Default.wallet_address;
            Bot.WALLET_PRIVATE_KEY = Properties.Settings.Default.wallet_private_key;

            System.Diagnostics.Debug.WriteLine("kontrol 1");
            if (Bot.Initialize())
            {
                System.Diagnostics.Debug.WriteLine("Connected");
                label8.Text = "Connected";
                label8.ForeColor = Color.Green;
                label73.Text = Bot.web3.GetChainName();
                label33.Text = Properties.Settings.Default.wallet_address;

                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Disconnected");
                label8.Text = "Disconnected";
                label8.ForeColor = Color.Red;
                label73.Text = "-";
                label33.Text = "-";

                return false;
            }
        }

        void clearMarketTransactionInformation()
        {
            linkLabel1.Text = "";
            linkLabel1.Links.Clear();
            label56.Text = "";
            label57.Text = "";
            label62.Text = "";
            label63.Text = "";
            label64.Text = "";
            label65.Text = "";
            label66.Text = "";
            label67.Text = "";
        }

        void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
                return;

            string tokenName = listView1.SelectedItems[0].Text;

            //foreach (Token token in tokens)
            //{
            //    if(token.Name == tokenName)
            //    {
            //        showToken(token);
            //    }
            //}
        }

        async void button9_Click(object sender, EventArgs e)
        {
            decimal account_balance = await Bot.web3.GetEthBalance(Bot.WALLET_ADDRESS);
            label34.Text = account_balance.ToString() + " BNB";
        }

        public void ShowMainTokenPrice(BigDecimal price)
        {
            label46.Text = price.ToString() + " $";
        }
        /**********************************************************/
        /************************* WALLET *************************/
        /**********************************************************/
        public void wallet_ShowTokenInfo(Token token)
        {
            label41.Text = token.Address;
            label42.Text = token.Name;
            label43.Text = token.Symbol;
            label44.Text = token.Balance.ToString();
            label45.Text = token.Price.ToString();
        }

        public string wallet_ListViewSelectedItem()
        {
            if (listView1.SelectedItems.Count == 0)
                return null;

            return listView1.SelectedItems[0].SubItems[2].Text;
        }

        public void wallet_RemoveItem(string address)
        {
            foreach (ListViewItem itemRow in listView1.Items)
            {
                if (itemRow.SubItems[2].Text == address)
                {
                    itemRow.Remove();
                    break;
                }
            }
        }

        // Add Button
        async private void button2_Click(object sender, EventArgs e)
        {
            string tokenAddress = textBox1.Text;

            dynamic response = await Bot.wallet_AddToken(tokenAddress);

            if (response.Error)
            {
                MessageBox.Show(response.Message);
                return;
            }

            Token token = response.Token;

            //db.AddWalletToken(token);

            string[] row = { token.Name, token.Symbol, token.Address };

            var listViewItem = new ListViewItem(row);
            listView1.Items.Add(listViewItem);

            textBox1.Text = null;
        }

        // Remove Button
        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            string tokenAddress = listView1.SelectedItems[0].SubItems[2].Text;

            Bot.wallet_RemoveToken(tokenAddress);
            wallet_RemoveItem(tokenAddress);
        }

        /**********************************************************/
        /************************* MARKET *************************/
        /**********************************************************/
        public string Market_GetMarketType()
        {
            return comboBox5.SelectedItem.ToString();
        }

        private string Market_GetTokenAddress()
        {
            return textBox2.Text;
        }

        public decimal Market_GetInputValue()
        {
            return Convert.ToDecimal(textBox3.Text.Replace(".", ","));
        }

        public decimal Market_GetSlippage()
        {
            return Convert.ToDecimal(textBox4.Text.Replace(".", ","));
        }

        private int Market_GetGasPrice()
        {
            int gasPrice = 5;
            switch (comboBox1.SelectedItem.ToString())
            {
                case "Standard (5 GWEI)":
                    gasPrice = 5;
                    break;
                case "Fast (6 GWEI)":
                    gasPrice = 6;
                    break;
                case "Instant (7 GWEI)":
                    gasPrice = 7;
                    break;
                case "Rapid (10 GWEI)":
                    gasPrice = 10;
                    break;
                case "TestNet (15 GWEI)":
                    gasPrice = 15;
                    break;
            }
            return gasPrice;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedItem.ToString() == "Buy")
            {
                button5.Visible = true;
                button11.Visible = false;
                button14.Visible = false;
                label9.Text = "BNB Value:";
            }
            else if (comboBox5.SelectedItem.ToString() == "Sell")
            {
                button5.Visible = false;
                button11.Visible = true;
                button14.Visible = true;
                label9.Text = "Token Value:";
            }
        }

        void textBox2_TextChanged(object sender, EventArgs e)
        {
            if(textBox2.Text.Length > 30)
            {
                Bot.Market_AddTokenInfo(textBox2.Text);
            }
        }

        public void Market_SetMinimumReceived(BigDecimal amount)
        {
            textBox12.Text = amount.ToString();
        }

        public void Market_ShowTokenInfo(Token token)
        {
            label13.Text = token.Name;
            label14.Text = token.Symbol;
            label16.Text = token.Decimals.ToString();
            label18.Text = token.Price.ToString();
            label70.Text = token.Balance.ToString();
            label71.Text = token.Approved.ToString();
        }

        private void Market_ShowTransactionInformation(dynamic response)
        {
            if (response.Error)
            {
                label56.Text = "Error";
                linkLabel1.Text = response.Message;
            }
            else
            {
                dynamic transactionDetails = response.TransactionDetails;

                linkLabel1.Text = transactionDetails.Hash;
                string links = Bot.web3.GetExplorerURL() + "tx/" + transactionDetails.Hash;
                linkLabel1.Links.Add(0, links.Length, links);
                label56.Text = transactionDetails.Status;
                label57.Text = transactionDetails.From;
                label62.Text = transactionDetails.To;
                label63.Text = transactionDetails.Value + " BNB";
                label64.Text = transactionDetails.TotalFee + " BNB";
                label65.Text = transactionDetails.GasLimit;
                label66.Text = transactionDetails.GasUsed;
                label67.Text = transactionDetails.GasPrice + " Gwei";
            }
            
        }

        // BUY BUTTON
        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            clearMarketTransactionInformation();

            dynamic response = await Bot.BuyToken(
                Market_GetTokenAddress(),
                Market_GetInputValue(),
                Market_GetSlippage(),
                Market_GetGasPrice()
                );

            Market_ShowTransactionInformation(response);
            button5.Enabled = true;
        }

        // SELL BUTTON
        private async void button14_Click(object sender, EventArgs e)
        {
            button11.Enabled = false;
            button14.Enabled = false;
            clearMarketTransactionInformation();

            dynamic response = await Bot.SellToken(
                Market_GetTokenAddress(),
                Market_GetInputValue(),
                Market_GetSlippage(),
                Market_GetGasPrice()
                );

            Market_ShowTransactionInformation(response);
            button11.Enabled = true;
            button14.Enabled = true;
        }

        // APPROVE BUTTON
        private async void button11_Click(object sender, EventArgs e)
        {
            button11.Enabled = false;
            button14.Enabled = false;
            clearMarketTransactionInformation();

            dynamic response = await Bot.ApproveToken(
                Market_GetTokenAddress(),
                Market_GetInputValue() 
                );

            Market_ShowTransactionInformation(response);
            button11.Enabled = true;
            button14.Enabled = true;
        }

        /**********************************************************/
        /************************* PRESALE ************************/
        /**********************************************************/
        private void button6_Click(object sender, EventArgs e)
        {
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        /**********************************************************/
        /************************** LIMIT *************************/
        /**********************************************************/
        void textBox11_TextChanged(object sender, EventArgs e)
        {
            if (textBox11.Text.Length > 30)
            {
                Bot.Limit_AddTokenInfo(textBox11.Text);
            }
        }

        public void Limit_ShowTokenInfo(Token token)
        {
            label86.Text = token.Name;
            label84.Text = token.Symbol;
            label82.Text = token.Decimals.ToString();
            label80.Text = token.Price.ToString();
            label77.Text = token.Balance.ToString();
            label76.Text = token.Approved.ToString();
        }

        private string GetTokenAddress_Limit()
        {
            return textBox11.Text;
        }

        private string GetTradeType_Limit()
        {
            return comboBox3.SelectedItem.ToString();
        }

        private decimal GetQuantity_Limit()
        {
            return Convert.ToDecimal(textBox10.Text.Replace(".", ","));
        }

        private decimal GetPrice_Limit()
        {
            return Convert.ToDecimal(textBox9.Text.Replace(".", ","));
        }

        private int GetSlippage_Limit()
        {
            return Convert.ToInt32(textBox8.Text);
        }

        private int GetGasPrice_Limit()
        {
            int gasPrice = 5;
            switch (comboBox4.SelectedItem.ToString())
            {
                case "Standard (5 GWEI)":
                    gasPrice = 5;
                    break;
                case "Fast (6 GWEI)":
                    gasPrice = 6;
                    break;
                case "Instant (7 GWEI)":
                    gasPrice = 7;
                    break;
                case "Rapid (10 GWEI)":
                    gasPrice = 10;
                    break;
                case "TestNet (15 GWEI)":
                    gasPrice = 15;
                    break;
            }
            return gasPrice;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem.ToString() == "Buy")
            {
                label75.Text = "BNB Amount:";
            }
            else if (comboBox3.SelectedItem.ToString() == "Sell")
            {
                label75.Text = "Token Amount:";
            }
        }

        private void AddItemInLimitView(string id, string symbol, string address, string price, string limit, string type, string amount, string time)
        {
            string[] row = {
                id,
                symbol,
                address,
                price,
                limit,
                type,
                amount,   
                time
            };

            var listViewItem = new ListViewItem(row);
            listViewItem.UseItemStyleForSubItems = false;

            listView2.Items.Add(listViewItem);
        }

        public void ListView2_RemoveItem(string id)
        {
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if(itemRow.SubItems[0].Text == id)
                {
                    itemRow.Remove();
                    break;
                }
            }
        }
        
        public void ListView2_UpdateTokenInfo(int subIndex, string tokenAddress, string data)
        {
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if (itemRow.SubItems[1].Text == tokenAddress)
                {
                    itemRow.SubItems[subIndex].Text = data;
                    break;
                }
            }
        }

        public void ListView2_UpdateTokenColor(int ID, Color background)
        {
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if (itemRow.SubItems[0].Text == ID.ToString())
                {
                    itemRow.BackColor = background;
                }
            }
        }

        public void ListView2_UpdateTokenPrice(string tokenAddress, BigDecimal price)
        {
            int addressIndex = 2;
            int priceIndex = 3;
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if (itemRow.SubItems[addressIndex].Text == tokenAddress)
                {
                    BigDecimal newPrice = price.RoundAwayFromZero(10);

                    if (itemRow.SubItems[priceIndex].Text != "-")
                    {
                        BigDecimal oldPrice = Convert.ToDecimal(itemRow.SubItems[priceIndex].Text.Replace(".", ","));

                        if (newPrice > oldPrice)
                        {
                            itemRow.SubItems[priceIndex].ForeColor = Color.Green;
                        }
                        else if (newPrice < oldPrice)
                        {
                            itemRow.SubItems[priceIndex].ForeColor = Color.Red;
                        }
                    }
                    try
                    {
                        itemRow.SubItems[priceIndex].Text = newPrice.ToString();
                    }
                    catch
                    {
                        break;
                    }
                    
                }
            }
        }

        // Add Button
        private async void button17_Click(object sender, EventArgs e)
        {
            dynamic response = await Bot.NewLimitOrder(
                GetTokenAddress_Limit(),
                GetTradeType_Limit(),
                GetPrice_Limit(),
                GetQuantity_Limit(),
                GetSlippage_Limit(),
                GetGasPrice_Limit()
                );

            if (response.Error)
            {
                MessageBox.Show(response.Message);
                return;
            }
            //response.Error = false;
            //response.Token = token;
            //response.Order = limitOrder;

            AddItemInLimitView(
                response.Order.ID.ToString(),
                response.Token.Symbol,
                response.Token.Address,
                "-",
                response.Order.Price.ToString(),
                response.Order.Type,
                response.Order.Quantity.ToString(),
                response.Order.Date.ToString()
                );

            groupBox6.Text = "Active Limit Order (" + Bot.GetLimitOrderCount().ToString() + "/" + Bot.GetMaxLimitOrderCount().ToString() + ") | Unique Token (" + Bot.GetUniqueTokenLimitCount() + "/" + Bot.GetMaxUniqueTokenLimitCount() + ")";
        }

        // Cancel Button
        private void button19_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            int ID = Convert.ToInt32(listView2.SelectedItems[0].SubItems[0].Text);
            string address = listView2.SelectedItems[0].SubItems[2].Text;

            Bot.DeleteLimitOrder(ID, address);
            ListView2_RemoveItem(ID.ToString());

            groupBox6.Text = "Active Limit Order (" + Bot.GetLimitOrderCount().ToString() + "/" + Bot.GetMaxLimitOrderCount().ToString() + ") | Unique Token (" + Bot.GetUniqueTokenLimitCount() + "/" + Bot.GetMaxUniqueTokenLimitCount() + ")";
        }

        /**********************************************************/
        /************************** OTHER *************************/
        /**********************************************************/
        private void button20_Click(object sender, EventArgs e)
        {
            Form2 formSettings = new Form2();
            formSettings.Show();
        }

        private void label58_Click(object sender, EventArgs e)
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Socket Connection: " + clientSocket.IsConnected().ToString());
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public bool CheckSelectedMainTab(string tabName)
        {
            if(tabControl1.SelectedTab == tabControl1.TabPages[tabName])
            {
                return true;
            }
            return false;
        }
    }
}
