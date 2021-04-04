using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extended.Extensions
{
    public static class CollectionExtensions
    {
        #region Random Element

        private static Random RandomGenerator { get; } = new Random();

        public static T Random<T>(this ICollection<T> source) =>
            (source.Count == 0) ? throw new InvalidOperationException("Collection contains zero elements.") :
                source.ElementAt(RandomGenerator.Next(source.Count));

        #endregion
    }
}