using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class RDownloadLink : Form
    {
        public RDownloadLink()
        {
            InitializeComponent();
        }

        private bool ClickedLink { get; set; }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ClickedLink = new bool();
            
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://cran.r-project.org/bin/windows/base/old/3.3.2/",
                    UseShellExecute = true
                };
                Process.Start(psi);
                ClickedLink = true;
            }
            catch(Exception er)
            {
                MessageBox.Show($"{er.Message}\r\n\r\nat:\r\n\r\n{er.StackTrace}", "Could Not Access R Download", MessageBoxButtons.OK);
                ClickedLink = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ClickedLink)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }
    }
}
