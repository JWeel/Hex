using System.Collections.Generic;

namespace Extended.Generators
{
    /// <summary> Exposes methods to generate numeric values. </summary>
    public static class Numeric
    {
        #region Range

        /// <summary> Returns a sequence of integers spanning the range [0, <paramref name="n"/>) in ascending order. </summary>
        /// <remarks> For example, with <paramref name="n"/> 3, the sequence will be {0, 1, 2}. </remarks>
        /// <param name="n"> The amount of integers in the range. </param>
        public static IEnumerable<int> Range(int n) =>
            Numeric.Range(0, n);

        /// <summary> Returns a sequence of integers spanning the range [<paramref name="start"/>, <paramref name="end"/>) in ascending order. </summary>
        /// <remarks> For example, with <paramref name="start"/> 0 and <paramref name="end"/> 3, the sequence will be {0, 1, 2}. </remarks>
        /// <param name="start"> The inclusive lower bound of the range. </param>
        /// <param name="end"> The exclusive upper bound of the range. </param>
        public static IEnumerable<int> Range(int start, int end) =>
            Numeric.Range(start, end, +1);

        /// <summary> Returns a sequence of integers starting from <paramref name="start"/> and ending before <paramref name="end"/> with a specified <paramref name="step"/> increase for each subsequent element in the sequence. </summary>
        /// <remarks> For example, with <paramref name="start"/> 0, <paramref name="end"/> 5 and <paramref name="step"/> 2, the sequence will be {0, 2, 4}.
        /// <para/> A positive <paramref name="step"/> will result in an ascending range and a negative <paramref name="step"/> in a descending range. The sequence may be infinitely long if <paramref name="step"/> is 0. </remarks>
        /// <param name="start"> The inclusive lower bound of the range. </param>
        /// <param name="end"> The exclusive upper bound of the range. </param>
        /// <param name="step"> The exclusive upper bound of the range. </param>
        public static IEnumerable<int> Range(int start, int end, int step)
        {
            while (start < end)
            {
                yield return start;
                start += step;
            }
        }

        #endregion
    }
}