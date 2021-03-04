using System.Collections.Generic;

namespace Extended.Extensions
{
    public static class DictionaryExtensions
    {
        #region Add To Dictionary

        /// <summary> Adds an element with the provided key and value from a tuple to the dictionary. </summary>
        public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> map, (TKey Key, TValue Value) tuple) =>
            map.Add(tuple.Key, tuple.Value);

        #endregion
    }
}