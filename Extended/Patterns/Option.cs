using System;

namespace Extended.Patterns
{
    /// <summary> Represents the possibility of the existence of a value. </summary>
    /// <typeparam name="T"> The type of the value that may or may not exist. </typeparam>
    public readonly struct Option<T>
    {
        #region Constructors

        /// <summary> Instantiates an option which contains a value. </summary>
        /// <param name="value"> The value that is to be contained in the <see cref="Option{T}"/>. </param>
        public Option(T value)
        {
            _value = value;
            _hasValue = true;
        }

        #endregion

        #region Members

        /// <summary> Determines whether or not the <see cref="Option{T}"/> has a value. </summary>
        private readonly bool _hasValue;

        /// <summary> The value wrapped by the <see cref="Option{T}"/> if it has a value, otherwise <see langword="default"/>. </summary>
        private readonly T _value;

        #endregion

        #region Methods

        /// <summary> Passes the wrapped value if it has one into a function, or calls a parameterless function if it does not. </summary>
        /// <param name="some"> The function to invoke if a value exists. </param>
        /// <param name="none"> The function to invoke if no value exists. </param>
        /// <typeparam name="TNext"> The type of the return value of the functions. </typeparam>
        /// <returns> The result of <paramref name="some"/> if a value exists, or the result of <paramref name="none"/> if no value exists. </returns>
        public TNext Match<TNext>(Func<T, TNext> some, Func<TNext> none) =>
            _hasValue ? some(_value) : none();

        /// <summary> Passes the wrapped value if it has one into an action, or calls a parameterless action if it does not. </summary>
        /// <param name="some"> The action to invoke if a value exists. </param>
        /// <param name="none"> The action to invoke if no value exists. </param>
        public void Match(Action<T> some, Action none)
        {
            if (_hasValue)
                some(_value);
            else
                none();
        }

        /// <summary> Passes the wrapped value if it has one into an action, or does nothing if it does not. </summary>
        /// <param name="some"> The action to invoke if a value exists. </param>
        public void Match(Action<T> some)
        {
            if (_hasValue)
                some(_value);
        }

        /// <summary> Gets the wrapped value if it has one or calls a function to retrieve an alternate if it does not. </summary>
        /// <param name="coalesce"> A function to invoke if no value exists. </param>
        /// <returns> The wrapped value if it exists or the result of <paramref name="coalesce"/> if it does not. </returns>
        public T Coalesce(Func<T> coalesce) =>
            this.Match(some: value => value, none: coalesce);

        /// <summary> Passes the wrapped value if it has one into a function that returns another optional value, or returns an empy option if it does not. </summary>
        /// <param name="bind"> The function that returns another optional value. </param>
        /// <typeparam name="TNext"> The type of the value wrapped by the binding function. </typeparam>
        /// <returns> The result of <paramref name="bind"/> if a value exists, or an empty option if no value exists. </returns>
        public Option<TNext> Bind<TNext>(Func<T, Option<TNext>> bind) =>
            _hasValue ? bind(_value) : Option.None;

        /// <summary> Passes the wrapped value if it has one into a function and wraps the return value in another option, or returns an empy option if it does not. </summary>
        /// <param name="bind"> The function that returns another optional value. </param>
        /// <typeparam name="TNext"> The type of the value returned by the mapping function. </typeparam>
        /// <returns> The result of <paramref name="map"/> wrapped inside another option if a value exists, or an empty option if no value exists. </returns>
        public Option<TNext> Map<TNext>(Func<T, TNext> map) =>
            this.Bind(value => Option.Some(map(value)));

        /// <summary> Passes the wrapped value if it has one into a predicate and returns this option if it is satisfied, otherwise or if no value exists returns an empty option. </summary>
        /// <param name="filter"> The predicate that determines whether to keep the optional value. </param>
        /// <returns> This option if a wrapped value exists and the value satisfies <paramref name="filter"/>, or an empty option if not. </returns>
        public Option<T> Filter(Predicate<T> filter) =>
            this.Bind(value => filter(value) ? Option.Some(value) : Option.None);

        #endregion

        #region Operators

        /// <summary> Replaces the instance of <see cref="NoneOption"/> with a new empty <see cref="Option{T}"/>. </summary>
        public static implicit operator Option<T>(NoneOption _) => new Option<T>();

        #endregion
    }

    /// <summary> Represents an empty option. Implicitly converts to <see cref="Option{T}"/>. </summary>
    public readonly struct NoneOption
    {
    }

    /// <summary> Exposes simplified access to <see cref="Option{T}"/> instances. </summary>
    public static class Option
    {
        #region Methods
        
        /// <summary> Wraps a value inside an option. </summary>
        /// <param name="value"> The value that is wrapped. </param>
        /// <typeparam name="T"> The type of the value that is wrapped. </typeparam>
        /// <returns> An option that contains <paramref name="value"/>. </returns>
        public static Option<T> Some<T>(T value) => new Option<T>(value);

        /// <summary> Gets an object that represents an empty option. This can be implicitly converted to <see cref="Option{T}"/>. </summary>
        /// <returns> An empty option that can be implicitly converted to <see cref="Option{T}"/>. </returns>
        public static NoneOption None { get; } = new NoneOption();

        #endregion
    }
}