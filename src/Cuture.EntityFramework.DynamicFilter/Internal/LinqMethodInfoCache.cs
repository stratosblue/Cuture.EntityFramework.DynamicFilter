using System.Collections.Immutable;
using System.ComponentModel.RuntimeValidation;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Internal;

/// <summary>
/// Linq的方法信息缓存
/// </summary>
internal static class LinqMethodInfoCache
{
    #region Public 属性

    /// <summary>
    /// <see cref="IEnumerable{T}"/> 的 <see cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/> 方法信息
    /// </summary>
    public static MethodInfo EnumerableWhereMethod { get; }

    /// <summary>
    /// 投影方法集合
    /// </summary>
    public static ImmutableHashSet<MethodInfo> ProjectionMethods { get; }

    /// <summary>
    /// <see cref="IQueryable{T}"/> 的 <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> 方法信息
    /// </summary>
    public static MethodInfo QueryableWhereMethod { get; }

    /// <summary>
    /// 需要跳过的方法集合
    /// </summary>
    public static ImmutableHashSet<MethodInfo> SkipMethods { get; }

    /// <summary>
    /// 支持的查询方法名称集合
    /// </summary>
    public static ImmutableHashSet<string> SupportMethodNames { get; } =
            [
            nameof(Queryable.Where),
            nameof(Queryable.All),
            nameof(Queryable.Any),
            nameof(Queryable.First),
            nameof(Queryable.FirstOrDefault),
            nameof(Queryable.Last),
            nameof(Queryable.LastOrDefault),
            nameof(Queryable.Count),
            nameof(Queryable.Single),
            nameof(Queryable.SingleOrDefault),
        ];

    /// <summary>
    /// 支持的查询方法集合
    /// </summary>
    public static ImmutableHashSet<MethodInfo> SupportMethods { get; }

    #endregion Public 属性

    #region Public 构造函数

    static LinqMethodInfoCache()
    {
        Type[] queryMethodParameterTypes = [
            typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), typeof(bool))),
        ];

        Type[] enumerableQueryMethodParameterTypes = [
            typeof(IEnumerable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), typeof(bool)),
        ];

        SupportMethods = [
            .. SupportMethodNames.Select(name => typeof(Queryable).GetMethod(name, queryMethodParameterTypes).Required()),
            .. SupportMethodNames.Select(name => typeof(Enumerable).GetMethod(name, enumerableQueryMethodParameterTypes).Required()),
        ];

        QueryableWhereMethod = typeof(Queryable).GetMethod(nameof(Queryable.Where), queryMethodParameterTypes).Required();
        EnumerableWhereMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Where), enumerableQueryMethodParameterTypes).Required();

        Type[] queryableSelectMethodParameterTypes = [
            typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)))
        ];

        Type[] queryableSelectManyMethodParameterTypes = [
            typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0),typeof(IEnumerable<>).MakeGenericType( Type.MakeGenericMethodParameter(1))))
        ];

        ProjectionMethods = [
            typeof(Queryable).GetMethod(nameof(Queryable.Select), queryableSelectMethodParameterTypes).Required(),
            typeof(Queryable).GetMethod(nameof(Queryable.SelectMany), queryableSelectManyMethodParameterTypes).Required(),
        ];

        SkipMethods = [
#if NET8_0
            typeof(RelationalQueryableExtensions).GetTypeInfo().GetDeclaredMethod("ExecuteDelete").Required(),
            typeof(RelationalQueryableExtensions).GetTypeInfo().GetDeclaredMethod("ExecuteUpdate").Required(),
#elif NET9_0
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod("ExecuteDelete").Required(),
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod("ExecuteUpdate").Required(),
#elif NET10_0_OR_GREATER
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod("ExecuteDelete").Required(),
            typeof(EntityFrameworkQueryableExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m => m.Name == "ExecuteUpdate" && m.GetParameters()[1].ParameterType == typeof(IReadOnlyList<System.Runtime.CompilerServices.ITuple>)).Required(),
#endif
            //Queryable
            ..GetQueryableExtensionMethods(nameof(Queryable.Order)),
            ..GetQueryableExtensionMethods(nameof(Queryable.OrderBy)),
            ..GetQueryableExtensionMethods(nameof(Queryable.OrderDescending)),
            ..GetQueryableExtensionMethods(nameof(Queryable.OrderByDescending)),
            ..GetQueryableExtensionMethods(nameof(Queryable.Max)),
            ..GetQueryableExtensionMethods(nameof(Queryable.MaxBy)),
            ..GetQueryableExtensionMethods(nameof(Queryable.Min)),
            ..GetQueryableExtensionMethods(nameof(Queryable.MinBy)),
            ..GetQueryableExtensionMethods(nameof(Queryable.GroupBy)),
            ..GetQueryableExtensionMethods(nameof(Queryable.Sum)),
            ..GetQueryableExtensionMethods(nameof(Queryable.Skip)),
            ..GetQueryableExtensionMethods(nameof(Queryable.SkipLast)),
            ..GetQueryableExtensionMethods(nameof(Queryable.SkipWhile)),
            ..GetQueryableExtensionMethods(nameof(Queryable.Take)),
            ..GetQueryableExtensionMethods(nameof(Queryable.TakeLast)),
            ..GetQueryableExtensionMethods(nameof(Queryable.TakeWhile)),

            //Enumerable
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Order)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.OrderBy)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.OrderDescending)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.OrderByDescending)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Max)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.MaxBy)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Min)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.MinBy)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.GroupBy)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Sum)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Skip)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.SkipLast)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.SkipWhile)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.Take)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.TakeLast)),
            ..GetEnumerableExtensionMethods(nameof(Enumerable.TakeWhile)),
        ];

        static IEnumerable<MethodInfo> GetEnumerableExtensionMethods(string name) => GetPublicStaticMethods(typeof(Enumerable), name);
        static IEnumerable<MethodInfo> GetQueryableExtensionMethods(string name) => GetPublicStaticMethods(typeof(Queryable), name);
        static IEnumerable<MethodInfo> GetPublicStaticMethods(Type type, string name) => type.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name == name);
    }

    #endregion Public 构造函数
}
