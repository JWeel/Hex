using Extended.Extensions;
using Extended.Patterns;
using System;
using System.Collections.Generic;

namespace Mogi.Inversion
{
    /// <summary> Provides a mechanism for restricting the sharing of new dependencies to within a scope. </summary>
    public class DependencyScope : Scope
    {
        #region Constructors

        /// <summary> Creates a scope which removes all dependencies that are registered within the scope from the dependency map when the scope ends. </summary>
        /// <remarks> Types registered inside the scope are not removed if they had already been registered to the shared dependency map outside the scope. </remarks>
        /// <param name="dependency"> The handler of shared dependencies. </param>
        public DependencyScope(DependencyHandler dependency)
            : base(new List<Type>().Into(typeList => new ScopedOperation(
                DependencyScope.CreatePreOperation(dependency, typeList),
                DependencyScope.CreatePostOperation(dependency, typeList))))
        {
        }

        #endregion

        #region Methods

        protected static Action CreatePreOperation(DependencyHandler dependency, List<Type> typeList) =>
            () => dependency.OnRegister += type => typeList.Add(type);

        protected static Action CreatePostOperation(DependencyHandler dependency, List<Type> typeList) =>
            () => typeList.Each(type => dependency.Unregister(type));

        #endregion
    }
}