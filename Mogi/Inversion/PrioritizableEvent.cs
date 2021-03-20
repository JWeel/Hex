using Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mogi.Inversion
{
    /// <summary> Provides encapsulation of an event that when triggered invokes delegates in order of priority. After each delegate invocation, a predicate is checked which if satisfied can prevent lower priority delegates from firing. </summary>
    /// <typeparam name="T"> The type of the argument passed into subscribing delegates. </typeparam>
    /// <remarks> This custom event class does not have language level support like <see langword="event"/>, therefore:
    /// <br/> - interfaces cannot expose it as field (must be property)
    /// <br/> - this event can be set to null by anyone, not just the owner
    /// <br/> Furthermore, this class exposes an equivalent to <see cref="Delegate.GetInvocationList"/>, but it returns <see cref="IEnumerable{}"/> instead of array. </remarks>
    public class PrioritizableEvent<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance. This should only be called by the <see cref="+"/> and <see cref="-"/> operators. </summary>
        private PrioritizableEvent(params PrioritizableDelegate<Action<T>>[] orderedDelegates)
        {
            this.OrderedDelegates = new SortedSet<PrioritizableDelegate<Action<T>>>(orderedDelegates);
        }

        #endregion

        #region Data Members

        /// <summary> The ordered set of delegates to invoke when the event is raised. </summary>
        protected SortedSet<PrioritizableDelegate<Action<T>>> OrderedDelegates { get; }

        #endregion

        #region Methods

        /// <summary> Returns the invocation list of the delegate. </summary>
        /// <returns> A collection of prioritizable delegates attached to this event. </returns>
        public IEnumerable<PrioritizableDelegate<Action<T>>> GetInvocationList() =>
            this.OrderedDelegates;

        /// <summary> Invokes attached delegates in order of priority until the end or until one prevents further invocations. </summary>
        /// <param name="arg"> The argument that is passed into the delegates. </param>
        public void Invoke(T arg)
        {
            this.OrderedDelegates
                .Defer(x => x.Delegate?.Invoke(arg))
                .TakeWhile(x => !x.Prevent())
                .Iterate();
        }

        /// <summary> Subscribes a delegate to the event. </summary>
        /// <param name="instance"> The event to which a delegate will be subscribed. </param>
        /// <param name="PrioritizableDelegateBase"> The wrapper containing the delegate, its priority function and its predicate. </param>
        public static PrioritizableEvent<T> operator +(PrioritizableEvent<T> instance, PrioritizableDelegate<Action<T>> prioritizableEvent)
        {
            if (instance == null)
                instance = new PrioritizableEvent<T>();
            instance.OrderedDelegates.Add(prioritizableEvent);
            return instance;
        }

        /// <summary> Unsubscribes a delegate from the event. </summary>
        /// <param name="instance"> The event from which a delegate will be unsubscribed. </param>
        /// <param name="delegate"> The unsubscribing delegate. </param>
        public static PrioritizableEvent<T> operator -(PrioritizableEvent<T> instance, Action<T> @delegate)
        {
            if (instance == null)
                return new PrioritizableEvent<T>();
            instance.OrderedDelegates.RemoveWhere(x => (x.Delegate == @delegate));
            return instance;
        }

        #endregion
    }

    /// <summary> Provides encapsulation of an event that when triggered invokes delegates in order of priority. After each delegate invocation, a predicate is checked which if satisfied can prevent lower priority delegates from firing. </summary>
    /// <typeparam name="T1"> The type of the first argument passed into subscribing delegates. </typeparam>
    /// <typeparam name="T2"> The type of the second argument passed into subscribing delegates. </typeparam>
    /// <remarks> This custom event class does not have language level support like <see langword="event"/>, therefore:
    /// <br/> - interfaces cannot expose it as field (must be property)
    /// <br/> - this event can be set to null by anyone, not just the owner
    /// <br/> Furthermore, this class exposes an equivalent to <see cref="Delegate.GetInvocationList"/>, but it returns <see cref="IEnumerable{}"/> instead of array. </remarks>
    public class PrioritizableEvent<T1, T2>
    {
        #region Constructors

        /// <summary> Initializes a new instance. This should only be called by the <see cref="+"/> and <see cref="-"/> operators. </summary>
        private PrioritizableEvent(params PrioritizableDelegate<Action<T1, T2>>[] orderedDelegates)
        {
            this.OrderedDelegates = new SortedSet<PrioritizableDelegate<Action<T1, T2>>>(orderedDelegates);
        }

        #endregion

        #region Data Members

        /// <summary> The ordered set of delegates to invoke when the event is raised. </summary>
        protected SortedSet<PrioritizableDelegate<Action<T1, T2>>> OrderedDelegates { get; }

        #endregion

        #region Methods

        /// <summary> Returns the invocation list of the delegate. </summary>
        /// <returns> A collection of prioritizable delegates attached to this event. </returns>
        public IEnumerable<PrioritizableDelegate<Action<T1, T2>>> GetInvocationList() =>
            this.OrderedDelegates;

        /// <summary> Invokes attached delegates in order of priority until the end or until one prevents further invocations. </summary>
        /// <param name="arg1"> The first argument that is passed into the delegates. </param>
        /// <param name="arg2"> The second argument that is passed into the delegates. </param>
        public void Invoke(T1 arg1, T2 arg2)
        {
            this.OrderedDelegates
                .Defer(x => x.Delegate?.Invoke(arg1, arg2))
                .TakeWhile(x => !x.Prevent())
                .Iterate();
        }

        /// <summary> Subscribes a delegate to the event. </summary>
        /// <param name="instance"> The event to which a delegate will be subscribed. </param>
        /// <param name="PrioritizableDelegateBase"> The wrapper containing the delegate, its priority function and its predicate. </param>
        public static PrioritizableEvent<T1, T2> operator +(PrioritizableEvent<T1, T2> instance, PrioritizableDelegate<Action<T1, T2>> prioritizableEvent)
        {
            if (instance == null)
                instance = new PrioritizableEvent<T1, T2>();
            instance.OrderedDelegates.Add(prioritizableEvent);
            return instance;
        }

        /// <summary> Unsubscribes a delegate from the event. </summary>
        /// <param name="instance"> The event from which a delegate will be unsubscribed. </param>
        /// <param name="delegate"> The unsubscribing delegate. </param>
        public static PrioritizableEvent<T1, T2> operator -(PrioritizableEvent<T1, T2> instance, Action<T1, T2> @delegate)
        {
            if (instance == null)
                return new PrioritizableEvent<T1, T2>();
            instance.OrderedDelegates.RemoveWhere(x => (x.Delegate == @delegate));
            return instance;
        }

        #endregion
    }
}