using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class DspPlateReport
    {
        private static string[] lets = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        private double platePosAvg { get; set; }
        public string stringOut { get; set; }
        public DspPlateReport(PlateViewCell[][] list)
        {
            platePosAvg = list.SelectMany(x => x).Select(y => y.posCounts).Average();

            StringBuilder sb = new StringBuilder();

            using (Html.Table table = new Html.Table(sb))
            {
                table.AddCaption("<p align=\"left\">Sample QC:<br>Row 1: ERCC POS and NEG<br>Row 2: Control geomean (HK or protein pos control)<br>Row 3: Total Counts</platePosAvg>");
                table.StartHead();
                table.Border(true);
                using (var thead = table.AddRow())
                {
                    thead.AddCell("");
                    for (int i = 1; i < 13; i++)
                    {
                        thead.AddHeadCell(i.ToString());
                    }
                    table.EndHead();
                    table.StartBody();
                    for (int i = 0; i < 8; i++)
                    {
                        using (var tr = table.AddRow(classAttributes: "plateRow"))
                        {
                            tr.AddHeadCell(lets[i].ToString());
                            int cells = list[i].Length;
                            for (int j = 0; j < cells; j++)
                            {
                                PlateViewCell temp = list[i][j];
                                List<string> dataStrings = new List<string>(3);
                                // Add ERCC
                                string one = string.Empty;
                                if(temp.posCounts <= platePosAvg/3)
                                {
                                    one += $"<font style=\"color: red;\">{Math.Round(temp.posCounts).ToString()}&nbsp&nbsp&nbsp{Math.Round(temp.negCounts).ToString()}</font>";
                                }
                                else
                                {
                                    one += $"<font style=\"color: green;\">{Math.Round(temp.posCounts).ToString()}&nbsp&nbsp&nbsp{Math.Round(temp.negCounts).ToString()}</font>";
                                }
                                string two = Math.Round(temp.controlMean).ToString();
                                string three = $"<font style=\"color: blue;\">{Math.Round(temp.totCounts).ToString()}</font>";
                                List<string> temp1 = new List<string>() { one, two, three };
                                tr.AddCell(addBreak(temp1));
                            }
                        }
                    }
                }
            }

            stringOut = sb.ToString();
        }

        private string addBreak(List<string> strings)
        {
            string temp = strings[0];
            for(int i = 1; i < strings.Count; i++)
            {
                temp += "<br style=\"mso-data-placement:same-cell;\" />";
                temp += strings[i];
            }
            return temp;
        }
    }
}
