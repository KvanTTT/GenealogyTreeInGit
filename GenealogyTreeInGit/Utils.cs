using System;
using System.Collections.Generic;
using System.Text;

namespace GenealogyTreeInGit
{
    public static class Utils
    {
        public static string JoinNotEmpty(params string[] strs)
        {
            var result = new StringBuilder();

            foreach (string str in strs)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    result.Append(str);
                    result.Append(' ');
                }
            }

            if (result.Length > 0)
                result.Remove(result.Length - 1, 1);

            return result.ToString();
        }

        public static bool TryGetFirstValue<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out List<TValue> values) && values.Count > 0)
            {
                value = values[0];
                return true;
            }

            value = default(TValue);
            return false;
        }

        public static bool IsDateUndefined(this DateTime dateTime)
        {
            return (dateTime - default(DateTime)).TotalDays < 1;
        }
    }
}
