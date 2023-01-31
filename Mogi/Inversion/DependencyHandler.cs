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
    Idea: Alternatively when using the overload that takes an instance, if it already exists throw an exception.
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

        #region Data Members

        /// <summary> Raised when a type is added to the dependency map. </summary>
        /// <remarks> It should be noted that this is before any eligible interface methods are invoked on the instance of the newly registered type. </remarks>
        public event Action<Type> OnRegister;

        protected DependencyMap DependencyMap { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and registers a dependency in the map if the type is not already registered.
        /// <para/>
        /// Creation requires one public constructor. If the constructor has parameters, arguments will be provided so long as their types exist either in the provided array <paramref name="args"/> or in the dependency map. 
        /// If they do not, or if there is not exactly one public constructor, an exception will be thrown.
        /// <para/>
        /// The optional <paramref name="args"/> array may not contain multiple instances of the same type. If both the dependency map and the <paramref name="args"/> array contain instances of the same type, the instance from <paramref name="args"/> takes precedence.
        /// <para/>
        /// If the dependency type inherits from specific interfaces (see remarks), the instance will, after instantiation, automatically get subscribed to events on the root instance.
        /// It may also have instance methods invoked which propagate the map.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The newly initialized instance, or an existing instance if the type was already in the map. </returns>
        /// <remarks>
        /// The following interfaces are leveraged by this method:
        /// <br/> <see cref="IRegister"/>, <see cref="IUpdate{}"/>, <see cref="IDraw{}"/>, <see cref="IResize{}"/>, <see cref="IActivate"/>, <see cref="ITerminate"/>, <see cref="IRegisterAs{}"/>
        /// </remarks>
        /// <exception cref="ArgumentException"> Parameter <paramref name="args"/> contains elements of the same type. </exception>
        public TDependency Register<TDependency>(params object[] args)
            where TDependency : class
        {
            var type = this.GetTypeToRegisterAs(typeof(TDependency));
            if (this.DependencyMap.TryGetValue(type, out var existingValue))
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
        /// If the type already exists in the map, the corresponding registered instance is returned and no changes are made.
        /// </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        /// <returns> The instance passed into this method if its type was not already registered, or the already registered instance. </returns>
        /// <remarks>
        /// The following interfaces are leveraged by this method:
        /// <br/> <see cref="IRegister"/>, <see cref="IUpdate{}"/>, <see cref="IDraw{}"/>, <see cref="IResize{}"/>, <see cref="IActivate"/>, <see cref="ITerminate"/>, <see cref="IRegisterAs{}"/>
        /// </remarks>
        public TDependency Register<TDependency>(TDependency instance)
            where TDependency : class
        {
            var type = this.GetTypeToRegisterAs(typeof(TDependency));
            if (this.DependencyMap.TryGetValue(type, out var existingValue))
                return (TDependency) existingValue;

            this.DependencyMap.Add(type, instance);
            this.OnRegister?.Invoke(type);

            if (instance is IRegister registry)
            {
                registry.Register(this);
            }

            return this.Attach(instance);
        }

        /// <summary> Removes the specified type from the dependency map. If it did not exist in the map, nothing happens. </summary>
        /// <typeparam name="TDependency"> The type of the dependency </typeparam>
        public void Unregister<TDependency>()
        {
            this.Unregister(typeof(TDependency));
        }

        /// <summary> Removes the specified type from the dependency map. If it did not exist in the map, nothing happens. </summary>
        /// <param name="type"> The type of the dependency. </param>
        public void Unregister(Type type)
        {
            this.DependencyMap.Remove(type);
        }

        #endregion

        #region Helper Methods

        /// <summary> Returns the type with which this type should be registered in the dependency map.
        /// <br/> The <see cref="IRegisterAs{}"/> interface is used to specify which type to use. If this type does not inherit from it, the type itself will be used. 
        /// <br/> If this type inherits from <see cref="IRegisterAs{}"/> multiple times, this method will throw an exception. </summary>
        protected Type GetTypeToRegisterAs(Type type)
        {
            var interfaceType = type.GetInterfaces()
                .Where(interfaceType => interfaceType.IsGenericType)
                .Where(interfaceType => (interfaceType.GetGenericTypeDefinition() == typeof(IRegisterAs<>)))
                .SingleOrDefault();
            if (interfaceType == default)
                return type;
            return interfaceType.GetGenericArguments().Single();
        }

        protected TDependency CreateInstance<TDependency>(params object[] args)
            where TDependency : class
        {
            var constructors = typeof(TDependency).GetConstructors();
            if (!constructors.One())
                throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because it does not have exactly one public constructor.");

            var argMap = args.ToDictionary(arg => arg.GetType());
            var constructor = constructors.Single();
            var parameters = constructor.GetParameters();
            var arguments = parameters
                .Select(parameter => argMap.TryGetValue(parameter.ParameterType, out var instance) ||
                    this.DependencyMap.TryGetValue(parameter.ParameterType, out instance) ? instance :
                    throw new InvalidOperationException($"Cannot register type '{typeof(TDependency).Name}' because dependency type '{parameter.ParameterType}' is neither registered nor provided."))
                .ToArray();
            return (TDependency) constructor.Invoke(arguments);
        }

        protected abstract TDependency Attach<TDependency>(TDependency dependency) where TDependency : class;

        #endregion
    }
}