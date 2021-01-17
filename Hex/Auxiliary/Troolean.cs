namespace Hex.Auxiliary
{
    /// <summary> Represents a boolean that defaults to <see langword="true"/>. </summary>
    public readonly struct Troolean
    {
        /// <summary> Initializes a new <see cref="Troolean"/> with a boolean value. </summary>
        public Troolean(bool value)
        {
            _value = value;
            _hasValue = true;
        }

        private readonly bool _value;
        private readonly bool _hasValue;

        /// <summary> Returns a new instance that is set to <see langword="true"/>. </summary>
        public static Troolean True => new Troolean();

        /// <summary> Returns a new instance that is set to <see langword="false"/>. </summary>
        public static Troolean False => new Troolean(false);

        /// <summary> Implicitly casts the <see cref="Troolean"/> to <see cref="bool"/>. If it has no value, it will default to <see langword="true"/>.</summary>
        public static implicit operator bool(Troolean value) => !value._hasValue || value._value;

        // should this be explicit?
        /// <summary> Implicitly casts the <see cref="bool"/> to <see cref="Troolean"/>. </summary>
        public static implicit operator Troolean(bool value) => new Troolean(value);
    }
}