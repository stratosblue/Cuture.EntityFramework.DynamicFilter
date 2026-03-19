using System.Linq.Expressions;

namespace Cuture.EntityFramework.DynamicFilter.Utils;

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

    public static bool TryAndAlso(ref Expression mainExpression, IEnumerable<IDynamicQueryFilter> queryFilters, ref ExpressionResolveContext context)
    {
        var newExpression = CombineVisitor.AndAlso(mainExpression, queryFilters, ref context);
        if (ReferenceEquals(mainExpression, newExpression))
        {
            return false;
        }

        mainExpression = newExpression;
        return true;
    }

    #endregion Public 方法

    #region Private 类

    private sealed class QueryFilterLambdaExpressionCombineVisitor : ExpressionVisitor
    {
        #region Private 字段

        private int _parameterCount;

        private ParameterValues? _parameterValues;

        private IEnumerable<IDynamicQueryFilter> _queryFilters = null!;

        private Type _targetType = null!;

        #endregion Private 字段

        #region Public 方法

        public Expression AndAlso(Expression mainExpression, IEnumerable<IDynamicQueryFilter> queryFilters, ref ExpressionResolveContext context)
        {
            _queryFilters = queryFilters;
            _parameterCount = context.ParameterCount;
            _parameterValues = context.ParameterValues;
            _targetType = context.CurrentFilterTargetType ?? throw new InvalidOperationException("CurrentFilterTargetType must be set in context.");

            try
            {
                return Visit(mainExpression);
            }
            finally
            {
                _queryFilters = null!;
                context.ParameterCount = _parameterCount;
                _parameterValues = null;
                _targetType = null!;
            }
        }

        #endregion Public 方法

        #region Protected 方法

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var finalParameter = node.Parameters[0];

            if (finalParameter.Type != _targetType)
            {
                return node;
            }

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
