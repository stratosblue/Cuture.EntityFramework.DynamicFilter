namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// 实体的动态过滤器元数据
/// </summary>
public class EntityDynamicQueryFilterMetadata
{
    #region Public 属性

    /// <summary>
    /// 动态过滤器工厂列表
    /// </summary>
    public List<Func<IServiceProvider, IDynamicQueryFilter>> DynamicQueryFilterFactories { get; } = [];

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type Type { get; set; }

    #endregion Public 属性

    #region Public 构造函数

    /// <inheritdoc cref="EntityDynamicQueryFilterMetadata"/>
    public EntityDynamicQueryFilterMetadata(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    #endregion Public 构造函数
}
