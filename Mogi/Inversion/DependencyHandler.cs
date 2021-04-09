using Extended.Extensions;
using System;
using System.Linq;

namespace Mogi.Inversion
{
    /// <summary> Provides access to <see cref="DependencyHandler{}"/> instances. </summary>
    public static class Dependency
    {
        #region Factory Methods

        /// <summary> Sets up a new instance of <see cref="DependencyHandler{}"/> with a root instance which helps construct dependencies and register them to events exposed on the root. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        public static DependencyHandler<T> Start<T>(T source)
            where T : class, IRoot
        {
            return new DependencyHandler<T>(source);
        }

        #endregion
    }

    /* 
    The reason this class is split between abstract and generic child is so it can be passed around as non-generic.
    This makes the IRegister interface easier to work with, as implementers do not need to know the root type.
    This could also have been achieved with an interface, but using classes avoids duplicate XML documentation.

    In the Register overload which takes an instance, the passed instance will be ignored if its type is already registered.
    This is because there is no way to guarantee that other dependencies do not rely specifically on the registered instance.
    */

    /// <summary> Provides simplified construction of types with automatic subscription to events defined in specific interfaces. </summary>
    public class DependencyHandler<TRoot> : DependencyHandler
        where TRoot : class, IRoot
    {
        #region Constructors

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        internal DependencyHandler(TRoot source)
            : this(source, new DependencyMap())
        {
        }

        /// <summary> Initializes a new instance using a specified source which exposes events to which eligible dependencies will subscribe, and a map which contains shared dependencies. </summary>
        /// <param name="source"> The instance which exposes events to which eligible dependencies will subscribe. </param>
        /// <param name="map"> The shared dependency map which was populated by a parent root class. </param>
        internal DependencyHandler(TRoot source, DependencyMap map)
            : base(map)
        {
            this.Source = source;
        }

        #endregion

        #region Properties

        protected TRoot Source { get; }

        #endregion

        #region Protected Methods

        protected override TDependency Attach<TDependency>(TDependency instance)
            where TDependency : class
        {
            return this.Source.Attach(instance);
        }

        #endregion
    }

    /// <summary> Exposes methods to register dependencies. </summary>
    public abstract class DependencyHandler
    {
        #region Constructors

        /// <summary> Initializes a new instance with the map which will hold shared dependencies. </summary>
        /// <param name="map"> The dependency map which will hold shared dependencies. </param>
        protected DependencyHandler(DependencyMap map)
        {
            this.DependencyMap = map;
        }

        #endregion

        #region Properties

        protected DependencyMap DependencyMap { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and registers a dependency in the map if the type is not already registered.
        /// <para/>
        /// Creation requires one public constructor. If the constructor has parameters, arguments will be provided so long as their types exist either in the provided array <paramref name="args"/> or in the dependency map. 
        /// If they do not, or if there is not exactly one public constructor, an exception will be thrown.
        /// <para/>
        /// If the dependency type inherits from specific interfaces (see remarks), the instance will, after instantiation, automatically get subscribed to events on the root instance.
        /// It may also have instance methods invoked which propagate the map.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The newly initialized instance, or an existing instance if the type was already in the map. </returns>
        /// <remarks>
        /// Eligible interfaces are: <see cref="IRegister"/>, <see cref="IUpdate{}"/>, <see cref="IDraw{}"/>, <see cref="IResize{}"/>, <see cref="ITerminate"/> 
        /// </remarks>
        /// <exception cref="ArgumentException"> Parameter <paramref name="args"/> contains elements of the same type. </exception>
        public TDependency Register<TDependency>(params object[] args)
            where TDependency : class
        {
            if (this.DependencyMap.TryGetValue(typeof(TDependency), out var existingValue))
                return (TDependency) existingValue;

            var instance = this.CreateInstance<TDependency>(args);
            return this.Register(instance);
        }

        /// <summary>
        /// Registers a dependency in the map if the type is not already registered.
        /// <para/>
        /// If the dependency type inherits from specific interfaces (see remarks), and does not yet exist in the map, the instance will automatically get subscribed to events on the root instance.
        /// It may also have instance methods invoked which propagate the map.
        /// <para/>
        /// If the type already exists in the map, the passed argument is returned and no changes are made.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The same instance passed into this method. </returns>
        /// <remarks>
        /// Eligible interfaces are: <see cref="IRegister"/>, <see cref="IUpdate{}"/>, <see cref="IDraw{}"/>, <see cref="IResize{}"/>, <see cref="ITerminate"/> 
        /// </remarks>
        public TDependency Register<TDependency>(TDependency instance)
            where TDependency : class
        {
            if (this.DependencyMap.ContainsKey(typeof(TDependency)))
                return instance;

            this.DependencyMap.Add(typeof(TDependency), instance);

            if (instance is IRegister registry)
            {
                registry.Register(this);
            }

            return this.Attach(instance);
        }

        #endregion

        #region Helper Methods

        protected TDependency CreateInstance<TDependency>(params object[] args)
            where TDependency : class
        {
            var constructors = typeof(TDependency).GetConstructors();
            if (!constructors.One())
                throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because it does not have exactly one public constructor.");

            var argMap = args.ToDictionary(arg => arg.GetType());

            var constructor = constructors.First();
            var parameters = constructor.GetParameters();
            var arguments = parameters
                .Select(parameter => argMap.TryGetValue(parameter.ParameterType, out var instance) ||
                    this.DependencyMap.TryGetValue(parameter.ParameterType, out instance) ? instance :
                    throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because dependency type '{parameter.ParameterType}' is neither registered nor provided."))
                .ToArray();
            var instance = (TDependency) constructor.Invoke(arguments);
            return instance;
        }

        protected abstract TDependency Attach<TDependency>(TDependency dependency) where TDependency : class;

        #endregion
    }
}