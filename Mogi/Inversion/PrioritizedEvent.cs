using Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mogi.Inversion
{
    /// <summary> Provides encapsulation of an event that when triggered invokes delegates in order of priority. The delegates return a value which indicates whether to stop lower priority delegates from firing. </summary>
    /// <typeparam name="T1"> The type of the argument passed into subscribed delegates. </typeparam>
    public class PrioritizedEvent<T1>
    {
        #region Constructors

        // TODO: dont use predicate<T>, instead use action<T> and add another predicate that takes no arg

        /// <summary> Initializes a new instance. This should only be called by the <see cref="+"/> and <see cref="-"/> operators. </summary>
        private PrioritizedEvent(params (Predicate<T1> Delegate, Func<int> GetPriority)[] orderedDelegates)
        {
            this.OrderedDelegates = new SortedSet<(Predicate<T1>, Func<int>)>(orderedDelegates, PRIORITY_COMPARER);
        }

        #endregion

        #region Data Members

        /// <summary> The comparer that determines the order in which delegates are invoked. </summary>
        private static readonly IComparer<(Predicate<T1>, Func<int>)> PRIORITY_COMPARER =
            Comparer<(Predicate<T1> Delegate, Func<int> GetPriority)>.Create((x, y) =>
            {
                var comparison = x.GetPriority().CompareTo(y.GetPriority());
                if (comparison != 0)
                    return comparison;
                return Object.ReferenceEquals(x.Delegate, y.Delegate) ? 0 : 1;
            });

        /// <summary> The ordered set of delegates to invoke when the event is raised. </summary>
        protected SortedSet<(Predicate<T1> Delegate, Func<int> GetPriority)> OrderedDelegates { get; }

        #endregion

        #region Methods

        /// <summary> Invokes all attached delegates in order of priority. </summary>
        /// <param name="arg1"> The argument that is passed into the delegates. </param>
        [DebuggerStepThrough]
        public void Invoke(T1 arg1)
        {
            this.OrderedDelegates
                .TakeWhile(x => (x.Delegate?.Invoke(arg1) ?? false))
                .Iterate();
        }

        /// <summary> Invokes all attached delegates in reversed order of priority. </summary>
        /// <param name="arg1"> The argument that is passed into the delegates. </param>
        public void InvokeReverse(T1 arg1)
        {
            this.OrderedDelegates
                .Reverse()
                .TakeWhile(x => (x.Delegate?.Invoke(arg1) ?? false))
                .Iterate();
        }

        /// <summary> Subscribes a delegate to the event with function that determines its priority. </summary>
        /// <param name="instance"> The event to which a delegate will be subscribed. </param>
        /// <param name="tuple"> The tuple that contains the subscribing delegate and a function that determines its priority. </param>
        public static PrioritizedEvent<T1> operator +(PrioritizedEvent<T1> instance, (Predicate<T1> Delegate, Func<int> GetPriority) tuple)
        {
            if (instance == null)
                instance = new PrioritizedEvent<T1>();
            instance.OrderedDelegates.Add(tuple);
            return instance;
        }

        /// <summary> Unsubscribes a delegate to the event with function that determines its priority. </summary>
        /// <param name="instance"> The event from which a delegate will be unsubscribed.. </param>
        /// <param name="predicate"> The unsubscribing delegate. </param>
        public static PrioritizedEvent<T1> operator -(PrioritizedEvent<T1> instance, Predicate<T1> predicate)
        {
            if (instance == null)
                return new PrioritizedEvent<T1>();
            instance.OrderedDelegates.RemoveWhere(x => (x.Delegate == predicate));
            return instance;
        }

        #endregion
    }
}