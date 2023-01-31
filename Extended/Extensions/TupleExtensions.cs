using System;

namespace Extended.Extensions
{
    public static class TupleExtensions
    {
        #region Transform

        /// <summary> Transforms the first item of this tuple using a specified func while preserving the second item. </summary>
        public static (T1New, T2) Transform1<T1Old, T1New, T2>(this (T1Old, T2) tuple,
            Func<T1Old, T1New> transform)
        {
            return tuple.Transform(transform, x => x);
        }

        /// <summary> Transforms the second item of this tuple using a specified func while preserving the first item. </summary>
        public static (T1, T2New) Transform2<T1, T2Old, T2New>(this (T1, T2Old) tuple,
            Func<T2Old, T2New> transform)
        {
            return tuple.Transform(x => x, transform);
        }

        /// <summary> Transforms both items of this tuple using two specified funcs. </summary>
        public static (T1New, T2New) Transform<T1Old, T1New, T2Old, T2New>(this (T1Old, T2Old) tuple,
            Func<T1Old, T1New> transform1, Func<T2Old, T2New> transform2)
        {
            var (item1, item2) = tuple;
            return (transform1(item1), transform2(item2));
        }

        #endregion
    }
}