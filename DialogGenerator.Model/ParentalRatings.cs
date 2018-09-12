using System.Collections.Generic;

namespace DialogGenerator.Model
{
    public static class ParentalRatings
    {
        /// <summary>
        /// G - General Audiences
        /// PG - Parental Guidance Suggested
        /// PG13 - Parents Strongly Cautioned
        /// R - Restricted - under 17 requires accompanying parent
        /// </summary>
        private static readonly Dictionary<string, int> mcDict = new Dictionary<string, int>
        {
            {"G", 1},
            {"PG", 2},
            {"PG13", 3},
            {"R", 4}
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_ratingString"></param>
        /// <returns></returns>
        public static int GetNumeric(string _ratingString)
        {
            // Try to get the result in the static Dictionary
            int _result;
            if (mcDict.TryGetValue(_ratingString, out _result))
                return _result;
            return -1;
        }
    }
}
