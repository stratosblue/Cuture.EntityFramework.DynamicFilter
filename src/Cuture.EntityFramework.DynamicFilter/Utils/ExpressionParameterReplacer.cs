using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Extensions.Utils;

internal static class ExpressionParameterReplacer
{
    #region Private 字段

    [ThreadStatic]
    private static ExpressionParameterReplaceVisitor? s_replaceVisitor;

    #endregion Private 字段

    #region Private 属性

    private static ExpressionParameterReplaceVisitor ReplaceVisitor => s_replaceVisitor ??= new();

    #endregion Private 属性

    #region Public 方法

    public static Expression Replace(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return ReplaceVisitor.Replace(expression, oldParameter, newParameter);
    }

    #endregion Public 方法

    #region Private 类

    private sealed class ExpressionParameterReplaceVisitor : ExpressionVisitor
    {
        #region Private 字段

        private ParameterExpression _newParameter = null!;

        private ParameterExpression _oldParameter = null!;

        #endregion Private 字段

        #region Public 方法

        public Expression Replace(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
            try
            {
                return Visit(expression);
            }
            finally
            {
                _oldParameter = null!;
                _newParameter = null!;
            }
        }

        #endregion Public 方法

        #region Protected 方法

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : node;
        }

        #endregion Protected 方法
    }

    #endregion Private 类
}
