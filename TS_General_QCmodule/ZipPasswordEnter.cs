using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class ZipPasswordEnter : Form
    {
        public ZipPasswordEnter()
        {
            InitializeComponent();
        }

        public string password { get; set; }

        private void okButton_Click(object sender, EventArgs e)
        {
            password = textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
