using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class CrossRlfClass
    {
        // >>>> PROPERTIES  <<<<
        public List<string> contentNames { get; set; }
        public List<CrossRlfRecord> totalContent { get; set; }
        public List<CrossRlfRecord> overlapContent { get; set; }
        public List<CrossRlfRecord> nonOverlapContent { get; set; }
        // >>>>>>>>>><<<<<<<<<<<    

        // PlexSet CrossRlfClass constructor
        public CrossRlfClass(List<RlfClass> _rlfList, bool isPlexSet, List<string> codeClassesIncluded)
        {
            contentNames = GetContentNamesPS(_rlfList);
            totalContent = GetCrossRlfRecordListPS(contentNames, _rlfList, codeClassesIncluded);
            overlapContent = getOverlapContent(totalContent);
            //nonOverlapContent = getNonOverlapContent(totalContent);
        }

        // Non-PS, Non DSP CrossRlfClass constructor
        public CrossRlfClass(List<RlfClass> _rlfList, List<string> codeClassesIncluded)
        {
            contentNames = GetContentNames(_rlfList);
            totalContent = GetCrossRlfRecordList(contentNames, _rlfList, codeClassesIncluded);
            overlapContent = getOverlapContent(totalContent);
            //nonOverlapContent = getNonOverlapContent(totalContent);
        }

        // For method 1 (Non-PS and non-DSP)
        private List<string> GetContentNames(List<RlfClass> rlfList)
        {
            IEnumerable<RlfRecord> temp0 = rlfList.SelectMany(x => x.content);
            return temp0.Select(x => x.ProbeID).Distinct().ToList();
        }

        private List<CrossRlfRecord> GetCrossRlfRecordList(List<string> _contentNames, List<RlfClass> rlfList, List<string> _codeClassesIncluded)
        {
            // Get cross RLF objects
            int len = contentNames.Count;
            int len2 = rlfList.Count;
            List<CrossRlfRecord> temp = new List<CrossRlfRecord>(len);
            for(int i = 0; i < len; i++)
            {
                List<RlfRecord> temp0 = new List<RlfRecord>(len2);
                for (int j = 0; j < len2; j++)
                {
                    temp0.Add(rlfList[j].content.Where(x => x.ProbeID == _contentNames[i]).FirstOrDefault());
                }
                temp.Add(new CrossRlfRecord(temp0));
            }

            // Get selected codeclasses and order for cross codeset group
            List<string> tempOrder = new List<string>(_codeClassesIncluded.Count);
            for(int i = 0; i < Form1.codeClassOrder.Length; i++)
            {
                tempOrder.AddRange(_codeClassesIncluded.Where(x => x == Form1.codeClassOrder[i]));
            }
            List<CrossRlfRecord> ordered = OrderCodeClasses(temp, tempOrder);

            return ordered;
        }

        private List<CrossRlfRecord> getOverlapContent(List<CrossRlfRecord> totalList)
        {
            return totalList.Where(x => x.included.All(y => y)).ToList();
        }

        private List<CrossRlfRecord> getNonOverlapContent(List<CrossRlfRecord> totalList)
        {
            return totalList.Where(x => x.included.Any(y => !y)).ToList();
        }

        // For method 2 (PS RLFs)
        private List<string> GetContentNamesPS(List<RlfClass> rlfList)
        {
            List<RlfRecord> temp0 = rlfList.SelectMany(x => x.content)
                                                  .Where(y => y.CodeClass.Contains('1'))
                                                  .Distinct()
                                                  .ToList();
            return temp0.Select(x => x.ProbeID).Distinct().ToList();
        }

        private List<CrossRlfRecord> GetCrossRlfRecordListPS(List<string> _contentNames, List<RlfClass> rlfList, List<string> _codeClassesIncluded)
        {
            int len = contentNames.Count;
            int len2 = rlfList.Count;
            string[] sets = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            List<CrossRlfRecord> temp = new List<CrossRlfRecord>(len);
            for(int k = 0; k < 8; k++)
            {
                // Add endo and HK content
                for (int i = 0; i < len; i++)
                {
                    List<RlfRecord> temp0 = new List<RlfRecord>(len2);
                    for(int j = 0; j < len2; j++)
                    {
                        temp0.Add(rlfList[j].content.Where(x => x.ProbeID == _contentNames[i] && x.CodeClass.Substring(x.CodeClass.Length - 2, 1) == sets[k]).FirstOrDefault());
                    }
                    temp.Add(new CrossRlfRecord(temp0));
                }

                // Add POS controls
                List<RlfRecord> tempPOS = new List<RlfRecord>(len2);
                for (int j = 0; j < len2; j++)
                {
                    tempPOS.Add(rlfList[j].content.Where(x => x.Name == $"POS_{sets[k]}").FirstOrDefault());
                }
                temp.Add(new CrossRlfRecord(tempPOS));

                // Add NEG controls
                List<RlfRecord> tempNEG = new List<RlfRecord>(len2);
                for (int j = 0; j < len2; j++)
                {
                    tempNEG.Add(rlfList[j].content.Where(x => x.Name == $"NEG_{sets[k]}").FirstOrDefault());
                }
                temp.Add(new CrossRlfRecord(tempNEG));
            }

            // Add Purespikes
            List<string> spikes = rlfList.SelectMany(x => x.content)
                                         .Where(y => y.CodeClass.Equals("Purification"))
                                         .Distinct()
                                         .Select(y => y.ProbeID)
                                         .ToList();
            int len3 = spikes.Count;
            for(int j = 0; j < len3; j++)
            {
                List<RlfRecord> temp0 = new List<RlfRecord>(len2);
                for (int i = 0; i < len2; i++)
                {
                    temp0.Add(rlfList[i].content.Where(x => x.ProbeID == spikes[j]).FirstOrDefault());
                }
                temp.Add(new CrossRlfRecord(temp0));
            }

            // Get selected codeClasses from those included and order crossRlfRecords
            List<string> tempOrder = new List<string>(_codeClassesIncluded.Count);
            for (int i = 0; i < Form1.psCodeClassOrder.Length; i++)
            {
                tempOrder.AddRange(_codeClassesIncluded.Where(x => x == Form1.psCodeClassOrder[i]));
            }
            List<CrossRlfRecord> ordered = OrderCodeClasses(temp, tempOrder);

            return ordered;
        }

        private List<CrossRlfRecord> OrderCodeClasses(List<CrossRlfRecord> unsorted, List<string> order)
        {
            List<CrossRlfRecord> sorted = new List<CrossRlfRecord>(unsorted.Count());
            List<CrossRlfRecord> overlap = unsorted.Where(x => x.included.All(y => y)).ToList();
            for (int i = 0; i < order.Count; i++)
            {
                sorted.AddRange(overlap.Where(x => x.CodeClass == order[i]).OrderBy(x => x.Name));
            }
            //List<CrossRlfRecord> notoverlapping = unsorted.Where(x => x.included.Any(y => !y)).ToList();
            //for (int i = 0; i < order.Count; i++)
            //{
            //    sorted.AddRange(notoverlapping.Where(x => x.CodeClass == order[i]).OrderBy(x => x.Name));
            //}
            return sorted;
        }
    }
}
