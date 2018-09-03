using System;
using System.Collections.Generic;
using System.Linq;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomDate
    {
        private static char[] _separators = new char[] { ' ' };

        private static List<string> Months = new List<string>()
        {
            "JAN",
            "FEB",
            "MAR",
            "APR",
            "MAY",
            "JUN",
            "JUL",
            "AUG",
            "SEP",
            "OCT",
            "NOV",
            "DEC"
        };

        private static Dictionary<string, GedcomDateType> DateTypesMap = new Dictionary<string, GedcomDateType>()
        {
            ["ABT"] = GedcomDateType.About,
            ["CAL"] = GedcomDateType.Calculated,
            ["EST"] = GedcomDateType.Estimated,
            ["AFT"] = GedcomDateType.After,
            ["BEF"] = GedcomDateType.Before
        };

        private bool _isDefaultDateCalculated;
        private DateTime _defaultDate;

        public static ILogger Logger { get; set; }

        public string Raw { get; }

        public bool IsDefined => Raw != null;

        public GedcomDate(string raw)
        {
            Raw = raw;
        }

        public static implicit operator GedcomDate(string str)
        {
            return new GedcomDate(str);
        }

        public static explicit operator DateTime(GedcomDate date)
        {
            return date.DefaultDate;
        }

        public DateTime DefaultDate
        {
            get
            {
                if (_isDefaultDateCalculated)
                {
                    return _defaultDate;
                }

                if (Raw != null)
                {
                    string[] strs = Raw.Split(_separators, StringSplitOptions.RemoveEmptyEntries).Where(str => str != "?").ToArray();
                    int startIndex = strs.Length > 0 && DateTypesMap.TryGetValue(strs[0], out _) ? 1 : 0;

                    if (!TryParseDate(strs, startIndex, out _defaultDate))
                    {
                        Logger?.LogError($"Unable to parse date {Raw}");
                    }
                }

                _isDefaultDateCalculated = true;
                return _defaultDate;
            }
        }

        public override string ToString()
        {
            return Raw ?? "undefined";
        }

        private static bool TryParseDate(string[] strs, int startIndex, out DateTime dateTime)
        {
            dateTime = default(DateTime);

            int day = 1, monthIndex = 0, year = 1;
            string dayStr = null, monthStr = null, yearStr = null;
            int dataFragmentCount = strs.Length - startIndex;

            if (dataFragmentCount == 3)
            {
                dayStr = strs[startIndex];
                monthStr = strs[startIndex + 1];
                yearStr = strs[startIndex + 2];
            }
            else if (dataFragmentCount == 2)
            {
                monthStr = strs[startIndex];
                yearStr = strs[startIndex + 1];
            }
            else if (dataFragmentCount == 1)
            {
                yearStr = strs[startIndex];
            }
            else
            {
                return false;
            }

            if (dayStr != null && !int.TryParse(dayStr, out day))
            {
                return false;
            }

            if (monthStr != null)
            {
                monthIndex = Months.IndexOf(monthStr);

                if (monthIndex == -1)
                    return false;
            }

            if (yearStr != null)
            {
                if (yearStr.Length > 4 && yearStr[4] == '/')  // Handle yyyy/yy format
                    yearStr = yearStr.Remove(4);

                if (!int.TryParse(yearStr, out year))
                    return false;
            }

            try
            {
                dateTime = new DateTime(year, monthIndex + 1, day);
            }
            catch
            {
                try
                {
                    dateTime = new DateTime(year, monthIndex + 1, 1);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}
