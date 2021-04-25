using System.Collections.Generic;

namespace Extended.Generators
{
    /// <summary> Exposes methods to return cached instances of empty objects. </summary>
    public static class Empty<T>
    {
        #region Array

        /// <summary> Returns an empty array of type <typeparamref name="T"/>. </summary>
        public static T[] Array { get; } = new T[0];

        #endregion

        #region Sequence

        /// <summary> Returns an empty <see cref="IEnumerable{T}"/> of type <typeparamref name="T"/>. </summary>
        public static IEnumerable<T> Sequence { get { yield break; } }

        #endregion
    }
}