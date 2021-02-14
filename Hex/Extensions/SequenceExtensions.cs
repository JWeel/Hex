using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Extensions
{
    public static class SequenceExtensions
    {
        #region Aggregate Methods

        public static (int Min, int Max) MinMax<T>(this IEnumerable<T> source, Func<T, int> selector) =>
            source.Aggregate(seed: (Min: int.MaxValue, Max: int.MinValue), 
                (aggregate, instance) => selector(instance)
                    .Into(value => (Math.Min(aggregate.Min, value), Math.Max(aggregate.Max, value))));

        #endregion
    }
}