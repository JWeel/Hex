using Hex.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Auxiliary
{
    /// <summary> Defines two methods that should be performed around a scope. </summary>
    public interface IScopedOperation
    {
        #region Methods

        void PreOperation();
        void PostOperation();

        #endregion
    }
    
    /// <summary> A wrapper of <see cref="Action"/> instances that should be performed around a scope. </summary>
    public class ScopedOperation : IScopedOperation
    {
        #region Constructors

        public ScopedOperation(Action preAction, Action postAction)
        {
            this.PreAction = preAction;
            this.PostAction = postAction;
        }

        #endregion

        #region Properties

        protected Action PreAction { get; }
        protected Action PostAction { get; }

        #endregion

        #region IScoped Implementation

        public void PreOperation() => this.PreAction?.Invoke();
        public void PostOperation() => this.PostAction?.Invoke();

        #endregion
    }
    
    /// <summary> Provides a mechanism for performing operations before and after a <see langword="using"/> scope. </summary>
    public class Scope : IDisposable
    {
        #region Constructors

        /// <summary> Initializes a new instance with an arbitrary number of operations to perform around a <see langword="using"/> scope. </summary>
        public Scope(params IScopedOperation[] scopes)
        {
            this.Operations = new List<IScopedOperation>();
            try
            {
                scopes.Each(scope => scope.With(this.Operations.Add).PreOperation());
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        /// <summary> Initializes a new instance with actions to perform before and after a <see langword="using"/> scope. </summary>
        public Scope(Action preOperation, Action postOperation)
        : this(new ScopedOperation(preOperation, postOperation)) { }

        #endregion

        #region Properties

        // cant be List<> because List has a stupid void method called Reverse....
        protected IList<IScopedOperation> Operations { get; }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this.Operations.Reverse().Each(scope => scope.PostOperation());
        }

        #endregion
    }
}