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
    public partial class LoginForm : Form
    {
        public static Form1 _form1;

        public LoginForm()
        {
            InitializeComponent();
        }

        private dynamic Login()
        {
            string mail = textBox1.Text;
            string password = textBox2.Text;

            Form1.clientSocket.SendData("{'type':'login', 'email':'"+ mail + "', 'password':'"+ password + "'}");
            dynamic receiveData = Form1.clientSocket.ReceiveData();
            string strData = receiveData.ToString();
            System.Diagnostics.Debug.WriteLine(strData);

            return receiveData;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            dynamic login = Login();

            if (login.error)
            {
                label3.Text = login.message;
                return;
            }
            else
            {
                _form1.LOGIN = true;
                this.Close();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        
    }
}
