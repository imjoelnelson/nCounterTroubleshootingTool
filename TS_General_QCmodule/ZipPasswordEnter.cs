using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class ZipPasswordEnter : Form
    {
        public ZipPasswordEnter(string title)
        {
            InitializeComponent();
            string fileName = Path.GetFileName(title);
            textBox3.Text = fileName;
            fileTip = new ToolTip();
            fileTip.SetToolTip(textBox3, fileName);
            fileTip.InitialDelay = 500;
            fileTip.AutoPopDelay = 10000;
        }

        public string password { get; set; }
        private ToolTip fileTip { get; set; }

        private void okButton_Click(object sender, EventArgs e)
        {
            password = textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
