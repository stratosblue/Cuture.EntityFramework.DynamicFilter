using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Extensions.Utils;

internal static class QueryExpressionParameterExtractor
{
    #region Private 字段

    /// <summary>
    /// 通用的参数名
    /// </summary>
    private static readonly string[] s_genericParameterName = [
        "__dynamic_filter_param__0",
        "__dynamic_filter_param__1",
        "__dynamic_filter_param__2",
        "__dynamic_filter_param__3",
        "__dynamic_filter_param__4",
        "__dynamic_filter_param__5",
        "__dynamic_filter_param__6",
        "__dynamic_filter_param__7",
        "__dynamic_filter_param__8",
        "__dynamic_filter_param__9",
    ];

    [ThreadStatic]
    private static SimpleQueryExpressionParameterExtractingVisitor? s_extractingVisitor;

    #endregion Private 字段

    #region Private 属性

    private static SimpleQueryExpressionParameterExtractingVisitor ExtractingVisitor => s_extractingVisitor ??= new();

    #endregion Private 属性

    #region Public 方法

    public static Expression Extracting(Expression expression, ref ExpressionResolveContext context)
    {
        return Extracting(expression, context.ParameterValues, ref context.ParameterCount);
    }

    public static Expression Extracting(Expression expression, IParameterValues parameterValues, ref int parameterCount)
    {
        return ExtractingVisitor.Extracting(expression, parameterValues, ref parameterCount);
    }

    #endregion Public 方法

    #region Private 类

    private sealed class SimpleQueryExpressionParameterExtractingVisitor : ExpressionVisitor
    {
        #region Private 字段

        private int _parameterCount;

        private IParameterValues? _parameterValues;

        #endregion Private 字段

        #region Public 方法

        public Expression Extracting(Expression expression, IParameterValues parameterValues, ref int parameterCount)
        {
            _parameterCount = parameterCount;
            _parameterValues = parameterValues;

            try
            {
                return Visit(expression);
            }
            finally
            {
                parameterCount = _parameterCount;
                _parameterValues = null;
            }
        }

        #endregion Public 方法

        #region Protected 方法

        protected override Expression VisitMember(MemberExpression node)
        {
            if (ShouldGetExpressionResultValue(node))
            {
                var value = Expression.Lambda<Func<object>>(Expression.Convert(node, typeof(object)))
                                      .Compile()
                                      .Invoke();

                var parameterName = GetParameterName(_parameterCount++);

                _parameterValues!.AddParameter(parameterName, value);

                return Expression.Parameter(node.Type, parameterName);
            }
            return base.VisitMember(node);
        }

        #endregion Protected 方法

        #region Private 方法

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetParameterName(in int parameterCount)
        {
            return parameterCount < 10
                   ? s_genericParameterName[parameterCount]
                   : $"__dynamic_filter_param__{parameterCount}";
        }

        private static bool ShouldGetExpressionResultValue(MemberExpression expression)
        {
            return expression.Expression switch
            {
                MemberExpression memberExpression => ShouldGetExpressionResultValue(memberExpression),
                ConstantExpression => true,
                _ => false,
            };
        }

        #endregion Private 方法
    }

    #endregion Private 类
}
