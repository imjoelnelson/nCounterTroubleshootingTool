using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public partial class DateSelect : Form
    {
        public DateSelect()
        {
            InitializeComponent();
            SendMessage(textBox1.Handle, 0x1501, 0, "yyyyMMdd"); // Text prompt banner
            SendMessage(textBox2.Handle, 0x1501, 0, "yyyyMMdd"); // Text prompt banner
        }

        public string SelectedStart { get; set; }
        public string SelectedEnd { get; set; }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(textBox1.Text.Length == 8)
            {
                Match match = Regex.Match(textBox1.Text, @"\d{8}");
                if(match.Success)
                {
                    if(textBox2.Text.Length == 8)
                    {
                        Match match2 = Regex.Match(textBox2.Text, @"\d{8}");
                        if(match2.Success)
                        {
                            okButton.Enabled = true;
                            SelectedStart = textBox1.Text;
                            SelectedEnd = textBox2.Text;
                        }
                    }
                }
                else
                {
                    okButton.Enabled = false;
                    MessageBox.Show("Date must have the format \"yyyyMMdd\"", "Wrong Date Format", MessageBoxButtons.OK);
                    textBox1.ResetText();
                    textBox1.Focus();
                }
            }
            else
            {
                okButton.Enabled = false;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length == 8)
            {
                Match match = Regex.Match(textBox2.Text, @"\d{8}");
                if (match.Success)
                {
                    if (textBox1.Text.Length == 8)
                    {
                        Match match2 = Regex.Match(textBox1.Text, @"\d{8}");
                        if (match2.Success)
                        {
                            okButton.Enabled = true;
                            SelectedStart = textBox1.Text;
                            SelectedEnd = textBox2.Text;
                        }
                    }
                }
                else
                {
                    okButton.Enabled = false;
                    MessageBox.Show("Date must have the format \"yyyyMMdd\"", "Wrong Date Format", MessageBoxButtons.OK);
                    textBox1.ResetText();
                    textBox1.Focus();
                }
            }
            else
            {
                okButton.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (CalendarDateSelect select = new CalendarDateSelect(true))
            {
                if (select.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = select.SelectedDate;
                    SelectedStart = select.SelectedDate;
                }
                else
                {
                    textBox1.ResetText();
                    textBox1.Focus();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (CalendarDateSelect select = new CalendarDateSelect(false))
            {
                if (select.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = select.SelectedDate;
                    SelectedEnd = select.SelectedDate;
                }
                else
                {
                    textBox2.ResetText();
                    textBox2.Focus();
                }
            }
        }
    }
}
