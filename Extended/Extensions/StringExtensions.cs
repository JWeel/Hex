namespace Extended.Extensions
{
    public static class StringExtensions
    {
        #region Is Null Or Empty/WhiteSpace

        /// <summary> Indicates whether this string is <see langword="null"/> or an empty string (""). </summary>
        public static bool IsNullOrEmpty(this string value) =>
            string.IsNullOrEmpty(value);

        /// <summary> Indicates whether this string is <see langword="null"/>, empty, or consists only of white-space characters. </summary>
        public static bool IsNullOrWhiteSpace(this string value) =>
            string.IsNullOrWhiteSpace(value);

        #endregion
    }
}