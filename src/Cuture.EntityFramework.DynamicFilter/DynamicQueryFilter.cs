using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// 抽象 <see cref="IDynamicQueryFilter"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DynamicQueryFilter<T> : IDynamicQueryFilter<T>
{
    #region Public 属性

    /// <inheritdoc/>
    public Type EntityType => typeof(T);

    /// <inheritdoc/>
    public abstract Expression<Func<T, bool>> Expression { get; }

    /// <inheritdoc/>
    public virtual bool IsEnable { get; } = true;

    /// <inheritdoc/>
    public virtual string? Name { get; init; }

    /// <inheritdoc/>
    public virtual int Order { get; } = IDynamicQueryFilter.DefaultOrder;

    /// <inheritdoc/>
    public virtual DynamicQueryFilterPlace Place { get; init; } = DynamicQueryFilterPlace.Default;

    /// <inheritdoc/>
    public LambdaExpression UnderlyingExpression => Expression;

    #endregion Public 属性

    #region Protected 构造函数

    /// <inheritdoc cref="DynamicQueryFilter{T}"/>
    protected DynamicQueryFilter(string? name = null)
    {
        Name = name;
    }

    #endregion Protected 构造函数
}
