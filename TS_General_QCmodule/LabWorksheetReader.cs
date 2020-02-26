using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS_General_QCmodule
{
    public class LabWorksheetReader
    {
        public LabWorksheetReader(string path)
        {
            List<string> lines = new List<string>(50);

            if (File.Exists(path))
            {
                try
                {
                    lines.AddRange(File.ReadAllLines(path));
                }
                catch(Exception er)
                {
                    if(er.GetType() == typeof(IOException))
                    {
                        var result = MessageBox.Show("This Worksheet file was open in another process. Close the file and try loading again", "File In Use", MessageBoxButtons.OK);
                        if(result == DialogResult.OK || result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                    else
                    {
                        string message = $"{er.Message}\r\nat:{er.StackTrace}";
                        string cap = "An Exception Has Occured";
                        var result = MessageBox.Show(message, cap, MessageBoxButtons.OK);
                        if (result == DialogResult.OK || result == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                var result = MessageBox.Show($"A file at:\r\n\r\n{path}\r\n\r\ncould not be found.", "File Not Found", MessageBoxButtons.OK);
                if (result == DialogResult.OK || result == DialogResult.Cancel)
                {
                    return;
                }
            }

            // Get saved PKCs
            List<string> collector = new List<string>();
            collector.AddRange(Directory.EnumerateFiles(Form1.pkcPath, "*.pkc"));
            collector.AddRange(Directory.EnumerateFiles(Form1.pkcPath, "*.json"));
            filePathPairs = new Dictionary<string, string>(collector.Count);
            for(int i = 0; i < collector.Count; i++)
            {
                filePathPairs.Add(Path.GetFileNameWithoutExtension(collector[i]), collector[i]);
            }

            // Get pkcList to pass to form1
            int pkcStart = lines.FindIndex(x => x.Contains("Core, Module(s)")) + 1;
            int pkcEnd = pkcStart + 8;
            GuiCursor.WaitCursor(() => { GetPkcList(pkcStart, pkcEnd, lines); });
            if(pkcFileNotFound.Count > 0)
            {
                using (AddPKCs addpkcs = new AddPKCs(pkcFileNotFound.Keys.ToList()))
                {
                    if(addpkcs.ShowDialog() == DialogResult.OK)
                    {
                        foreach(KeyValuePair<string, List<int>> k in  pkcFileNotFound)
                        {
                            string tempPath = addpkcs.items.Where(x => x.name == k.Key)
                                                           .Select(x => x.path)
                                                           .FirstOrDefault();
                            if(tempPath != null)
                            {
                                for(int i = 0; i < k.Value.Count; i++)
                                {
                                    pkcList[i].Add(tempPath);
                                }
                            }
                        }
                    }
                }
            }
            

            

            //areaMatrix = new int[8][];
            //areaTotals = new int[12];
            //int areaStart = lines.FindIndex(x => x.Contains("Area μm2")) + 1;
            //int areaEnd = areaStart + 9;
            //for(int i = areaStart; i < areaEnd; i++)
            //{
            //    string[] bits = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                
            //    int[] temp = new int[12];
            //    for(int j = 0; j < 12; j++)
            //    {
            //        if (i < areaEnd - 1)
            //        {
            //            temp[j] = int.Parse(bits[j + 1]);
            //        }
            //        else
            //        {
            //            areaTotals[j] = int.Parse(bits[j + 1]);
            //        }
            //    }
            //    areaMatrix[i - areaStart] = temp;
            //}
        }

        private static List<string> lets = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H" };
        public List<string>[] pkcList { get; set; }
        private Dictionary<string, List<int>> pkcFileNotFound { get; set; }
        private void GetPkcList(int start, int stop, List<string> _lines)
        {
            if(pkcList == null)
            {
                pkcList = new List<string>[8];
            }
            if(pkcFileNotFound == null)
            {
                pkcFileNotFound = new Dictionary<string, List<int>>();
            }
            else
            {
                pkcFileNotFound.Clear();
            }

            // Hashset to check if PKC is in saved list
            List<string> keys = new List<string>(filePathPairs.Keys);

            char[] trim = new char[] { ' ' };
            for (int i = start; i < stop; i++)
            {
                string[] bits = _lines[i].TrimStart(trim).Split(trim, 2);
                int let = lets.IndexOf(bits[0]);
                List<string> newPKCs = bits[1].Split(',').Select(x => FormatPKCname(x)).ToList();
                if(pkcList[let] == null)
                {
                    pkcList[let] = new List<string>();
                }
                else
                {
                    pkcList[let].Clear();
                }

                for (int j = 0; j < newPKCs.Count; j++)
                {
                    if(keys.Contains(newPKCs[j]))
                    {
                        pkcList[let].Add(filePathPairs[newPKCs[j]]);
                    }
                    else
                    {
                        string tempFileName = PullPkcFromRepos(Form1.pkcReposPaths[0], newPKCs[j]);
                        if(tempFileName != null)
                        {
                            filePathPairs.Add(newPKCs[j], tempFileName);
                            pkcList[let].Add(tempFileName);
                            keys.Add(newPKCs[j]);
                        }
                        else
                        {
                            if(pkcFileNotFound.Keys.Contains(newPKCs[j]))
                            {
                                pkcFileNotFound[newPKCs[j]].Add(let);
                            }
                            else
                            {
                                List<int> temp = new List<int>() { let };
                                pkcFileNotFound.Add(newPKCs[j], temp);
                            }
                        }
                    }
                }
            }
        }

        private string FormatPKCname(string input)
        {
            char[] trim = new[] { ' ' };
            string temp1 = input.TrimStart(trim);
            if(temp1.Contains(" (v"))
            {
                string temp2 = temp1.Replace(" (v", "_v");
                return temp2.Replace(")", string.Empty);
            }
            else
            {
                return temp1;
            }
        }

        private List<string> pkcDir { get; set; }
        private string PullPkcFromRepos(string pathToRepos, string fileToMatch)
        {
            if (pkcDir == null)
            {
                if (Directory.Exists(pathToRepos))
                {
                    pkcDir = Directory.EnumerateFiles(pathToRepos, "*", SearchOption.AllDirectories).ToList();
                }
                else
                {
                    return null;
                }
            }

            string pathToLoad = pkcDir.Where(x => Path.GetFileNameWithoutExtension(x).Equals(fileToMatch)).FirstOrDefault();
            if (pathToLoad != null)
            {
                try
                {
                    string thisFileName = $"{Form1.pkcPath}\\{Path.GetFileName(pathToLoad)}";
                    File.Copy(pathToLoad, thisFileName);
                    return thisFileName;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private Dictionary<string, string> filePathPairs { get; set; }
        public int[][] areaMatrix { get; set; }
        public int[] areaTotals { get; set; }
    }
}
