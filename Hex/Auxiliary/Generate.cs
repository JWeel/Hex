using System.Collections.Generic;

namespace Hex.Auxiliary
{
    public class Generate
    {
        #region Range

        public static IEnumerable<int> Range(int end) =>
            Generate.Range(start: 0, end);

        public static IEnumerable<int> Range(int start, int end) =>
            Generate.Range(start, end, step: 1);

        public static IEnumerable<int> Range(int start, int end, int step)
        {
            while (start < end)
            {
                yield return start;
                start += step;
            }
        }

        public static IEnumerable<int> RangeDescending(int start) =>
            Generate.RangeDescending(start, end: 0);

        public static IEnumerable<int> RangeDescending(int start, int end) =>
            Generate.RangeDescending(start, end, step: 1);

        public static IEnumerable<int> RangeDescending(int start, int end, int step)
        {
            while (start > end)
            {
                yield return start;
                start -= step;
            }
        }

        #endregion
    }
}