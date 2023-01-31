using System;
using System.Runtime.Serialization;

namespace Extended.Exceptions
{
    /// <summary> Represents errors occurring from using invalid enum values. </summary>
    /// <typeparam name="T"> The type of the invalid enum. </typeparam>
    [Serializable]
    public class InvalidEnumException<T> : Exception
        where T : Enum
    {
        #region Constructors

        /// <summary> Initializes a new instance with the invalid enum value. </summary>
        /// <param name="value"> The invalid enum value. </param>
        public InvalidEnumException(T value)
            : base($"Invalid value for enum '{typeof(T).Name}': '{value}'")
        {
        }

        /// <summary> Initializes a new instance with serialized data. </summary>
        /// <param name="info"> The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context"> The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. </param>
        protected InvalidEnumException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}