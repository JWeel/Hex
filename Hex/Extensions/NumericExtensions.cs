namespace Hex.Extensions
{
    public static class NumericExtensions
    {
        /// <summary> Performs modular arithmetic of this integer with the specified modulus. </summary>
        /// <param name="n"> The input value. </param>
        /// <param name="m"> The modulus. </param>
        /// <returns> The result of the modulo operation of <paramref name="n"/> with modulus <paramref name="m"/>. </returns>
        /// <exception cref="System.DivideByZeroException"> The modulus is 0. </exception>
        public static int Modulo(this int n, int m) =>
            ((n %= m) < 0) ? n + m : n;
    }
}