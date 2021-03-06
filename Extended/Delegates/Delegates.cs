namespace Extended.Delegates
{
    /// <summary> Encapsulates a <c>TryParse</c> method that tries to convert a <see cref="string"/> representation of a value of type <typeparamref name="T"/> to an instance of that type.
    /// <para/> A returned <see cref="bool"/> should indicate whether or not conversion succeeded. If it succeeded, the <see langword="out"/> parameter should contain the converted instance. </summary>
    /// <typeparam name="T"> The type that a given <see cref="string"/> should be parsed to. </typeparam>
    /// <param name="value"> The value that should be parsed. </param>
    /// <param name="result"> The parsed value if parsing was possible, or the <see langword="default"/> value if it was not. </param>
    public delegate bool TryParse<T>(string value, out T result);
}