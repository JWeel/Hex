namespace Hex.Extensions
{
    public static class NumericExtensions
    {
        public static int IfOddAddOne(this int value) =>
            value % 2 == 0 ? value : value + 1;

        public static int IfOddSubtractOne(this int value) =>
            value % 2 == 0 ? value : value - 1;

        /// <summary> Determines whether or not this integer is even. </summary>
        public static bool IsEven(this int value) =>
            (value % 2 == 0);

        /// <summary> Determines whether or not this integer is odd. </summary>
        public static bool IsOdd(this int value) =>
            (value % 2 != 0);
    }
}