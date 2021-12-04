using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GannyBot
{
    public partial class Form2 : Form
    {
        BlockChain web3 = new BlockChain();
        public static Form1 _form1;
        public Form2()
        {
            InitializeComponent();
            web3.StartOnlyChain();

            textBox1.Text = Chain.WalletManager.Address();
            textBox2.Text = Chain.WalletManager.Key();

            switch (Chain.ChainManager.Name())
            {
                case "Smart Chain":
                    comboBox1.SelectedIndex = 0;
                    break;
                case "Smart Chain Test":
                    comboBox1.SelectedIndex = 1;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!Chain.WalletManager.CheckWalletAddress(textBox1.Text))
            {
                MessageBox.Show("Invalid Wallet Address");
                return;
            }

            if (textBox2.Text.Length == 0 || textBox2.Text == "null")
            {
                MessageBox.Show("Invalid Wallet Private Key");
                return;
            }

            if(!Chain.WalletManager.Check(textBox1.Text, textBox2.Text, Chain.ChainManager.ChainID()))
            {
                MessageBox.Show("Wallet address and private key do not match.");
                return;
            }

            Chain.chain chain = new Chain.chain();

            switch (comboBox1.SelectedItem.ToString())
            {
                case "Binance Smart Chain":
                    chain = Chain.chain.binance_smart_chain;
                    break;
                case "Binance Smart Chain TestNet":
                    chain = Chain.chain.binance_smart_chain_test;
                    break;
            }
            Database.DatabaseManager db = new Database.DatabaseManager();
            db.DeleteAccount();

            string KeyString = Security.CryptManager.GenerateAPassKey("SS2131asdajn1!^!'ÇDASD!^1231231231");
            string EncryptedPassword = Security.CryptManager.Encrypt(textBox2.Text, KeyString);
            db.AddAccount(textBox1.Text, EncryptedPassword);

            Chain.ChainManager.Select(chain);
            Chain.WalletManager.Set(
                textBox1.Text,
                textBox2.Text,
                Chain.ChainManager.ChainID()
                );
            Chain.Web3Manager.Start();
            Chain.RouterManager.Select(Chain.router.PancakeSwapV2);

            _form1.SetWeb3Status();

            this.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
