using System;
using System.Linq.Expressions;

namespace Extended.Expressions
{
    /// <summary> Wraps an expression of a func and its pre-compiled func to avoid compiling multiple times. </summary>
    public class ExpressionWrapper<T1, T2>
    {
        #region Constructors

        public ExpressionWrapper(Expression<Func<T1, T2>> expression)
        {
            this.Expression = expression;
            this.Func = expression.Compile();
        }

        #endregion

        #region Properties

        public Expression<Func<T1, T2>> Expression { get; }

        public Func<T1, T2> Func { get; }

        #endregion
    }
}