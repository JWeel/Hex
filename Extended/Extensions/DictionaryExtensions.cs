using System;
using System.Collections.Generic;

namespace Extended.Extensions
{
    public static class DictionaryExtensions
    {
        #region Add To Dictionary

        /// <summary> Adds an element with the provided key and value in the tuple to the dictionary. </summary>
        public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> map, (TKey Key, TValue Value) tuple) =>
            map.Add(tuple.Key, tuple.Value);

        #endregion

        #region Remove From Dictionary

        /// <summary> Removes the first occurence of the key and value in the tuple from the dictionary. </summary>
        /// <returns> <see langword="true"/> if an item was successfully removed from the <see cref="ICollection{}"/>; otherwise, <see langword="false"/>. 
        /// <br/> This method also returns <see langword="false"/> if the item is not found in the <see cref="ICollection{}"/>. </returns>
        public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> map, (TKey Key, TValue Value) tuple) =>
            map.Remove(KeyValuePair.Create(tuple.Key, tuple.Value));

        #endregion

        #region Coalesce

        /// <summary> Gets the value associated with the specified key, or a default value if the key does not exist in the map. </summary>
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> map, TKey key) =>
            map.TryGetValue(key, out var value) ? value : default;

        #endregion

        #region Create

        /// <summary> Creates a <see cref="Dictionary{TKey, TValue"/> from a sequence of <see cref="KeyValuePair{TKey, TValue}"/> instances. </summary>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items) =>
            new Dictionary<TKey, TValue>(items);

        #endregion

        #region Get Or Set

        /// <summary> Attempts to get the value associated with the specified key.
        /// <br/> If the key does not exist in the map, a provided <see cref="Func{TValue"/> is invoked to retrieve an alternate value. This value will be added with the key to the map, and will then be returned. </summary>
        public static TValue GetOrSet<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TValue> func)
        {
            if (source.TryGetValue(key, out var value))
                return value;
            var alternate = func();
            source[key] = alternate;
            return alternate;
        }

        #endregion
    }
}