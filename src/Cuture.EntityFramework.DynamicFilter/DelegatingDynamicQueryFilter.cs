using System.Linq.Expressions;

namespace Cuture.EntityFramework.DynamicFilter;

/// <summary>
/// 基于表达式的 <inheritdoc cref="IDynamicQueryFilter{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ExpressionDynamicQueryFilter<T> : IDynamicQueryFilter<T>
{
    #region Public 属性

    /// <inheritdoc/>
    public Type EntityType => typeof(T);

    /// <inheritdoc/>
    public Expression<Func<T, bool>> Expression { get; }

    /// <inheritdoc/>
    public bool IsEnable => true;

    /// <inheritdoc/>
    public string? Name { get; }

    /// <inheritdoc/>
    public LambdaExpression UnderlyingExpression => Expression;

    #endregion Public 属性

    #region Public 构造函数

    /// <inheritdoc cref="ExpressionDynamicQueryFilter{T}"/>
    public ExpressionDynamicQueryFilter(string? name, Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression = expression;
        Name = name;
    }

    #endregion Public 构造函数
}
