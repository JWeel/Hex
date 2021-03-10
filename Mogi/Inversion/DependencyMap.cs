using System;
using System.Collections.Generic;

namespace Mogi.Inversion
{
    /// <summary> Keeps track of dependencies by type. </summary>
    public class DependencyMap : Dictionary<Type, object>
    {
        #region Constructors

        /// <summary> Initializes a new instance. </summary>
        public DependencyMap()
        {
        }

        #endregion

        #region Methods

        /// <summary> Adds a new dependency to the map using its type as key. </summary>
        public void Add<T>(T instance)
        {
            this.Add(typeof(T), instance);
        }

        #endregion
    }
}