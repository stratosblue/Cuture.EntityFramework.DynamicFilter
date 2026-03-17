namespace Cuture.EntityFramework.DynamicFilter;

/// <summary>
/// 实体的动态过滤器元数据
/// </summary>
public class EntityDynamicQueryFilterMetadata(Type type)
{
    #region Public 属性

    /// <summary>
    /// 动态过滤器工厂列表
    /// </summary>
    public List<Func<IServiceProvider, IDynamicQueryFilter>> DynamicQueryFilterFactories { get; } = [];

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type Type { get; set; } = type ?? throw new ArgumentNullException(nameof(type));

    #endregion Public 属性
}
