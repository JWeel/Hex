using Extended.Exceptions;
using System;

namespace Extended.Extensions
{
    public static class EnumExtensions
    {
        #region Invalid

        /// <summary> Creates an <see cref="InvalidEnumException{}"/> from this value. </summary>
        /// <remarks> This can be used to throw an exception on unsupported switch cases. </remarks>
        public static InvalidEnumException<T> Invalid<T>(this T value) where T : Enum =>
            new InvalidEnumException<T>(value);

        #endregion
    }
}