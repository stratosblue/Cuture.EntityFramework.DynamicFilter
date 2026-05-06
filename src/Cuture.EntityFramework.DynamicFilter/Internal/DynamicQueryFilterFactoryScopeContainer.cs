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

    /// <inheritdoc cref="DynamicQueryFilterFactoryContainer.GetFilters(Type, IServiceProvider)"/>
    public IDynamicQueryFilter[]? GetFilters(Type type) => _queryFilterFactoryContainer.GetFilters(type, _serviceProvider);

    /// <inheritdoc cref="DynamicQueryFilterFactoryContainer.GetPredicateExpressionType(Type)"/>
    public Type? GetPredicateExpressionType(Type type) => _queryFilterFactoryContainer.GetPredicateExpressionType(type);

    /// <inheritdoc cref="DynamicQueryFilterFactoryContainer.GetPredicateFuncType(Type)"/>
    public Type? GetPredicateFuncType(Type type) => _queryFilterFactoryContainer.GetPredicateFuncType(type);

    #endregion Public 方法
}
