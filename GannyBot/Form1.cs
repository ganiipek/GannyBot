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
using System.Text.RegularExpressions;

namespace GannyBot
{
    public partial class Form1 : Form
    {
        UI.UIManager uiManager;

        ToolTip toolTip1 = new ToolTip();

        decimal textBox4_LastValue = 10;
        decimal textBox8_LastValue = 10;
        int textBox13_LastValue = 5;
        int textBox14_LastValue = 5;

        public Form1()
        {
            Form2._form1 = this;
            LoginForm._form1 = this;

            InitializeComponent();
            
            Chain.Web3Manager._form1 = this;
            UI.LimitManager._form1 = this;
            UI.MarketManager._form1 = this;
            UI.WalletManager._form1 = this;
            UI.UIManager._form1 = this;

            uiManager = new UI.UIManager();

            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;

            textBox13.Enabled = false;
            textBox14.Enabled = false;

            tabControl1.TabPages["mainTab_presale"].Enabled = false;

            label40.Text = Chain.ChainManager.Token().Symbol + ":";

            

            Limit_SetGroupBoxText();
            SetToolTip();
        }

        void SetToolTip()
        {
            

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            toolTip1.SetToolTip(this.label13, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label14, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label16, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label18, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label70, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label71, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label86, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label84, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label82, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label80, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label77, "Kopyalamak için çift tıklayın.");
            toolTip1.SetToolTip(this.label76, "Kopyalamak için çift tıklayın.");
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //clientSocket.Close();
        }

        public void Exit()
        {
            Application.Exit();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }


        public void ShowWETHPrice(BigDecimal price)
        {
            label46.Text = price.ToString() + " $";
        }

        public void SetWeb3Status()
        {
            if (Chain.Web3Manager.IsConnected())
            {
                Console.WriteLine("Web3: Connected");
                label8.Text = "Connected";
                label8.ForeColor = Color.Green;
                label73.Text = Chain.ChainManager.Name();
                label33.Text = Chain.WalletManager.Address();
            }
            else
            {
                Console.WriteLine("Web3: Disconnected");
                label8.Text = "Disconnected";
                label8.ForeColor = Color.Red;
                label73.Text = "-";
                label33.Text = "-";
            }
        }

        /**********************************************************/
        /************************* WALLET *************************/
        /**********************************************************/
        public void ShowWalletAddress(string address)
        {
            label33.Text = address;
        }

        public void ShowWalletBalance(BigDecimal balance)
        {
            label34.Text = balance.ToString() + " BNB";
        }

        void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0)
                return;

