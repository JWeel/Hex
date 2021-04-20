using Extended.Exceptions;
using System;

namespace Extended.Extensions
{
    public static class EnumExtensions
    {
        #region Invalid

        /// <summary> Creates a <see cref="InvalidEnumException{}"/> from this value. </summary>
        /// <remarks> This can be used to throw an exception in the default case when switching on an enum. </remarks>
        public static InvalidEnumException<T> Invalid<T>(this T value) where T : Enum =>
            new InvalidEnumException<T>(value);

        #endregion
    }
}