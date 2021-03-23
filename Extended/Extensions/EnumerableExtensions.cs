using System;
using System.Collections.Generic;
using System.Linq;

namespace Extended.Extensions
{
    public static class EnumerableExtensions
    {
        #region Each

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
                checked { index++; }
                action(element, index);
            }
        }

        #endregion

        #region Defer

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

        #endregion

        #region Iterate

        /// <summary> Moves through each element in a sequence without the overhead of storing them in a different data structure. </summary>
        public static void Iterate<T>(this IEnumerable<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
        }

        /// <summary> Moves through a specified number of elements in a sequence. </summary>
        /// <param name="iterations"> The number of elements to iterate over. If this exceeds the number of elements in the sequence, the remaining enumerations are skipped. </param>
        public static void Iterate<T>(this IEnumerable<T> enumerable, int iterations)
        {
            var enumerations = 0;
            var enumerator = enumerable.GetEnumerator();
            while ((enumerations++ < iterations) && (enumerator.MoveNext())) ;
        }

        #endregion

        #region Select Multi

        /// <summary> Projects each element of a sequence into multiple new forms. </summary>
        /// <param name="source"> A sequence of values to invoke a transform function on. </param>
        /// <param name="selectors"> A sequence of transform functions to apply sequentially to each source element. </param>
        /// <typeparam name="TSource"> The type of the elements of <paramref name="source"/>. </typeparam>
        /// <typeparam name="TResult"> The type of the value returned by elements of <paramref name="selectors"/>. </typeparam>
        /// <returns> An <see cref="IEnumerable{TResult}"/> whose elements are the result of invoking each transform function of <paramref name="selectors"/> on each element of <paramref name="source"/>. </returns>
        public static IEnumerable<TResult> SelectMulti<TSource, TResult>(this IEnumerable<TSource> source,
            params Func<TSource, TResult>[] selectors)
        {
            foreach (var element in source)
                foreach (var selector in selectors)
                    yield return selector(element);
        }

        #endregion

        #region Select With Next

        /// <summary> Projects each element of a sequence into a tuple with its next element. </summary>
        /// <remarks> If the sequence contains only one element, the result will be an empty sequence. </remarks>
        /// <param name="source"> A sequence of values. </param>
        /// <typeparam name="T"> The type of the elements of <paramref name="source"/>. </typeparam>
        public static IEnumerable<(T, T)> SelectWithNext<T>(this IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            var previous = default(T);
            var hasPrevious = false;
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (hasPrevious)
                {
                    yield return (previous, current);
                }
                previous = current;
                hasPrevious = true;
            }
        }

        #endregion

        #region Take While Defer

        /// <summary> Returns elements from a sequence as long as a specified condition is <see langword="true"/>.
        /// <br/> Unlike <see cref="Enumerable.TakeWhile{}"/>, the predicate is checked after the element is returned, which means the element which breaks the condition is also returned. </summary>
        /// <param name="source"> A sequence to return elements from. </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <typeparam name="T"></typeparam>
        /// <returns> An <see cref="IEnumerable{}"/> that contains the elements from the input sequence up to and including the element at which the test no longer passes. </returns>
        /// <remarks> This method is potentially dangerous because predicates are checked after elements are returned, which means that an element may have been returned for which the predicate would throw an exception on continued enumeration. </remarks>
        public static IEnumerable<T> TakeWhileDefer<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                yield return item;
                if (!predicate(item))
                    yield break;
            }
        }
            
        #endregion

        #region Yield

        /// <summary> Returns this <paramref name="value"/> inside an <see cref="IEnumerable{T}"/>. </summary> 
        public static IEnumerable<T> Yield<T>(this T value) { yield return value; }

        #endregion

        #region Concat

        /// <summary> Concatenates this sequence and a specified value. </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> left, T right) =>
            left.Concat(right.Yield());

        #endregion

        #region Except

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using default equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except) =>
             source.Where(x => !x.Equals(except));

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using specified equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except, IEqualityComparer<T> equalityComparer) =>
             source.Where(x => !equalityComparer.Equals(x, except));

        #endregion

        #region Flatten

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source) =>
            source.SelectMany(x => x);
            
        #endregion
    }
}