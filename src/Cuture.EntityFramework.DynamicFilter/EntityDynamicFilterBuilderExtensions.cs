using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// EntityDynamicFilterBuilder 拓展
/// </summary>
public static class EntityDynamicFilterBuilderExtensions
{
    #region ExpressionFilter

    #region Head

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的头部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddHeadFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(null!, predicate, order, DynamicQueryFilterPlace.Head);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的头部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddHeadFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, string name, Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<TEntity>(name, order, DynamicQueryFilterPlace.Head, predicate);
        return builder.AddFilter(_ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的头部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddHeadFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(null!, predicateFactory, order, DynamicQueryFilterPlace.Head);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的头部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddHeadFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, string name, Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(serviceProvider => new ExpressionDynamicQueryFilter<TEntity>(name, order, DynamicQueryFilterPlace.Head, predicateFactory(serviceProvider)));
    }

    #endregion Head

    #region Tail

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的尾部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddTailFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(null!, predicate, order, DynamicQueryFilterPlace.Tail);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加基于表达式 <paramref name="predicate"/> 的尾部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddTailFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, string name, Expression<Func<TEntity, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<TEntity>(name, order, DynamicQueryFilterPlace.Tail, predicate);
        return builder.AddFilter(_ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的尾部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddTailFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(null!, predicateFactory, order, DynamicQueryFilterPlace.Tail);
    }

    /// <summary>
    /// 为实体 <typeparamref name="TEntity"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的尾部过滤器
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityDynamicFilterBuilder<TEntity> AddTailFilter<TEntity>(this EntityDynamicFilterBuilder<TEntity> builder, string name, Func<IServiceProvider, Expression<Func<TEntity, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return builder.AddFilter(serviceProvider => new ExpressionDynamicQueryFilter<TEntity>(name, order, DynamicQueryFilterPlace.Tail, predicateFactory(serviceProvider)));
    }

    #endregion Tail

    #endregion ExpressionFilter
}
