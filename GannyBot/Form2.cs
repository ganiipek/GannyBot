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

            textBox1.Text = Properties.Settings.Default.wallet_address;
            textBox2.Text = Properties.Settings.Default.wallet_private_key;

            if (Properties.Settings.Default.chain == "Binance Smart Chain") comboBox1.SelectedIndex = 0;
            else if (Properties.Settings.Default.chain == "Binance Smart Chain TestNet") comboBox1.SelectedIndex = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!web3.CheckWalletAddress(textBox1.Text))
            {
                MessageBox.Show("Invalid Wallet Address");
                return;
            }

            if (textBox2.Text.Length == 0 || textBox2.Text == "null")
            {
                MessageBox.Show("Invalid Wallet Private Key");
                return;
            }

            if(!web3.ControlPrivateKey(textBox1.Text, textBox2.Text))
            {
                MessageBox.Show("Wallet address and private key do not match.");
                return;
            }

            Properties.Settings.Default.wallet_address = textBox1.Text;
            Properties.Settings.Default.wallet_private_key = textBox2.Text;
            Properties.Settings.Default.chain = comboBox1.SelectedItem.ToString();

            _form1.Web3Initialize();
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
