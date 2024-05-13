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
    public partial class CalendarDateSelect : Form
    {
        public CalendarDateSelect(bool isStart)
        {
            InitializeComponent();

            if(isStart)
            {
                this.Text = "Select Start Date";
            }
            else
            {
                this.Text = "Select End Date";
            }

            monthCalendar1.MaxSelectionCount = 1;
            monthCalendar1.DateSelected += new DateRangeEventHandler(MonthCalendar1_DateSelected);
        }

        public string SelectedDate { get; set; }

        private void MonthCalendar1_DateSelected(object sender, DateRangeEventArgs e)
        {
            SelectedDate = monthCalendar1.SelectionStart.ToString("yyyyMMdd");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
