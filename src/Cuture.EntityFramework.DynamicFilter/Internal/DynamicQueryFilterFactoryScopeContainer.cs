namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

/// <summary>
/// <see cref="DynamicQueryFilterFactoryContainer"/> 的 Scope 容器
/// </summary>
internal sealed class DynamicQueryFilterFactoryScopeContainer
{
    #region Private 字段

    private readonly DynamicQueryFilterFactoryContainer _queryFilterFactoryContainer;

    private readonly IServiceProvider _serviceProvider;

    #endregion Private 字段

    #region Public 构造函数

    public DynamicQueryFilterFactoryScopeContainer(DynamicQueryFilterFactoryContainer queryFilterFactoryContainer, IServiceProvider serviceProvider)
    {
        _queryFilterFactoryContainer = queryFilterFactoryContainer ?? throw new ArgumentNullException(nameof(queryFilterFactoryContainer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    #endregion Public 构造函数

    #region Public 方法

    /// <summary>
    /// 根据模型类型 <paramref name="type"/> 获取其对应的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IDynamicQueryFilter[]? GetFilters(Type type) => _queryFilterFactoryContainer.GetFilters(type, _serviceProvider);

    #endregion Public 方法
}
