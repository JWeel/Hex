using System;

namespace Mogi.Inversion
{
    /// <summary> Wraps a delegate with a function to determine its priority and a predicate that determines if lower priority delegates should be fired. </summary>
    /// <typeparam name="T1"> The type of the argument that is passed into the delegate. </typeparam>
    public class PrioritizableDelegate<T1> : IComparable<PrioritizableDelegate<T1>>
    {
        #region Constructors

        /// <summary> Initializes a new instance with a delegate, a priority function and a predicate. </summary>
        /// <param name="action"> The delegate action wrapped by this instance. </param>
        /// <param name="priorityFunc"> The funcion that determines the priority of the delegate. </param>
        /// <param name="preventFunc"> The predicate that determines if lower priority delegates should fire. </param>
        public PrioritizableDelegate(Action<T1> action, Func<int> priorityFunc, Func<bool> preventFunc)
        {
            this.Delegate = action;
            this.Priority = priorityFunc;
            this.Prevent = preventFunc;
        }

        #endregion

        #region Properties

        /// <summary> The delegate action. </summary>
        public Action<T1> Delegate { get; }

        /// <summary> The funcion that determines the priority of the delegate. </summary>
        public Func<int> Priority { get; }

        /// <summary> The predicate that determines if lower priority delegates should fire. </summary>
        public Func<bool> Prevent { get; }

        #endregion

        #region IComparable Implementation

        /// <summary> Compares this instance to another and returns an indication of their relative values. </summary>
        /// <param name="other"> An instance to compare. </param>
        /// <returns> Less than zero - priority of this instance is less than priority of <paramref name="other"/>. 
        /// <br/> Zero - priority and delegate of this instance are equal to priority and delegate of <paramref name="other"/>. 
        /// <br/> Greater than zero - priority of this instance is greater than priority of <paramref name="other"/> OR priority is the same but delegates are different. </returns>
        public int CompareTo(PrioritizableDelegate<T1> other)
        {
            const int COMPARISON_EQUAL = 0;
            const int COMPARISON_GREATER = 1;
            var comparison = this.Priority().CompareTo(other.Priority());
            if (comparison != COMPARISON_EQUAL)
                return comparison;
            return Object.ReferenceEquals(this.Delegate, other.Delegate) ? COMPARISON_EQUAL : COMPARISON_GREATER;
        }

        #endregion

        #region Implicit Operators

        /// <summary> Implicitly convers the tuple to a prioritizable delegate. </summary>
        /// <param name="tuple"> The tuple with all parameters needed to create a prioritizable delegate. </param>
        /// <typeparam name="T1"> The type of the argument that is passed into the delegate. </typeparam>
        public static implicit operator PrioritizableDelegate<T1>((Action<T1> Action, Func<int> PriorityFunc, Func<bool> PreventFunc) tuple) =>
            new PrioritizableDelegate<T1>(tuple.Action, tuple.PriorityFunc, tuple.PreventFunc);

        #endregion
    }
}