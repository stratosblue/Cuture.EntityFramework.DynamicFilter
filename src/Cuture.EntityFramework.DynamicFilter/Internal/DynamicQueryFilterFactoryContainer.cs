using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace Cuture.EntityFramework.DynamicFilter.Internal;

/// <summary>
/// <see cref="IDynamicQueryFilter"/> 工厂容器，用于创建 <see cref="IDynamicQueryFilter"/>
/// </summary>
internal sealed class DynamicQueryFilterFactoryContainer
{
    #region Private 属性

    private ImmutableDictionary<Type, QueryFilterMetadataCache> QueryFilterMetadataCaches { get; }

    #endregion Private 属性

    #region Public 构造函数

    /// <inheritdoc cref="DynamicQueryFilterFactoryContainer"/>
    public DynamicQueryFilterFactoryContainer(IOptions<EntityFrameworkDynamicFilterOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var collection = options.QueryFilterMetadataCollection;

        QueryFilterMetadataCaches = collection.ToImmutableDictionary(m => m.Key.Type, m => new QueryFilterMetadataCache(m.Value.Key.PredicateExpressionType, m.Value.Key.PredicateFuncType, [.. m.Value.DynamicQueryFilterFactories]));
    }

    #endregion Public 构造函数

    #region Public 方法

    /// <summary>
    /// 根据模型类型 <paramref name="type"/> 获取其对应的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public IDynamicQueryFilter[]? GetFilters(Type type, IServiceProvider serviceProvider)
    {
        if (QueryFilterMetadataCaches.TryGetValue(type, out var metadataCache)
            && metadataCache.HasQueryFilterFactory)
        {
            var factories = metadataCache.QueryFilterFactories;
            var filters = new IDynamicQueryFilter[factories.Length];
            for (int i = 0; i < factories.Length; i++)
            {
                filters[i] = factories[i](serviceProvider);
            }
            return filters;
        }
        return null;
    }

    /// <summary>
    /// 获取类型<paramref name="type"/>的筛选表达式类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Type? GetPredicateExpressionType(Type type)
    {
        if (QueryFilterMetadataCaches.TryGetValue(type, out var metadataCache))
        {
            return metadataCache.PredicateExpressionType;
        }

        return null;
    }

    /// <summary>
    /// 获取类型<paramref name="type"/>的筛选方法类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Type? GetPredicateFuncType(Type type)
    {
        if (QueryFilterMetadataCaches.TryGetValue(type, out var metadataCache))
        {
            return metadataCache.PredicateFuncType;
        }

        return null;
    }

    #endregion Public 方法

    private record struct QueryFilterMetadataCache(Type PredicateExpressionType, Type PredicateFuncType, Func<IServiceProvider, IDynamicQueryFilter>[] QueryFilterFactories)
    {
        public bool HasQueryFilterFactory { get; } = QueryFilterFactories.Length > 0;
    }
}
