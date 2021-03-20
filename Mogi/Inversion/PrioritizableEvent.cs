using Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mogi.Inversion
{
    /// <summary> Provides encapsulation of an event that when triggered invokes delegates in order of priority. After each delegate invocation, a predicate is checked which if satisfied prevents any lower priority delegates from firing. </summary>
    /// <typeparam name="T1"> The type of the argument passed into subscribed delegates. </typeparam>
    public class PrioritizableEvent<T1>
    {
        #region Constructors

        /// <summary> Initializes a new instance. This should only be called by the <see cref="+"/> and <see cref="-"/> operators. </summary>
        private PrioritizableEvent(params PrioritizableDelegate<T1>[] orderedDelegates)
        {
            this.OrderedDelegates = new SortedSet<PrioritizableDelegate<T1>>(orderedDelegates);
        }

        #endregion

        #region Data Members

        /// <summary> The ordered set of delegates to invoke when the event is raised. </summary>
        protected SortedSet<PrioritizableDelegate<T1>> OrderedDelegates { get; }

        #endregion

        #region Methods

        /// <summary> Invokes all attached delegates in order of priority. </summary>
        /// <param name="arg1"> The argument that is passed into the delegates. </param>
        [DebuggerStepThrough]
        public void Invoke(T1 arg1)
        {
            this.OrderedDelegates
                .Defer(x => x.Delegate?.Invoke(arg1))
                .TakeWhile(x => !x.Prevent())
                .Iterate();
        }

        /// <summary> Invokes all attached delegates in reversed order of priority. </summary>
        /// <param name="arg1"> The argument that is passed into the delegates. </param>
        [DebuggerStepThrough]
        public void InvokeReverse(T1 arg1)
        {
            // TODO efficient reverse
            this.OrderedDelegates
                .Reverse()
                .Defer(x => x.Delegate?.Invoke(arg1))
                .TakeWhile(x => !x.Prevent())
                .Iterate();
        }

        /// <summary> Subscribes a delegate to the event with function that determines its priority. </summary>
        /// <param name="instance"> The event to which a delegate will be subscribed. </param>
        /// <param name="prioritizableDelegate"> The wrapper containing the delegate, its priority function and its predicate. </param>
        public static PrioritizableEvent<T1> operator +(PrioritizableEvent<T1> instance, PrioritizableDelegate<T1> prioritizableDelegate)
        {
            if (instance == null)
                instance = new PrioritizableEvent<T1>();
            instance.OrderedDelegates.Add(prioritizableDelegate);
            return instance;
        }

        /// <summary> Unsubscribes a delegate to the event with function that determines its priority. </summary>
        /// <param name="instance"> The event from which a delegate will be unsubscribed. </param>
        /// <param name="action"> The unsubscribing delegate. </param>
        public static PrioritizableEvent<T1> operator -(PrioritizableEvent<T1> instance, Action<T1> action)
        {
            if (instance == null)
                return new PrioritizableEvent<T1>();
            instance.OrderedDelegates.RemoveWhere(x => (x.Delegate == action));
            return instance;
        }

        #endregion
    }
}