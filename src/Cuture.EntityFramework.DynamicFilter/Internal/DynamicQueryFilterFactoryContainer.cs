using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

/// <summary>
/// <see cref="IDynamicQueryFilter"/> 工厂容器，用于创建 <see cref="IDynamicQueryFilter"/>
/// </summary>
internal sealed class DynamicQueryFilterFactoryContainer
{
    #region Private 属性

    private ImmutableDictionary<Type, Func<IServiceProvider, IDynamicQueryFilter>[]> QueryFilterFactories { get; }

    #endregion Private 属性

    #region Public 构造函数

    /// <inheritdoc cref="DynamicQueryFilterFactoryContainer"/>
    public DynamicQueryFilterFactoryContainer(IOptions<EntityFrameworkDynamicFilterOptions> optionsAccessor)
    {
        var collection = optionsAccessor.Value.QueryFilterCollection;

        QueryFilterFactories = collection.ToImmutableDictionary(m => m.Key, m => m.Value.ToArray());
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
        if (QueryFilterFactories.TryGetValue(type, out var factories)
            && factories.Length > 0)
        {
            var filters = new IDynamicQueryFilter[factories.Length];
            for (int i = 0; i < factories.Length; i++)
            {
                filters[i] = factories[i](serviceProvider);
            }
            return filters;
        }
        return null;
    }

    #endregion Public 方法
}
