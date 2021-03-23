using Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mogi.Inversion
{
    /// <summary> Represents an event where subscription and invocation are scoped to logical phases. </summary>
    /// <remarks> This class is similar to <see langword="event"/> but does not have language level support, therefore:
    /// <br/> - interfaces cannot expose it as field (must be property)
    /// <br/> - this event can be set to null by anyone, not just the owner </remarks>
    public class PhasedEvent<T>
    {
        #region Constructors

        /// <summary> Initializes a new instance, using the values of another instance if it exists. </summary>
        /// <remarks> This should only be called by the <see cref="+"/> and <see cref="-"/> operators. </remarks>
        private PhasedEvent(PhasedEvent<T> sourceOrDefault)
        {
            this.Map = new Dictionary<Type, IList<Action<T>>>();
            if (sourceOrDefault != default)
                sourceOrDefault.Map.Each(this.Map.Add);
        }

        #endregion

        #region Data Members

        /// <summary> The ordered set of delegates to invoke when the event is raised. </summary>
        protected IDictionary<Type, IList<Action<T>>> Map { get; }

        #endregion

        #region Methods

        /// <summary> Returns the invocation list of the delegate. </summary>
        public Action<T>[] GetInvocationList() =>
            this.Map.Values.Flatten().ToArray();

        /// <summary> Invokes delegates of a specified pahse. </summary>
        /// <param name="arg"> The argument that is passed into the delegates. </param>
        public void Invoke<TPhase>(T arg) where TPhase : IPhase
        {
            if (!this.Map.TryGetValue(typeof(TPhase), out var actionList))
                return;

            actionList.Each(x => x?.Invoke(arg));
        }

        /// <summary> Subscribes a delegate to the event of the specified phase. </summary>
        /// <param name="instance"> The event map to which a delegate will be subscribed. </param>
        /// <param name="tuple"> A wrapper containing the logical phase where the delagate applies and the delegate. </param>
        public static PhasedEvent<T> operator +(PhasedEvent<T> instance, (Type Type, Action<T> Action) tuple)
        {
            var (type, action) = tuple;
            if (!type.IsAssignableTo(typeof(IPhase)))
                throw new InvalidOperationException($"Type must inherit from {nameof(IPhase)}");
            
            var result = new PhasedEvent<T>(instance);
            if (!result.Map.TryGetValue(type, out var actionList))
            {
                actionList = new List<Action<T>>();
                result.Map.Add((type, actionList));
            }
            actionList.Add(action);
            return result;
        }

        /// <summary> Unsubscribes a delegate from the event. </summary>
        /// <param name="instance"> The event map from which a delegate will be unsubscribed. </param>
        /// <param name="tuple"> A wrapper containing the logical phase where the delagate applies and the delegate. </param>
        public static PhasedEvent<T> operator -(PhasedEvent<T> instance, (Type Type, Action<T> Action) tuple)
        {
            var (type, action) = tuple;
            if (!type.IsAssignableTo(typeof(IPhase)))
                throw new InvalidOperationException($"Type must inherit from {nameof(IPhase)}");

            var result = new PhasedEvent<T>(instance);
            if (!result.Map.TryGetValue(type, out var actionList))
                return result;
            actionList.Remove(action);
            if (!actionList.Any())
                result.Map.Remove((type, actionList));
            return result;
        }

        #endregion
    }
}