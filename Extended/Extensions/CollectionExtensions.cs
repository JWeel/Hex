using System;
using System.Collections.Generic;
using System.Linq;

namespace Extended.Extensions
{
    public static class CollectionExtensions
    {
        #region Random Element

        private static Random RandomGenerator { get; } = new Random();

        /// <summary> Returns a pseudo-randomly selected element from this collection. </summary>
        /// <param name="source"> The collection to return an element from. </param>
        /// <typeparam name="T"> The type of the elements of source. </typeparam>
        /// <returns> A pseudo-randomly selected element in the source collection. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
        /// <exception cref="InvalidOperationException"> <paramref name="source"/> is empty. </exception>
        public static T Random<T>(this ICollection<T> source) =>
            (source == null) ? throw new ArgumentNullException(nameof(source)) :
            (source.Count == 0) ? throw new InvalidOperationException("Collection is empty.") :
                source.ElementAt(RandomGenerator.Next(source.Count));

        #endregion
    }
}