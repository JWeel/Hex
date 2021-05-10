using System;

namespace Hex.Extensions
{
    public static class FuncExtensions
    {
        #region Try Invoke

        /// <summary> Invokes this func if it is not null, storing the result in an out parameter. <br/> The returned boolean indicates whether the func was invoked or not. </summary>
        public static bool TryNotNullInvoke<TResult>(this Func<TResult> func, out TResult result)
        {
            if (func == null)
            {
                result = default;
                return false;
            }
            result = func();
            return true;
        }

        /// <summary> Invokes this func if it is not null, storing the result in an out parameter. <br/> The returned boolean indicates whether the func was invoked or not. </summary>
        public static bool TryNotNullInvoke<T, TResult>(this Func<T, TResult> func, T arg, out TResult result)
        {
            if (func == null)
            {
                result = default;
                return false;
            }
            result = func(arg);
            return true;
        }

        /// <summary> Invokes this func if it is not null, storing the result in an out parameter. <br/> The returned boolean indicates whether the func was invoked or not. </summary>
        public static bool TryNotNullInvoke<T1, T2, TResult>(this Func<T1, T2, TResult> func, T1 arg1, T2 arg2, out TResult result)
        {
            if (func == null)
            {
                result = default;
                return false;
            }
            result = func(arg1, arg2);
            return true;
        }

        #endregion
    }
}