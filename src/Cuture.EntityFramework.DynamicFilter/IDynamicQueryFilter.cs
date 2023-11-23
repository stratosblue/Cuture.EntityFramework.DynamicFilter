using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// 动态 QueryFilter
/// </summary>
public interface IDynamicQueryFilter
{
    #region Public 字段

    /// <summary>
    /// 默认排序
    /// </summary>
    public const int DefaultOrder = 0;

    #endregion Public 字段

    #region Public 属性

    /// <summary>
    /// 目标实体类型
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnable { get; }

    /// <summary>
    /// 名称
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// 应用排序
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// filter位置
    /// </summary>
    public DynamicQueryFilterPlace Place { get; }

    /// <summary>
    /// 表达式
    /// </summary>
    public LambdaExpression UnderlyingExpression { get; }

    #endregion Public 属性
}

/// <summary>
/// 针对实体 <typeparamref name="T"/> 的动态 QueryFilter
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDynamicQueryFilter<T> : IDynamicQueryFilter
{
    #region Public 属性

    /// <inheritdoc cref="IDynamicQueryFilter.UnderlyingExpression"/>
    public Expression<Func<T, bool>> Expression { get; }

    #endregion Public 属性
}