            string address = listView1.SelectedItems[0].SubItems[2].Text;
            uiManager.walletManager.ShowSelectedToken(address);
        }

        public void Wallet_ListViewAddItem(Chain.Token token)
        {
            string[] row = { token.Name, token.Symbol, token.Address };

            var listViewItem = new ListViewItem(row);
            listView1.Items.Add(listViewItem);
        }

        public void Wallet_ShowTokenInfo(Chain.Token token)
        {
            label41.Text = token.Address;
            label42.Text = token.Name;
            label43.Text = token.Symbol;
            label44.Text = token.Balance.ToString();
            label45.Text = token.Price.ToString();
        }

        public string Wallet_ListViewSelectedItem()
        {
            if (listView1.SelectedItems.Count == 0)
                return null;

            return listView1.SelectedItems[0].SubItems[2].Text;
        }

        public void Wallet_RemoveItem(string address)
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

            dynamic response = await uiManager.walletManager.AddToken(tokenAddress);

            if (response.Error)
            {
                MessageBox.Show(response.Message);
                return;
            }

            Trade.OrderToken token = response.Token;

            UI.UIManager.database.AddWalletToken(token);

            Wallet_ListViewAddItem(token);

            textBox1.Text = null;
        }

        // Remove Button
        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            string tokenAddress = listView1.SelectedItems[0].SubItems[2].Text;

            uiManager.walletManager.RemoveToken(tokenAddress);
            UI.UIManager.database.RemoveWalletTokens(tokenAddress);
            Wallet_RemoveItem(tokenAddress);
        }

        /**********************************************************/
        /************************* MARKET *************************/
        /**********************************************************/
        private void textBox4_Leave(object sender, EventArgs e)
        {
            if (Regex.IsMatch(textBox4.Text.Replace(".", ","), "^-{0,1}\\d+\\,{0,1}\\d*$"))
            {
                decimal slippage = Convert.ToDecimal(textBox4.Text.Replace(".", ","));

                if (slippage >= 0 && slippage <= 100)
                {
                    textBox4_LastValue = slippage;
                }
            }
            textBox4.Text = textBox4_LastValue.ToString();
        }

        private void textBox14_Leave(object sender, EventArgs e)
        {
            if (Regex.IsMatch(textBox14.Text, "^[0-9]+$"))
            {
                int numbers = Convert.ToInt32(textBox14.Text);

                if (numbers < 5)
                {
                    textBox14.Text = textBox14_LastValue.ToString();
                    return;
                }
                textBox14_LastValue = numbers;
            }
            else
            {
                textBox14.Text = textBox14_LastValue.ToString();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox4.SelectedItem.ToString())
            {
                case "Standard":
                    textBox13.Enabled = false;
                    textBox13.Text = "5";
                    break;
                case "Fast":
                    textBox13.Enabled = false;
                    textBox13.Text = "6";
                    break;
                case "Instant":
                    textBox13.Enabled = false;
                    textBox13.Text = "7";
                    break;
                case "Rapid":
                    textBox13.Enabled = false;
                    textBox13.Text = "10";
                    break;
                case "Custom":
                    textBox13.Enabled = true;
                    break;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            textBox2.Text = listView1.SelectedItems[0].SubItems[2].Text;
        }

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
            return Convert.ToInt32(textBox14.Text);
        }

        void Market_ClearTransactionInformation()
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
                uiManager.marketManager.AddToken(textBox2.Text);
            }
        }

        public void Market_SetMinimumReceived(BigDecimal amount)
        {
            textBox12.Text = amount.ToString();
        }

        public void Market_ShowTokenInfo(Trade.OrderToken token)
        {
            label13.Text = token.Name;
            label14.Text = token.Symbol;
            label16.Text = token.Decimals.ToString();
            label18.Text = token.Price.ToString();
            label70.Text = token.Balance.ToString();
            label71.Text = token.Approved.ToString();

            label13.ForeColor = Color.Black;
            label13.ForeColor = Color.Black;
        }

        public void Market_ShowError(string error)
        {
            label13.Text = "Error";
            label14.Text = error;
            label16.Text = "";
            label18.Text = "";
            label70.Text = "";
            label71.Text = "";

            label13.ForeColor = Color.Red;
            label14.ForeColor = Color.Red;
        }

        public void Market_ShowTransactionProcess(int type)
        {
            if(type == 0)
            {
                label56.Text = "Sistem";
                linkLabel1.Text = "Sözleşme Hazırlanıyor...";

                label56.ForeColor = Color.Blue;
            }
            else if (type == 0)
            {
                label56.Text = "Bekleyin";
                linkLabel1.Text = "Sözleşme Gönderildi. Ağdan cevap bekleniyor...";

                label56.ForeColor = Color.Orange;
            }
        }

        private void Market_ShowTransactionInformation(dynamic response)
        {
            if (response.Error)
            {
                label56.Text = "Error";
                linkLabel1.Text = response.Message;

                label56.ForeColor = Color.Red;
            }
            else
            {
                dynamic transactionDetails = response.TransactionDetails;

                linkLabel1.Text = transactionDetails.Hash;
                string links = Chain.ChainManager.ExplorerUrl() + "tx/" + transactionDetails.Hash;
                linkLabel1.Links.Add(0, links.Length, links);
                label56.Text = transactionDetails.Status;
                label57.Text = transactionDetails.From;
                label62.Text = transactionDetails.To;
                label63.Text = transactionDetails.Value + " BNB";
                label64.Text = transactionDetails.TotalFee + " BNB";
                label65.Text = transactionDetails.GasLimit;
                label66.Text = transactionDetails.GasUsed;
                label67.Text = transactionDetails.GasPrice + " Gwei";

                label56.ForeColor = Color.DarkGreen;
            }
            
        }

        // BUY BUTTON
        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            Market_ClearTransactionInformation();

            dynamic response = await uiManager.marketManager.BuyToken(
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
            Market_ClearTransactionInformation();

            dynamic response = await uiManager.marketManager.SellToken(
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
            Market_ClearTransactionInformation();

            Market_ShowTransactionProcess(type: 0);

            dynamic transactionReceipt = await Trade.TradeManager.Approve(
                Market_GetTokenAddress(),
                Market_GetInputValue() 
                );

            Market_ShowTransactionProcess(type: 1);

            dynamic response = await Trade.TradeManager.CheckTransactionStatus(transactionReceipt);

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
        private void textBox8_Leave(object sender, EventArgs e)
        {
            if (Regex.IsMatch(textBox8.Text.Replace(".", ","), "^-{0,1}\\d+\\,{0,1}\\d*$"))
            {
                decimal slippage = Convert.ToDecimal(textBox8.Text.Replace(".", ","));

                if (slippage >= 0 && slippage <= 100)
                {
                    textBox8_LastValue = slippage;
                }
            }
            textBox8.Text = textBox8_LastValue.ToString();
        }

        private void textBox13_Leave(object sender, EventArgs e)
        {
            if (Regex.IsMatch(textBox13.Text, "^[0-9]+$"))
            {
                int numbers = Convert.ToInt32(textBox13.Text);

                if (numbers < 5)
                {
                    textBox13.Text = textBox13_LastValue.ToString();
                    return;
                }
                textBox13_LastValue = numbers;
            }
            else
            {
                textBox13.Text = textBox13_LastValue.ToString();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedItem.ToString())
            {
                case "Standard":
                    textBox14.Enabled = false;
                    textBox14.Text = "5";
                    break;
                case "Fast":
                    textBox14.Enabled = false;
                    textBox14.Text = "6";
                    break;
                case "Instant":
                    textBox14.Enabled = false;
                    textBox14.Text = "7";
                    break;
                case "Rapid":
                    textBox14.Enabled = false;
                    textBox14.Text = "10";
                    break;
                case "Custom":
                    textBox14.Enabled = true;
                    break;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            textBox11.Text = listView1.SelectedItems[0].SubItems[2].Text;
        }

        public void Limit_SetGroupBoxText()
        {
            groupBox6.Text = "Active Limit Order (" + uiManager.limitManager.GetLimitOrderCount().ToString() + "/" + uiManager.limitManager.GetMaxLimitOrderCount().ToString() + ") | Unique Token (" + uiManager.limitManager.GetUniqueTokenLimitCount() + "/" + uiManager.limitManager.GetMaxUniqueTokenLimitCount() + ")";
        }

        void textBox11_TextChanged(object sender, EventArgs e)
        {
            if (textBox11.Text.Length > 30)
            {
                uiManager.limitManager.Limit_AddTokenInfo(textBox11.Text);
            }
        }

        public void Limit_ShowTokenInfo(Trade.OrderToken token)
        {
            label86.Text = token.Name;
            label84.Text = token.Symbol;
            label82.Text = token.Decimals.ToString();
            label80.Text = token.Price.ToString();
            label77.Text = token.Balance.ToString();
            label76.Text = token.Approved.ToString();

            label86.ForeColor = Color.Black;
            label84.ForeColor = Color.Black;
        }

        public void Limit_ShowError(string error)
        {
            label86.Text = "Error";
            label84.Text = error;
            label82.Text = "";
            label80.Text = "";
            label77.Text = "";
            label76.Text = "";

            label86.ForeColor = Color.Red;
            label84.ForeColor = Color.Red;
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

        public decimal GetSlippage_Limit()
        {
            return Convert.ToDecimal(textBox8.Text.Replace(".", ","));
        }

        private int GetGasPrice_Limit()
        {
            if(Regex.IsMatch(textBox13.Text, "[^0-9]"))
            {
                return Convert.ToInt32(textBox13.Text);
            }
            return 5;
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

        public void LimitListView_AddItem(string id, string symbol, string address, string price, string limit, string type, string amount, string time)
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
            Limit_SetGroupBoxText();
        }

        public void LimitListView_RemoveItem(string id)
        {
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if(itemRow.SubItems[0].Text == id)
                {
                    itemRow.Remove();
                    Limit_SetGroupBoxText();
                    break;
                }
            }
        }
        
        public void LimitListView_UpdateTokenInfo(int subIndex, string tokenAddress, string data)
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

        public void LimitListView_UpdateTokenColor(int ID, Color background)
        {
            foreach (ListViewItem itemRow in listView2.Items)
            {
                if (itemRow.SubItems[0].Text == ID.ToString())
                {
                    itemRow.SubItems[0].BackColor = background;
                    itemRow.SubItems[1].BackColor = background;
                    itemRow.SubItems[2].BackColor = background;
                    itemRow.SubItems[3].BackColor = background;
                    itemRow.SubItems[4].BackColor = background;
                    itemRow.SubItems[5].BackColor = background;
                    itemRow.SubItems[6].BackColor = background;
                    itemRow.SubItems[7].BackColor = background;
                }
            }
        }

        public void LimitListView_UpdateTokenPrice(string tokenAddress, BigDecimal price)
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
            dynamic checkResponse = await uiManager.limitManager.CheckLimitOrder(
                GetTokenAddress_Limit(),
                GetTradeType_Limit(),
                GetPrice_Limit(),
                GetQuantity_Limit(),
                GetSlippage_Limit(),
                GetGasPrice_Limit()
                );

            if (checkResponse.Error)
            {
                MessageBox.Show(checkResponse.Message);
                return;
            }

            dynamic response = await uiManager.limitManager.NewLimitOrder(
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

            LimitListView_AddItem(
                response.Order.ID.ToString(),
                response.Token.Symbol,
                response.Token.Address,
                "-",
                response.Order.Price.ToString(),
                response.Order.Type,
                response.Order.Quantity.ToString(),
                response.Order.Date.ToString()
                );

            Limit_SetGroupBoxText();
        }

        // Cancel Button
        private void button19_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            int ID = Convert.ToInt32(listView2.SelectedItems[0].SubItems[0].Text);
            string address = listView2.SelectedItems[0].SubItems[2].Text;

            uiManager.limitManager.DeleteLimitOrder(ID, address);
            LimitListView_RemoveItem(ID.ToString());
        }
        // Force Sell
        private void button16_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            int ID = Convert.ToInt32(listView2.SelectedItems[0].SubItems[0].Text);
            Trade.LimitOrder limitOrder = uiManager.limitManager.FindLimitOrderByID(ID);

            uiManager.limitManager.LimitTrade(limitOrder);
        }
        // Force Buy
        private void button13_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
                return;

            int ID = Convert.ToInt32(listView2.SelectedItems[0].SubItems[0].Text);
            Trade.LimitOrder limitOrder = uiManager.limitManager.FindLimitOrderByID(ID);

            uiManager.limitManager.LimitTrade(limitOrder);
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

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        string textBox3_PreviousInput = "";
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            //ControlDecimalsText(textBox3.Text);
        }

        void ControlDecimalsText(string text)
        {
            text = text.Replace(".", ",");

            int commaCount = text.Split(',').Length - 1;
            Console.WriteLine("\ncommaCount: " + commaCount.ToString());

            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                {
                    if (c == ',')
                    {
                        if(commaCount > 1)
                        {
                            text = text.Replace(c.ToString(), "");
                            commaCount--;
                        }
                        
                    }
                    else if(c == ',' && commaCount == 1)
                    {
                        continue;
                    }
                    text = text.Replace(c.ToString(), "");
                }
            }

            Console.WriteLine(text);

            Regex regex = new Regex("^-{0,1}\\d+\\,{0,1}\\d*$");
            Match m = regex.Match(text);
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView2.SelectedItems.Count == 0)
            { 
                button13.Enabled = false;
                button16.Enabled = false;
                return;
            }

            switch (listView2.SelectedItems[0].SubItems[5].Text)
            {
                case "Buy":
                    button13.Enabled = true;
                    button16.Enabled = false;
                    break;

                case "Sell":
                    button13.Enabled = false;
                    button16.Enabled = true;
                    break;
            }
        }

        /**********************************************************/
        /************************** COPY **************************/
        /**********************************************************/
        private void label13_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label13.Text);
        }

        private void label14_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label14.Text);
        }

        private void label16_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label16.Text);
        }

        private void label18_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label18.Text);
        }

        private void label70_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label70.Text);
        }

        private void label71_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label71.Text);
        }

        private void label86_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label86.Text);
        }

        private void label84_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label84.Text);
        }

        private void label82_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label82.Text);
        }

        private void label80_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label80.Text);
        }

        private void label77_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label77.Text);
        }

        private void label76_DoubleClick(object sender, EventArgs e)
        {
            Clipboard.SetText(label76.Text);
        }

        
    }
}
