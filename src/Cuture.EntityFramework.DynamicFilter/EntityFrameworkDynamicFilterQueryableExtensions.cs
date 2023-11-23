using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// DynamicFilterQueryable拓展
/// </summary>
public static class EntityFrameworkDynamicFilterQueryableExtensions
{
    #region Internal 字段

    internal static readonly MethodInfo EFIgnoreQueryFiltersMethodInfo = typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetMethod(nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters), new Type[] { typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!;

    internal static readonly MethodInfo IgnoreQueryFilterByNameMethodInfo = typeof(EntityFrameworkDynamicFilterQueryableExtensions).GetTypeInfo().GetMethod(nameof(IgnoreQueryFilter), new Type[] { typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(string) })!;

    internal static readonly MethodInfo IgnoreQueryFilterByTypeMethodInfo = typeof(EntityFrameworkDynamicFilterQueryableExtensions).GetTypeInfo().GetMethod(nameof(IgnoreQueryFilter), new Type[] { typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!;

    #endregion Internal 字段

    #region Public 方法

    /// <summary>
    /// 忽略当前查询中 <typeparamref name="TEntity"/> 的类型为 <typeparamref name="TFilter"/> 的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TFilter"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> IgnoreQueryFilter<TEntity, TFilter>(this IQueryable<TEntity> source) where TFilter : IDynamicQueryFilter
    {
        return source.IgnoreQueryFilter(typeof(TFilter));
    }

    /// <summary>
    /// 忽略当前查询中 <typeparamref name="TEntity"/> 的类型为 <paramref name="filterType"/> 的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="source"></param>
    /// <param name="filterType"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> IgnoreQueryFilter<TEntity>(this IQueryable<TEntity> source, Type filterType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filterType);

        return source.Provider is EntityQueryProvider
                    ? source.Provider.CreateQuery<TEntity>(
                        expression: Expression.Call(instance: null,
                                                    method: IgnoreQueryFilterByTypeMethodInfo.MakeGenericMethod(typeof(TEntity), filterType),
                                                    source.Expression))
                    : source;
    }

    /// <summary>
    /// 忽略当前查询中 <typeparamref name="TEntity"/> 的名称为 <paramref name="filterName"/> 的 <see cref="IDynamicQueryFilter"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="source"></param>
    /// <param name="filterName"></param>
    /// <returns></returns>
    public static IQueryable<TEntity> IgnoreQueryFilter<TEntity>(this IQueryable<TEntity> source, string filterName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNullOrEmpty(filterName);

        return source.Provider is EntityQueryProvider
                    ? source.Provider.CreateQuery<TEntity>(
                        expression: Expression.Call(instance: null,
                                                    method: IgnoreQueryFilterByNameMethodInfo.MakeGenericMethod(typeof(TEntity)),
                                                    source.Expression,
                                                    Expression.Constant(filterName)))
                    : source;
    }

    #endregion Public 方法
}
