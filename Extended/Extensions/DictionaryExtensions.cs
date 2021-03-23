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
    }
}