using System.Collections.Generic;

namespace Extended.Generators
{
    /// <summary> Exposes methods to return numeric ranges. </summary>
    public static class Range
    {
        #region Ascending

        /// <summary> Returns a sequence of integers spanning the range [0, <paramref name="n"/>) in ascending order. </summary>
        /// <remarks> For example, with <paramref name="n"/> 3, the sequence will be {0, 1, 2}. </remarks>
        /// <param name="n"> The amount of integers in the range. </param>
        public static IEnumerable<int> Ascend(int n)
        {
            var i = 0;
            while (i < n)
                yield return i++;
        }

        // public static IEnumerable<int> Ascend(int start, int end)
        // {
        //     while (start < end)
        //         yield return start++;
        // }

        // public static IEnumerable<int> Ascend(int start, int end, int step)
        // {
        //     while (start < end)
        //     {
        //         yield return start;
        //         start += step;
        //     }
        // }

        #endregion

        #region Descending

        /// <summary> Returns a sequence of integers spanning the range [0, <paramref name="n"/>) in descending order. </summary>
        /// <remarks> For example, with <paramref name="n"/> 3, the sequence will be {2, 1, 0}. </remarks>
        /// <param name="n"> The amount of integers in the range. </param>
        public static IEnumerable<int> Descend(int n)
        {
            var i = n - 1;
            while (i >= 0)
                yield return i--;
        }

        #endregion
    }
}