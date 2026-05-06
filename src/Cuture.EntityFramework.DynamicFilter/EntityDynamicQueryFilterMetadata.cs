namespace Cuture.EntityFramework.DynamicFilter;

/// <summary>
/// 实体的动态过滤器元数据
/// </summary>
/// <param name="Key">唯一键</param>
public record class EntityDynamicQueryFilterMetadata(QueryFilterMetadataKey Key)
{
    #region Public 属性

    /// <summary>
    /// 动态过滤器工厂列表
    /// </summary>
    public List<Func<IServiceProvider, IDynamicQueryFilter>> DynamicQueryFilterFactories { get; } = [];

    #endregion Public 属性
}

/// <summary>
/// 查询过滤元数据键
/// </summary>
/// <param name="Type">目标类型</param>
/// <param name="PredicateExpressionType">筛选表达式类型</param>
/// <param name="PredicateFuncType">筛选方法类型</param>
public record struct QueryFilterMetadataKey(Type Type, Type PredicateExpressionType, Type PredicateFuncType);
