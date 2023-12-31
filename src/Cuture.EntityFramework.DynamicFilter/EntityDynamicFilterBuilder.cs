using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// <typeparamref name="TEntity"/> 的动态过滤器构建器
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityDynamicFilterBuilder<TEntity>
{
    #region Private 字段

    private readonly EntityDynamicQueryFilterMetadata _metadata;

    #endregion Private 字段

    #region Public 构造函数

    /// <inheritdoc cref="EntityDynamicFilterBuilder{TEntity}"/>
    public EntityDynamicFilterBuilder(EntityDynamicQueryFilterMetadata metadata)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    #endregion Public 构造函数

    #region DelegationFilter

    internal EntityDynamicFilterBuilder<TEntity> AddFilter(Func<IServiceProvider, IDynamicQueryFilter> queryFilterFactory)
    {
        _metadata.DynamicQueryFilterFactories.Add(queryFilterFactory);
        return this;
    }

    #endregion DelegationFilter

    #region InstanceFilter

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加过滤器 <paramref name="queryFilter"/>
    /// </summary>
    /// <param name="queryFilter"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter(IDynamicQueryFilter<TEntity> queryFilter)
    {
        return AddFilter(_ => queryFilter);
    }

    #endregion InstanceFilter

    #region TypeFilter

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加过滤器 <typeparamref name="TQueryFilter"/>
    /// </summary>
    /// <typeparam name="TQueryFilter"></typeparam>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter<TQueryFilter>() where TQueryFilter : IDynamicQueryFilter<TEntity>
    {
        return AddFilter(static serviceProvider => serviceProvider.GetRequiredService<TQueryFilter>());
    }

    #endregion TypeFilter

    #region ExpressionFilter

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的过滤器
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter(Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter(null!, predicate, order, filterPlace);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的过滤器
    /// </summary>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter(string name, Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<TEntity>(name, order, filterPlace, predicate);
        return AddFilter(_ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的过滤器
    /// </summary>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter(Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter(null!, predicateFactory, order, filterPlace);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的过滤器
    /// </summary>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityDynamicFilterBuilder<TEntity> AddFilter(string name, Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter(serviceProvider => new ExpressionDynamicQueryFilter<TEntity>(name, order, filterPlace, predicateFactory(serviceProvider)));
    }

    #endregion ExpressionFilter
}
