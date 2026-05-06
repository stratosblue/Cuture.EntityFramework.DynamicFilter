using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cuture.EntityFramework.DynamicFilter;

/// <summary>
/// EntityFrameworkDynamicFilter 选项
/// </summary>
public sealed class EntityFrameworkDynamicFilterOptions
{
    #region Public 属性

    /// <summary>
    /// 类型对应动态过滤器元数据集合
    /// </summary>
    public ConcurrentDictionary<QueryFilterMetadataKey, EntityDynamicQueryFilterMetadata> QueryFilterMetadataCollection { get; } = new();

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
        var key = new QueryFilterMetadataKey(typeof(TEntity), typeof(Expression<Func<TEntity, bool>>), typeof(Func<TEntity, bool>));
        var builder = new EntityDynamicFilterBuilder<TEntity>(QueryFilterMetadataCollection.GetOrAdd(key, type => new(type)));
        buildAction(builder);
        return builder;
    }

    #endregion Public 方法
}
