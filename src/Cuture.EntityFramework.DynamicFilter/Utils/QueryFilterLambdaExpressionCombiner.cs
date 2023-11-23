using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Extensions.Utils;

internal static class QueryFilterLambdaExpressionCombiner
{
    #region Private 字段

    [ThreadStatic]
    private static QueryFilterLambdaExpressionCombineVisitor? s_combineVisitor;

    #endregion Private 字段

    #region Private 属性

    private static QueryFilterLambdaExpressionCombineVisitor CombineVisitor => s_combineVisitor ??= new();

    #endregion Private 属性

    #region Public 方法

    public static Expression AndAlso(Expression mainExpression, IEnumerable<IDynamicQueryFilter> queryFilters, ref ExpressionResolveContext context)
    {
        return CombineVisitor.AndAlso(mainExpression, queryFilters, ref context);
    }

    #endregion Public 方法

    #region Private 类

    private sealed class QueryFilterLambdaExpressionCombineVisitor : ExpressionVisitor
    {
        #region Private 字段

        private int _parameterCount;

        private IParameterValues? _parameterValues;

        private IEnumerable<IDynamicQueryFilter> _queryFilters = null!;

        #endregion Private 字段

        #region Public 方法

        public Expression AndAlso(Expression mainExpression, IEnumerable<IDynamicQueryFilter> queryFilters, ref ExpressionResolveContext context)
        {
            _queryFilters = queryFilters;
            _parameterCount = context.ParameterCount;
            _parameterValues = context.ParameterValues;

            try
            {
                return Visit(mainExpression);
            }
            finally
            {
                _queryFilters = null!;
                context.ParameterCount = _parameterCount;
                _parameterValues = null;
            }
        }

        #endregion Public 方法

        #region Protected 方法

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var finalParameter = node.Parameters[0];
            var finalExpressionBody = node.Body;
            foreach (var queryFilter in _queryFilters)
            {
                var filterExpression = queryFilter.UnderlyingExpression;
                var parameterReplacedExpression = ExpressionParameterReplacer.Replace(filterExpression.Body, filterExpression.Parameters[0], finalParameter);
                var parameterizeBody = QueryExpressionParameterExtractor.Extracting(parameterReplacedExpression, _parameterValues!, ref _parameterCount);

                finalExpressionBody = queryFilter.Place switch
                {
                    DynamicQueryFilterPlace.Tail => Expression.AndAlso(finalExpressionBody, parameterizeBody),
                    _ => Expression.AndAlso(parameterizeBody, finalExpressionBody),
                };
            }
            return Expression.Lambda(finalExpressionBody, finalParameter);
        }

        #endregion Protected 方法
    }

    #endregion Private 类
}
