using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public class LaneFilter
    {
        public LaneFilter(List<Lane> inputLanes, string property, string[] filterTerm)
        {
            GetMethods = new Dictionary<string, GetLanes>
            {
                { "None", NoFilter},
                { "Cartridge ID", FromCartID },
                { "Cartridge Barcode", FromBarcode },
                { "RLF", FromRLF },
                { "Date Range", FromDate },
                { "Instrument Type", FromInstrument },
                { "Lane Numbers", FromNumber }
            };
            LanesOut = GetLanesOut(GetMethods[property], inputLanes, filterTerm);
        }


        // *** OPTIONS FOR FILTERING LANES DEFINED BY:
        //          PropsToSelect - provides strings to be used for combobox
        //          A filter method
        //          A GetLanes delegate instance instantiated with the method
        //          GetMethods - Dictionary that links string in PropsToSelect to the GetLanes instance
        //          Entry control on Form1 and added to switch statement in Form1.comboBox1 index changed event


        public static string[] PropsToSelect = new string[]
        {
            "None",
            "Cartridge ID",
            "Cartridge Barcode",
            "RLF",
            "Date Range",
            "Instrument Type",
            "Lane Number(s)"
        };

        private static Dictionary<string, GetLanes> GetMethods { get; set; }

        private static List<Lane> GetNoFilter(List<Lane> lanesIn, string[] filterOn)
        {
            return lanesIn;
        }

        private static List<Lane> GetLanesFromCartID(List<Lane> lanesIn, string[] filterOn)
        {
            IEnumerable<Lane> temp = lanesIn.Where(x => filterOn.Contains(x.cartID));
            List<Lane> result = temp.Count() > 0 ? temp.ToList() : new List<Lane>();
            return result;
        }

        private static List<Lane> GetLanesFromCartBarcode(List<Lane> lanesIn, string[] filterOn)
        {
            IEnumerable<Lane> temp = lanesIn.Where(x => filterOn.Contains(x.CartBarcode));
            List<Lane> result = temp.Count() > 0 ? temp.ToList() : new List<Lane>();
            return result;
        }

        private static List<Lane> GetLanesFromRLF(List<Lane> lanesIn, string[] filterOn)
        {
            IEnumerable<Lane> temp = lanesIn.Where(x => filterOn.Contains(x.RLF));
            List<Lane> result = temp.Count() > 0 ? temp.ToList() : new List<Lane>();
            return result;
        }

        private static List<Lane> GetLanesFromDateRange(List<Lane> lanesIn, string[] filterOn)
        {
            DateTime[] range = new DateTime[2];
            DateTime d;
            bool check1 = DateTime.TryParseExact(filterOn[0], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d);
            if(!check1)
            {
                return new List<Lane>();
            }
            else
            {
                range[0] = d;
            }
            DateTime d2;
            bool check2 = DateTime.TryParseExact(filterOn[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out d2);
            if(!check2)
            {
                return new List<Lane>();
            }
            else
            {
                range[1] = d2;
            }
            IEnumerable<Lane> temp1 = lanesIn.Where(x => x.ParsedDate != null);
            IEnumerable<Lane> temp2 = temp1.Where(x => x.ParsedDate >= range[0] && x.ParsedDate <= range[1]);
            List<Lane> result = temp2.Count() > 0 ? temp2.ToList() : new List<Lane>();
            return result;
        }

        private static List<Lane> GetLanesFromInstrument(List<Lane> lanesIn, string[] filterOn)
        {
            List<Lane> result = new List<Lane>();
            if(filterOn[0].StartsWith("S"))
            {
                result.AddRange(lanesIn.Where(x => x.isSprint));
            }
            else
            {
                result.AddRange(lanesIn.Where(x => !x.isSprint));
            }
            return result;
        }

        private static List<Lane> GetLanesFromNumbers(List<Lane> lanesIn, string[] filterOn)
        {
            int[] laneNums = filterOn.Select(x => int.Parse(x)).OrderBy(y => y).ToArray();
            IEnumerable<Lane> temp = lanesIn.Where(x => x.LaneID >= laneNums[0] && x.LaneID <= laneNums[laneNums.Length - 1]);
            List<Lane> result = temp.Count() > 0 ? temp.ToList() : new List<Lane>();
            return result;
        }

        private delegate List<Lane> GetLanes(List<Lane> lanesIn, string[] filterOn);
        private static GetLanes NoFilter = new GetLanes(GetNoFilter);
        private static GetLanes FromCartID = new GetLanes(GetLanesFromCartID);
        private static GetLanes FromBarcode = new GetLanes(GetLanesFromCartBarcode);
        private static GetLanes FromRLF = new GetLanes(GetLanesFromRLF);
        private static GetLanes FromDate = new GetLanes(GetLanesFromDateRange);
        private static GetLanes FromInstrument = new GetLanes(GetLanesFromInstrument);
        private static GetLanes FromNumber = new GetLanes(GetLanesFromNumbers);

        private List<Lane> GetLanesOut(GetLanes method, List<Lane> lanesIn, string[] filterOn)
        {
            return method(lanesIn, filterOn);
        }

        public List<Lane> LanesOut { get; set; }
    }
}
