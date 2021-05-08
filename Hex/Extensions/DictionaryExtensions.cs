using System.Collections.Generic;

namespace Hex.Extensions
{
    public static class DictionaryExtensions
    {
        #region Not Null TryGetValue

        /// <summary> If source is not <see langword="null"/>, returns <see cref="IDictionary{TKey,TValue}.TryGetValue(TKey, out TValue)"/>.
        /// <br/> If source is <see langword="null"/>, defaults the <see langword="out"/> parameter and returns <see langword="false"/>. </summary>
        public static bool NotNullTryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            if (source == null)
            {
                value = default;
                return false;
            }
            return source.TryGetValue(key, out value);
        }
            
        #endregion
    }
}