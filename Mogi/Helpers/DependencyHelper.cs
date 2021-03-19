using Mogi.Inversion;
using System;
using System.Linq;

namespace Mogi.Helpers
{
    /// <summary> Provides factory methods to create <see cref="DependencyHelper{}"/> instances. </summary>
    public class DependencyHelper
    {
        #region Factory Methods

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        public static DependencyHelper<T> Create<T>(T source)
            where T : class, IRoot
        {
            return new DependencyHelper<T>(source);
        }

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe, and a map which contains shared dependencies. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        /// <param name="map"> The shared dependency map which was populated by a parent root class. </param>
        /// <remarks> This overload is constrainted to <see cref="ILoad"/> because it is indended to be called inside <see cref="ILoad.Load(DependencyMap)"/>. </remarks>
        public static DependencyHelper<T> Create<T>(T source, DependencyMap sharedDependencyMap)
            where T : class, IRoot, ILoad
        {
            return new DependencyHelper<T>(source, sharedDependencyMap);
        }

        #endregion
    }

    /// <summary> Provides simplified construction of types with automatic subscription to events defined in specific interfaces. </summary>
    public class DependencyHelper<TRoot>
        where TRoot : class, IRoot
    {
        #region Constructors

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        internal DependencyHelper(TRoot source)
            : this(source, new DependencyMap())
        {
        }

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe, and a map which contains shared dependencies. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        /// <param name="map"> The shared dependency map which was populated by a parent root class. </param>
        internal DependencyHelper(TRoot source, DependencyMap map)
        {
            this.Source = source;
            this.DependencyMap = map;
        }

        #endregion

        #region Properties

        protected TRoot Source { get; }
        protected DependencyMap DependencyMap { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates and registers a dependency in the map if the type is not already registered.
        /// <para/>
        /// Initialization expects one public constructor and an exception will be thrown if there are none or more than one.
        /// If the constructor has parameters, arguments will be provided so long as their types exist in the map. If they do not, a similar exception will be thrown.
        /// <para/>
        /// If the dependency type inherits from specific interfaces (see remarks), the instance will, after initialization, automatically get subscribed to events on the root instance.
        /// It may also have instance methods invoked which propagate the map.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The newly initialized instance, or an existing instance if the type was already in the map. </returns>
        /// <remarks>
        /// Eligible interfaces are: <see cref="ILoad"/>, <see cref="IUpdate"/>, <see cref="IDraw"/>, <see cref="IResize"/>, <see cref="ITerminate"/> 
        /// </remarks>
        public TDependency Register<TDependency>()
            where TDependency : class
        {
            if (this.DependencyMap.TryGetValue(typeof(TDependency), out var existingValue))
                return (TDependency) existingValue;

            var constructors = typeof(TDependency).GetConstructors();
            if (constructors.Length != 1)
            {
                throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because it does not have exactly one public constructor.");
            }
            var constructor = constructors.First();

            var parameters = constructor.GetParameters();
            var arguments = parameters
                .Select(parameter => this.DependencyMap.TryGetValue(parameter.ParameterType, out var instance) ?
                    instance :
                    throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because dependency type '{parameter.ParameterType}' is not registered."))
                .ToArray();
            var instance = (TDependency) constructor.Invoke(arguments);
            return this.Register(instance);
        }

        /// <summary>
        /// Registers a dependency in the map if the type is not already registered.
        /// <para/>
        /// If the dependency type inherits from specific interfaces (see remarks), and does not yet exist in the map, the instance will automatically get subscribed to events on the root instance.
        /// It may also have instance methods invoked which propagate the map.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The same instance passed into this method. </returns>
        /// <remarks>
        /// Eligible interfaces are: <see cref="ILoad"/>, <see cref="IUpdate"/>, <see cref="IDraw"/>, <see cref="IResize"/>, <see cref="ITerminate"/> 
        /// </remarks>
        public TDependency Register<TDependency>(TDependency instance)
            where TDependency : class
        {
            if (this.DependencyMap.ContainsKey(typeof(TDependency)))
                return instance;

            this.DependencyMap.Add(typeof(TDependency), instance);

            if (instance is ILoad loader)
                loader.Load(this.DependencyMap);

            return this.Source.Attach(instance);
        }

        #endregion
    }
}