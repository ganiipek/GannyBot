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
        private void button1_Click(object sender, EventArgs e)
        {
            dynamic login = Security.LoginManager.Login(textBox1.Text, textBox2.Text);

            if (login.error)
            {
                label3.Text = login.message;
                return;
            }
            else
            {
                UI.UIManager.LOGIN = true;
                Security.User.Email = textBox1.Text;
                Security.User.Password = textBox2.Text;

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
