using System;
using System.Diagnostics;

namespace Extended.Extensions
{
    public static class GenericExtensions
    {
        #region With Side Effect

        /// <summary> Returns this instance after passing it as argument to the invocation of a given <paramref name="action"/>. </summary>
        /// <remarks> The instance will be returned even if <paramref name="action"/> is null (in which case the action is ignored). </remarks>
        /// <param name="action"> The action to invoke using the given instance. </param>
        [DebuggerStepThrough]
        public static T With<T>(this T value, Action<T> action)
        {
            action?.Invoke(value);
            return value;
        }

        #endregion

        #region Into

        /// <summary> Passes this value into the invocation of a <see cref="Func{,}"/> and returns the result. </summary>
        /// <param name="func"> The function to invoke using the given instance. </param>
        [DebuggerStepThrough]
        public static TResult Into<TValue, TResult>(this TValue value, Func<TValue, TResult> func) =>
            func(value);

        #endregion
    }
}