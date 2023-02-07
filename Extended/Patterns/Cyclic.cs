using Extended.Delegates;
using Extended.Extensions;
using Extended.Generators;
using System;
using System.Linq;

namespace Extended.Patterns
{
    /// <summary> Provides a mechanism to expose a single element in a collection with methods to cycle through the other elements. </summary>
    /// <typeparam name="T"> The type of the elements in the cycle. </typeparam>
    public class Cyclic<T>
    {
        #region Constructors

        /// <summary> Creates a new instance with an arbitrary number of values. </summary>
        public Cyclic(params T[] values)
        {
            if (values.None())
                throw new ArgumentException($"Parameter {nameof(values)} must have at least one element.");

            this.Index = 0;
            this.Values = values;
        }

        #endregion

        #region Data Members

        protected int _index;

        protected T[] Values { get; set; }

        /// <summary> The index that tracks the current value in the cycle. </summary>
        public int Index
        {
            get => _index;
            protected set
            {
                if (_index == value)
                    return;
                var oldValue = this.Value;
                _index = value;
                this.OnChange?.Invoke(oldValue, this.Value);
            }
        }

        /// <summary> The total number of elements in the cycle. </summary>
        public int Length => this.Values.Length;

        /// <summary> The current value of the cycle. </summary>
        public T Value =>
            this.Values[this.Index];

        /// <summary> Raised when the current value of the cycle is changed. </summary>
        public event ChangeHandler<T> OnChange;

        #endregion

        #region Methods

        /// <summary> Sets the current value in the cycle to its next one. </summary>
        public void Advance()
        {
            var index = this.Index + 1;
            if (index >= this.Values.Length)
                index = 0;
            this.Index = index;
        }

        /// <summary> Sets the current value in the cycle to its previous one. </summary>
        public void Reverse()
        {
            var index = this.Index - 1;
            if (index < 0)
                index = this.Values.Length - 1;
            this.Index = index;
        }

        /// <summary> Sets the current value of the cycle to the specified value.
        /// <br/> This method will throw an exception if the specified value does not exist in the cycle. </summary>
        /// <param name="value"> The value to set as the current value in the cycle. </param>
        public void Set(T value)
        {
            var index = this.Values.IndexOf(value);
            if (index == -1)
                throw new ArgumentException("Cannot set value that does not exist in the cycle.");
            this.Index = index;
        }

        /// <summary> Sets the current value of the cycle to its first one. </summary>
        public void Restart() =>
            this.Index = 0;

        /// <summary> Returns the current value of the cycle. </summary>
        public static implicit operator T(Cyclic<T> cycle) =>
            cycle.Value;

        /// <summary> Returns a string representation of the current value of the cycle. </summary>
        public override string ToString() =>
            this.Value?.ToString();

        #endregion
    }

    /// <summary> Exposes simplified access to <see cref="Cyclic{T}"/> instances. </summary>
    public static class Cyclic
    {
        #region Methods

        /// <summary> Wraps the defined values of a specified enum type in a new instance of <see cref="Cyclic{}"/>. </summary>
        /// <typeparam name="T"> The type of the enum values. </typeparam>
        public static Cyclic<T> Enum<T>()
            where T : struct, Enum
        {
            return new Cyclic<T>(System.Enum.GetValues<T>());
        }

        /// <summary> Wraps a sequence of integers starting from 0 and incrementing until the specified value in a new instance of <see cref="Cylic{}"/>. </summary>
        /// <param name="range"> The upper bound of the sequence of integers. Must be higher than 0. </param>
        /// <exception cref="ArgumentException"> <paramref name="range"/> is not at higher than 0. </exception>
        public static Cyclic<int> Range(int range)
        {
            if (range < 1)
                throw new ArgumentException($"{nameof(range)} '{range}' must be higher than 0.");
            return new Cyclic<int>(Numeric.Range(range).ToArray());
        }

        public static Cyclic<T> FromValues<T>(params T[] values)
        {
            return new Cyclic<T>(values);
        }

        #endregion

    }
}