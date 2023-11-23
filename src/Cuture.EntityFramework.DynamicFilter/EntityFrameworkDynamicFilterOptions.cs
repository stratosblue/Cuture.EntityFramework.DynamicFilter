using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// EntityFrameworkDynamicFilter 选项
/// </summary>
public sealed class EntityFrameworkDynamicFilterOptions
{
    #region Public 属性

    /// <summary>
    /// 已声明的 <see cref="IDynamicQueryFilter"/> 集合
    /// </summary>
    public ConcurrentDictionary<Type, List<Func<IServiceProvider, IDynamicQueryFilter>>> QueryFilterCollection { get; } = new();

    #endregion Public 属性

    #region InstanceFilter

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加过滤器 <paramref name="queryFilter"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryFilter"></param>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<T>(IDynamicQueryFilter<T> queryFilter)
    {
        return AddFilter(typeof(T), _ => queryFilter);
    }

    #endregion InstanceFilter

    #region TypeFilter

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加过滤器 <typeparamref name="TQueryFilter"/>
    /// </summary>
    /// <typeparam name="TQueryFilter"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<TQueryFilter, T>() where TQueryFilter : IDynamicQueryFilter<T>
    {
        return AddFilter(typeof(T), static serviceProvider => serviceProvider.GetRequiredService<TQueryFilter>());
    }

    #endregion TypeFilter

    #region ExpressionFilter

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<T>(Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter<T>(null!, predicate, order, filterPlace);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<T>(string name, Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<T>(name, order, filterPlace, predicate);
        return AddFilter(typeof(T), _ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<T>(Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter<T>(null!, predicateFactory, order, filterPlace);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <param name="filterPlace"></param>
    /// <returns></returns>
    public EntityFrameworkDynamicFilterOptions AddFilter<T>(string name, Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder, DynamicQueryFilterPlace filterPlace = DynamicQueryFilterPlace.Default)
    {
        return AddFilter(typeof(T), serviceProvider => new ExpressionDynamicQueryFilter<T>(name, order, filterPlace, predicateFactory(serviceProvider)));
    }

    #endregion ExpressionFilter

    #region Internal 方法

    internal EntityFrameworkDynamicFilterOptions AddFilter(Type targetType, Func<IServiceProvider, IDynamicQueryFilter> queryFilterFactory)
    {
        var factories = QueryFilterCollection.GetOrAdd(targetType, _ => new());
        factories.Add(queryFilterFactory);
        return this;
    }

    #endregion Internal 方法
}

/// <summary>
/// EntityFrameworkDynamicFilter 拓展
/// </summary>
public static class EntityFrameworkDynamicFilterOptionsExtensions
{
    #region ExpressionFilter

    #region Head

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的头部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddHeadFilter<T>(this EntityFrameworkDynamicFilterOptions options, Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter<T>(null!, predicate, order, DynamicQueryFilterPlace.Head);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的头部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddHeadFilter<T>(this EntityFrameworkDynamicFilterOptions options, string name, Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<T>(name, order, DynamicQueryFilterPlace.Head, predicate);
        return options.AddFilter(typeof(T), _ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的头部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddHeadFilter<T>(this EntityFrameworkDynamicFilterOptions options, Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter<T>(null!, predicateFactory, order, DynamicQueryFilterPlace.Head);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的头部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddHeadFilter<T>(this EntityFrameworkDynamicFilterOptions options, string name, Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter(typeof(T), serviceProvider => new ExpressionDynamicQueryFilter<T>(name, order, DynamicQueryFilterPlace.Head, predicateFactory(serviceProvider)));
    }

    #endregion Head

    #region Tail

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的尾部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddTailFilter<T>(this EntityFrameworkDynamicFilterOptions options, Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter<T>(null!, predicate, order, DynamicQueryFilterPlace.Tail);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加基于表达式 <paramref name="predicate"/> 的尾部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddTailFilter<T>(this EntityFrameworkDynamicFilterOptions options, string name, Expression<Func<T, bool>> predicate, int order = IDynamicQueryFilter.DefaultOrder)
    {
        var queryFilter = new ExpressionDynamicQueryFilter<T>(name, order, DynamicQueryFilterPlace.Tail, predicate);
        return options.AddFilter(typeof(T), _ => queryFilter);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的尾部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddTailFilter<T>(this EntityFrameworkDynamicFilterOptions options, Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter<T>(null!, predicateFactory, order, DynamicQueryFilterPlace.Tail);
    }

    /// <summary>
    /// 为实体 <typeparamref name="T"/> 添加从 <paramref name="predicateFactory"/> 获取表达式的尾部过滤器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <param name="predicateFactory"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    public static EntityFrameworkDynamicFilterOptions AddTailFilter<T>(this EntityFrameworkDynamicFilterOptions options, string name, Func<IServiceProvider, Expression<Func<T, bool>>> predicateFactory, int order = IDynamicQueryFilter.DefaultOrder)
    {
        return options.AddFilter(typeof(T), serviceProvider => new ExpressionDynamicQueryFilter<T>(name, order, DynamicQueryFilterPlace.Tail, predicateFactory(serviceProvider)));
    }

    #endregion Tail

    #endregion ExpressionFilter
}
