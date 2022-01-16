using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GannyBot
{
    public partial class ConnectionLostForm : Form
    {
        public ConnectionLostForm()
        {
            InitializeComponent();
        }

        private void ConnectionLostForm_Load(object sender, EventArgs e)
        {
            
        }

        public void UpdateLabel2Text(string text)
        {
            label2.Text = text;
        }

    }
}
