using Extended.Extensions;
using System.Collections.Generic;

namespace Hex.Extensions
{
    public static class DictionaryExtensions
    {
        #region Not Null Get

        /// <summary> If source is not <see langword="null"/> and it contains an entry with the specified key, returns the associated value.
        /// <br/> Otherwise returns the <see langword="default"/> value of type <typeparamref name="TValue"/>. </summary>
        public static TValue NotNullGetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
        {
            if (source == null)
                return default;
            return source.GetOrDefault(key);
        }

        /// <summary> If source is not <see langword="null"/>, returns <see cref="IDictionary{TKey,TValue}.TryGetValue(TKey, out TValue)"/>.
        /// <br/> Otherwise defaults the <see langword="out"/> parameter and returns <see langword="false"/>. </summary>
        public static bool NotNullTryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            if (source == null)
            {
                value = default;
                return false;
            }
            return source.TryGetValue(key, out value);
        }

        /// <summary> If source is not <see langword="null"/>, returns <see cref="IDictionary{TKey,TValue}.ContainsKey(TKey)"/>.
        /// <br/> Otherwise returns <see langword="false"/>. </summary>
        public static bool NotNullContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
        {
            if (source == null)
                return false;
            return source.ContainsKey(key);
        }

        #endregion
    }
}