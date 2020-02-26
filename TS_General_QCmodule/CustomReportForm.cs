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
    public partial class CustomReportForm : Form
    {
        public CustomReportForm()
        {
            InitializeComponent();

            string text1 = "Click create to save a new table using the selected template";
            toolTip1.SetToolTip(button1, text1);
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
