using System;
using System.Runtime.Serialization;

namespace Extended.Exceptions
{
    [Serializable]
    public class InvalidEnumException<T> : Exception
        where T : Enum
    {
        #region Constructors
            
        public InvalidEnumException(T value)
            : base($"Invalid value for enum '{typeof(T).Name}': '{value}'")
        {
        }

        protected InvalidEnumException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}