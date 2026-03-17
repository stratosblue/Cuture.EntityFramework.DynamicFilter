namespace Cuture.EntityFramework.DynamicFilter.Internal;

/// <summary>
/// <see cref="DynamicQueryFilterFactoryContainer"/> 的 Scope 容器
/// </summary>
internal sealed class DynamicQueryFilterFactoryScopeContainer(DynamicQueryFilterFactoryContainer queryFilterFactoryContainer,
                                                              IServiceProvider serviceProvider)
{
    #region Private 字段

    private readonly DynamicQueryFilterFactoryContainer _queryFilterFactoryContainer = queryFilterFactoryContainer ?? throw new ArgumentNullException(nameof(queryFilterFactoryContainer));

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    #endregion Private 字段

    #region Public 方法

    /// <summary>
    /// 根据模型类型 <paramref name="type"/> 获取其对应的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IDynamicQueryFilter[]? GetFilters(Type type) => _queryFilterFactoryContainer.GetFilters(type, _serviceProvider);

    #endregion Public 方法
}
