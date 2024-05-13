using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class GenericDSPCodeSumTable
    {
        public GenericDSPCodeSumTable(List<Lane> lanes)
        {
            if(TableLines == null)
            {
                TableLines = new List<string>();
            }
            else
            {
                TableLines.Clear();
            }

            TableLines.AddRange(GetHeaderMatrix(lanes).Select(x => string.Join(",", x)));
            TableLines.Add(new string(',', lanes.Count));
            TableLines.AddRange(GetProbeCounts(lanes).Select(x => string.Join(",", x)));

            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Choose name and location to save table to";
                    sfd.Filter = "CSV|*.csv;*.CSV";
                    sfd.RestoreDirectory = true;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllLines(sfd.FileName, TableLines.ToArray());
                        OpenFileAfterSaved(sfd.FileName, 3000);
                    }
                    else
                    {
                        return;
                    }

                }
            }
            catch(Exception er)
            {
                MessageBox.Show($"{er.Message}\r\n\r\n{er.StackTrace}", "Error", MessageBoxButtons.OK);
                return;
            }
        }
        
        public List<string> TableLines { get; set; }

        private List<string[]> GetHeaderMatrix(List<Lane> lanes)
        {
            List<string[]> result = new List<string[]>(13);

            for(int i = 0; i < 13; i++)
            {
                List<string> temp = new List<string>(lanes.Count + 1);
                switch (i)
                {
                    case 0:
                        temp.Add(string.Empty);
                        temp.AddRange(lanes.Select(x => x.fileName));
                        result.Add(temp.ToArray());
                        break;
                    case 1:
                        temp.Add("Lane ID");
                        temp.AddRange(lanes.Select(x => x.LaneID.ToString()));
                        result.Add(temp.ToArray());
                        break;
                    case 2:
                        temp.Add("Owner");
                        temp.AddRange(lanes.Select(x => x.owner));
                        result.Add(temp.ToArray());
                        break;
                    case 3:
                        temp.Add("Comments");
                        temp.AddRange(lanes.Select(x => x.comments));
                        result.Add(temp.ToArray());
                        break;
                    case 4:
                        temp.Add("Sample ID");
                        temp.AddRange(lanes.Select(x => x.SampleID));
                        result.Add(temp.ToArray());
                        break;
                    case 5:
                        temp.Add("RLF");
                        temp.AddRange(lanes.Select(x => x.RLF));
                        result.Add(temp.ToArray());
                        break;
                    case 6:
                        temp.Add("Instrument");
                        temp.AddRange(lanes.Select(x => x.Instrument));
                        result.Add(temp.ToArray());
                        break;
                    case 7:
                        temp.Add("Stage Position");
                        temp.AddRange(lanes.Select(x => x.StagePosition.ToString()));
                        result.Add(temp.ToArray());
                        break;
                    case 8:
                        temp.Add("Cartridge Barcode");
                        temp.AddRange(lanes.Select(x => x.CartBarcode));
                        result.Add(temp.ToArray());
                        break;
                    case 9:
                        temp.Add("Cartridge ID");
                        temp.AddRange(lanes.Select(x => x.cartID));
                        result.Add(temp.ToArray());
                        break;
                    case 10:
                        temp.Add("FOV Count");
                        temp.AddRange(lanes.Select(x => x.FovCount.ToString()));
                        result.Add(temp.ToArray());
                        break;
                    case 11:
                        temp.Add("FOV Counted");
                        temp.AddRange(lanes.Select(x => x.FovCounted.ToString()));
                        result.Add(temp.ToArray());
                        break;
                    case 12:
                        temp.Add("Binding Density");
                        temp.AddRange(lanes.Select(x => x.BindingDensity.ToString()));
                        result.Add(temp.ToArray());
                        break;
                }
            }

            return result;
        }

        private List<string[]> GetProbeCounts(List<Lane> lanes)
        {
            List<string> idList = lanes[0].probeContent.Select(x => x[3]).ToList();
            List<string[]> result = new List<string[]>(idList.Count);
            for(int i = 0; i < idList.Count; i++)
            {
                List<string> temp = new List<string>(lanes.Count + 1);
                temp.Add(idList[i]);
                var temp1 = lanes.Select(x => x.probeContent.Where(y => y[3].Equals(idList[i])).Select(y => y[5]).First());
                temp.AddRange(temp1);
                result.Add(temp.ToArray());
            }
            return result;
        }

        private void OpenFileAfterSaved(string _path, int delay)
        {
            string message = $"Would you like to open {_path.Substring(_path.LastIndexOf('\\') + 1)} now?";
            string cap = "File Saved";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result = MessageBox.Show(message, cap, buttons);
            if (result == DialogResult.Yes)
            {
                int sleepAmount = 3000;
                int sleepStart = 0;
                int maxSleep = delay;
                while (true)
                {
                    try
                    {
                        Process.Start(_path);
                        break;
                    }
                    catch (Exception er)
                    {
                        if (sleepStart <= maxSleep)
                        {
                            System.Threading.Thread.Sleep(3000);
                            sleepStart += sleepAmount;
                        }
                        else
                        {
                            string message2 = $"The file could not be opened because an exception occured.\r\n\r\nDetails:\r\n{er.Message}\r\nat:\r\n{er.StackTrace}";
                            string cap2 = "File Saved";
                            MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                            DialogResult result2 = MessageBox.Show(message2, cap2, buttons2);
                            if (result2 == DialogResult.OK || result2 == DialogResult.Cancel)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
