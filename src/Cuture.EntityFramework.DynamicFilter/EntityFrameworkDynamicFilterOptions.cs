using System.Collections.Concurrent;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// EntityFrameworkDynamicFilter 选项
/// </summary>
public sealed class EntityFrameworkDynamicFilterOptions
{
    #region Public 属性

    /// <summary>
    /// 类型对应动态过滤器元数据集合
    /// </summary>
    public ConcurrentDictionary<Type, EntityDynamicQueryFilterMetadata> QueryFilterMetadataCollection { get; } = new();

    #endregion Public 属性

    #region Public 方法

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 配置过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="buildAction"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> Entity<TEntity>(Action<EntityDynamicFilterBuilder<TEntity>> buildAction)
    {
        var builder = new EntityDynamicFilterBuilder<TEntity>(QueryFilterMetadataCollection.GetOrAdd(typeof(TEntity), type => new(type)));
        buildAction(builder);
        return builder;
    }

    #endregion Public 方法
}
