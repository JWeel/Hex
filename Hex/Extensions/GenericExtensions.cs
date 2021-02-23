using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hex.Extensions
{
    public static class GenericExtensions
    {
        #region Enumeration Each/Defer/Enumerate

        /// <summary> Performs a specified action for each element in a sequence. </summary>
        public static void Each<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var element in enumerable)
                action(element);
        }

        /// <summary> Performs a specified action for each element and their index in a sequence. </summary>
        public static void Each<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            int index = -1;
            foreach (var element in enumerable)
            {
                checked
                { index++; }
                action(element, index);
            }
        }

        /// <summary> Defers an action to be performed for each enumerated element in a sequence. </summary>
        public static IEnumerable<T> Defer<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var element in enumerable)
                yield return element.With(action);
        }

        /// <summary> Defers an action to be performed for each enumerated element and their index in a sequence. </summary>
        public static IEnumerable<T> Defer<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            int index = -1;
            foreach (var element in enumerable)
            {
                checked
                { index++; }
                yield return element.With(x => action(x, index));
            }
        }

        /// <summary> Moves through each element in a sequence without the overhead of storing them in a different data structure. </summary>
        public static void Iterate<T>(this IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
        }

        /// <summary> Moves through a specified number of elements in a sequence. </summary>
        /// <param name="iterations"> The number of elements to iterate over. If this exceeds the number of elements in the sequence, the remaining enumerations are ignored. </param>
        public static void Iterate<T>(this IEnumerable<T> enumerable, int iterations)
        {
            var enumerations = 0;
            var enumerator = enumerable.GetEnumerator();
            while ((enumerations++ < iterations) && (enumerator.MoveNext())) ;
        }

        #endregion   

        #region With Side Effect

        // maybe rename to Side or SideEffect
        /// <summary> Returns this instance after passing it as argument to the invocation of a given <paramref name="action"/>. </summary>
        /// <remarks> The instance will be returned even if <paramref name="action"/> is null (in which case the action is ignored). </remarks>
        [DebuggerStepThrough]
        public static T With<T>(this T value, Action<T> action)
        {
            action?.Invoke(value);
            return value;
        }

        #endregion

        #region Into

        /// <summary> Passes this value into the invocation of a <see cref="Func{,}"/> and returns the result. </summary>
        [DebuggerStepThrough]
        public static TResult Into<TValue, TResult>(this TValue value, Func<TValue, TResult> func) =>
            func(value);

        #endregion

        #region Case

        /// <summary> Invokes a given action only if this <paramref name="condition"/> is <see langword="true"/>. </summary>
        public static void Case(this bool condition, Action action)
        {
            if (condition) action?.Invoke();
        }

        #endregion

        #region Select Multi

        /// <summary> Projects each element of a sequence into multiple new forms. </summary>
        /// <param name="source"> A sequence of values to invoke a transform function on. </param>
        /// <param name="selectors"> A sequence of transform functions to apply sequentially to each source element. </param>
        /// <typeparam name="TSource"> The type of the elements of source. </typeparam>
        /// <typeparam name="TResult"> The type of the value returned by selector. </typeparam>
        /// <returns> An <see cref="IEnumerable{TResult}"/> whose elements are the result of invoking each transform function of <paramref name="selectors"/> on each element of <paramref name="source"/>. </returns>
        public static IEnumerable<TResult> SelectMulti<TSource, TResult>(this IEnumerable<TSource> source,
            params Func<TSource, TResult>[] selectors)
        {
            foreach (var element in source)
                foreach (var selector in selectors)
                    yield return selector(element);
        }

        #endregion
    }
}