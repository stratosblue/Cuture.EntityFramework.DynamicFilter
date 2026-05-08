using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel.RuntimeValidation;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace Cuture.EntityFramework.DynamicFilter.Internal;

internal sealed partial class DynamicFilterQueryExpressionInterceptor(DynamicQueryFilterFactoryScopeContainer queryFilterFactoryScopeContainer, ILogger<DynamicFilterQueryExpressionInterceptor> logger)
{
    #region Public 字段

    /// <summary>
    /// 投影方法集合
    /// </summary>
    public static readonly ImmutableHashSet<MethodInfo> ProjectionMethods;

    /// <summary>
    /// 需要跳过的方法集合
    /// </summary>
    public static readonly ImmutableHashSet<MethodInfo> SkipMethods;

    /// <summary>
    /// 支持的查询方法名称集合
    /// </summary>
    public static readonly ImmutableHashSet<string> SupportMethodNames =
            [
            nameof(Queryable.Where),
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
    public static readonly ImmutableHashSet<MethodInfo> SupportMethods;

    #endregion Public 字段

    #region Private 字段

    private static readonly MethodInfo s_queryableWhereMethod;

    private readonly DynamicQueryFilterFactoryScopeContainer _queryFilterFactoryScopeContainer = queryFilterFactoryScopeContainer.Required();

    #endregion Private 字段

    #region Public 构造函数

    static DynamicFilterQueryExpressionInterceptor()
    {
        Type[] queryMethodParameterTypes = [
            typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), typeof(bool))),
        ];

        SupportMethods = [.. SupportMethodNames.Select(name => typeof(Queryable).GetMethod(name, queryMethodParameterTypes).Required())];

        s_queryableWhereMethod = typeof(Queryable).GetMethod(nameof(Queryable.Where), queryMethodParameterTypes).Required();

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
        ];

        static IEnumerable<MethodInfo> GetQueryableExtensionMethods(string name) => GetPublicStaticMethods(typeof(Queryable), name);
        static IEnumerable<MethodInfo> GetPublicStaticMethods(Type type, string name) => type.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name == name);
    }

    #endregion Public 构造函数

    #region Public 方法

    public Expression Resolve(Expression expression, ParameterValues parameterValues)
    {
        int parameterCount = 0;
        bool ignoreQueryFilters = false;
        var context = new ExpressionResolveContext()
        {
            ParameterValues = parameterValues,
            ParameterCount = ref parameterCount,
            IgnoreQueryFilters = ref ignoreQueryFilters,
            ExpressionTypeStack = [],
            FilterContext = new(),
        };
        return Resolve(expression, ref context);
    }

    #endregion Public 方法

    #region Private 方法

    private Expression Resolve(Expression expression, ref ExpressionResolveContext context)
    {
        switch (expression)
        {
            case MethodCallExpression methodCallExpression:
                {
                    var genericTargetMethod = methodCallExpression.Method.IsGenericMethod
                                              ? methodCallExpression.Method.GetGenericMethodDefinition()
                                              : null;
                    if (genericTargetMethod is { } targetMethod
                        && methodCallExpression.Arguments.Count == 2)   //当前方法可能为支持的查询方法
                    {
                        //所有的支持方法都是两个参数，第一个为前一个表达式，第二个为查询表达式
                        //前一个表达式
                        var preExpression = methodCallExpression.Arguments[0];
                        //查询表达式
                        var queryExpression = methodCallExpression.Arguments[1];

                        if (SupportMethods.Contains(targetMethod))  //当前方法为支持的查询方法
                        {
                            var currentQueryStackIndex = context.ExpressionTypeStack.Count;
                            context.ExpressionTypeStack.Add(queryExpression.Type);

                            var processedPreExpression = Resolve(preExpression, ref context);
                            //尝试解析内部是否有子查询
                            var processedQueryExpression = ResolveNext(queryExpression, ref context);

                            var filterContext = context.FilterContext;
                            var queryFilterTargetStackIndex = context.QueryFilterTargetStackIndex;

                            //当前为目标尾部查询，且存在尾部筛选器
                            if (currentQueryStackIndex == queryFilterTargetStackIndex.Tail
                                && filterContext.TailQueryFilters is not null)
                            {
                                QueryFilterLambdaExpressionCombiner.TryAndAlso(ref processedQueryExpression, filterContext.TailQueryFilters, ref context);
                            }
                            //当前为目标头部查询，且存在头部筛选器
                            if (currentQueryStackIndex == queryFilterTargetStackIndex.Head
                                && filterContext.HeadQueryFilters is not null)
                            {
                                QueryFilterLambdaExpressionCombiner.TryAndAlso(ref processedQueryExpression, filterContext.HeadQueryFilters, ref context);
                            }

                            if (!ReferenceEquals(preExpression, processedPreExpression)
                                || !ReferenceEquals(queryExpression, processedQueryExpression))
                            {
                                return Expression.Call(methodCallExpression.Method,
                                                       processedPreExpression,
                                                       processedQueryExpression);
                            }
                        }
                        else if (targetMethod == CutureEFDynamicFilterQueryableExtensions.IgnoreQueryFilterByNameMethodInfo)  //当前方法为按名称忽略筛选器
                        {
                            string? filterName = null;
#if NET10_0_OR_GREATER
                            if (queryExpression is QueryParameterExpression parameterExpression
                                && context.ParameterValues.TryGetValue(parameterExpression.Name!, out var filterNameObject))
#else
                            if (queryExpression is ParameterExpression parameterExpression
                                && context.ParameterValues.ParameterValues.TryGetValue(parameterExpression.Name!, out var filterNameObject))
#endif
                            {
                                filterName = filterNameObject as string;
                            }

                            if (string.IsNullOrEmpty(filterName))
                            {
                                throw new InvalidOperationException($"Invalid ignore query filter expression \"{methodCallExpression}\".");
                            }

                            context.FilterContext.AddIgnoreFilter(filterName);

                            return Resolve(methodCallExpression.Arguments[0], ref context);
                        }
                        else if (SkipMethods.Contains(targetMethod))   //需要跳过的方法，不处理当前查询表达式
                        {
                            var processedPreExpression = Resolve(preExpression, ref context);

                            if (!ReferenceEquals(preExpression, processedPreExpression))
                            {
                                return Expression.Call(methodCallExpression.Method,
                                                       processedPreExpression,
                                                       queryExpression);
                            }
                        }
                        else //当前方法为其它方法，尝试解析内部是否有子查询
                        {
                            var processedPreExpression = Resolve(preExpression, ref context);

                            //尝试解析内部是否有子查询
                            var processedQueryExpression = ResolveNext(queryExpression, ref context);

                            if (!ReferenceEquals(preExpression, processedPreExpression)
                                || !ReferenceEquals(queryExpression, processedQueryExpression))
                            {
                                return Expression.Call(methodCallExpression.Method,
                                                       processedPreExpression,
                                                       processedQueryExpression);
                            }
                        }
                    }
                    else if (genericTargetMethod == CutureEFDynamicFilterQueryableExtensions.IgnoreQueryFilterByTypeMethodInfo)  //当前方法为按类型忽略筛选器
                    {
                        var genericArguments = methodCallExpression.Method.GetGenericArguments();
                        Debug.Assert(genericArguments.Length == 2);

                        context.FilterContext.AddIgnoreFilter(genericArguments[1]);

                        return Resolve(methodCallExpression.Arguments[0], ref context);
                    }
                    else if (genericTargetMethod == CutureEFDynamicFilterQueryableExtensions.EFIgnoreQueryFiltersMethodInfo)  //当前方法为EF的忽略所有QueryFilter
                    {
                        context.IgnoreQueryFilters = true;
                    }
                    else    //当前方法不是支持的查询方法，尝试解析参数是否有子查询
                    {
                        var modified = false;
                        var arguments = new Expression[methodCallExpression.Arguments.Count];
                        for (int i = 0; i < methodCallExpression.Arguments.Count; i++)
                        {
                            var argument = methodCallExpression.Arguments[i];

                            //第一个参数默认认为其为前一个表达式，属于当前的查询层级，使用当前状态，后续参数使用新的状态
                            var processedArgument = i == 0
                                                    ? Resolve(argument, ref context)
                                                    : ResolveNext(argument, ref context);

                            if (!ReferenceEquals(argument, processedArgument))
                            {
                                modified = true;
                            }
                            arguments[i] = processedArgument;
                        }
                        if (modified)
                        {
                            return Expression.Call(methodCallExpression.Method, arguments);
                        }
                    }
                }
                break;

            case EntityQueryRootExpression entityQueryRootExpression:
                {
                    //所有查询都会在根节点收敛
                    if (context.IgnoreQueryFilters)
                    {
                        break;
                    }

                    //查询根节点，获取当前查询的相关信息
                    var targetElementType = entityQueryRootExpression.ElementType;
                    var predicateExpressionType = _queryFilterFactoryScopeContainer.GetPredicateExpressionType(targetElementType);
                    var predicateFuncType = _queryFilterFactoryScopeContainer.GetPredicateFuncType(targetElementType);
                    var queryFilters = _queryFilterFactoryScopeContainer.GetFilters(targetElementType);

                    if (queryFilters is not null
                        && predicateExpressionType is not null
                        && predicateFuncType is not null)
                    {
                        var filterContext = context.FilterContext;

                        List<string>? ignoreFilterNames = filterContext.IgnoreFilterNames;
                        List<Type>? ignoreFilterTypes = filterContext.IgnoreFilterTypes;

                        bool IsIgnoredFilter(IDynamicQueryFilter filter)
                        {
                            if (ignoreFilterNames is not null)
                            {
                                for (int i = ignoreFilterNames.Count - 1; i >= 0; i--)
                                {
                                    if (string.Compare(ignoreFilterNames[i], filter.Name) == 0)
                                    {
                                        return true;
                                    }
                                }
                            }
                            if (ignoreFilterTypes is not null)
                            {
                                for (int i = ignoreFilterTypes.Count - 1; i >= 0; i--)
                                {
                                    if (ignoreFilterTypes[i] == filter.GetType())
                                    {
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }

#pragma warning disable IDE0305

                        filterContext.TailQueryFilters = queryFilters.Where(m => m.IsEnable && !IsIgnoredFilter(m) && m.Place == DynamicQueryFilterPlace.Tail)
                                                                     .OrderBy(static m => m.Order)
                                                                     .ToList();
                        filterContext.HeadQueryFilters = queryFilters.Where(m => m.IsEnable && !IsIgnoredFilter(m) && m.Place != DynamicQueryFilterPlace.Tail)
                                                                     .OrderByDescending(static m => m.Order)
                                                                     .ToList();

                        context.CurrentPredicateFuncType = predicateFuncType;

#pragma warning restore IDE0305

                        var headQueryFilterTargetStackIndex = context.ExpressionTypeStack.FindLastIndex(m => m == predicateExpressionType);
                        var tailQueryFilterTargetStackIndex = context.ExpressionTypeStack.FindIndex(m => m == predicateExpressionType);

                        if (headQueryFilterTargetStackIndex != -1
                            && tailQueryFilterTargetStackIndex != -1)
                        {
                            context.QueryFilterTargetStackIndex = new(headQueryFilterTargetStackIndex, tailQueryFilterTargetStackIndex);
                        }
                        else    //没有表达式，则为裸查询，直接添加筛选
                        {
                            Expression? queryExpression = null;
                            ParameterExpression? parameter = null;

                            var filters = filterContext.HeadQueryFilters.Reverse().Concat(filterContext.TailQueryFilters);

                            foreach (var queryFilter in filters)
                            {
                                var underlyingExpression = queryFilter.UnderlyingExpression;
                                if (queryExpression is null)
                                {
                                    parameter = underlyingExpression.Parameters[0];

                                    var parameterizeBody = QueryExpressionParameterExtractor.Extracting(underlyingExpression.Body, ref context);
                                    queryExpression = ExpressionParameterReplacer.Replace(parameterizeBody, underlyingExpression.Parameters[0], parameter!);
                                }
                                else
                                {
                                    var parameterizeBody = QueryExpressionParameterExtractor.Extracting(underlyingExpression.Body, ref context);
                                    var parameterReplacedExpression = ExpressionParameterReplacer.Replace(parameterizeBody, underlyingExpression.Parameters[0], parameter!);

                                    queryExpression = Expression.AndAlso(queryExpression, parameterReplacedExpression);
                                }
                            }

                            if (queryExpression is not null)
                            {
                                var lambdaExpression = Expression.Lambda(queryExpression, parameter!);
                                return Expression.Call(s_queryableWhereMethod.MakeGenericMethod(entityQueryRootExpression.ElementType),
                                                       entityQueryRootExpression,
                                                       Expression.MakeUnary(ExpressionType.Quote, lambdaExpression, lambdaExpression.GetType()));
                            }
                        }
                    }

                    break;
                }

            case UnaryExpression unaryExpression:
                {
                    //展开处理内部的表达式
                    if (unaryExpression.Operand is LambdaExpression lambdaExpression)
                    {
                        var originExpression = lambdaExpression.Body;
                        var processedExpression = originExpression;
                        if (originExpression is MethodCallExpression methodCallExpression)
                        {
                            processedExpression = Resolve(methodCallExpression, ref context);
                        }
                        else if (originExpression is NewExpression newExpression)
                        {
                            var argumentsCount = newExpression.Arguments.Count;

                            using var argumentExpressionMemory = MemoryPool<Expression>.Shared.Rent(argumentsCount);
                            var argumentExpressionBuffer = argumentExpressionMemory.Memory.Span;

                            var index = 0;
                            var modified = false;

                            foreach (var argumentExpression in newExpression.Arguments)
                            {
                                var processedArgumentExpression = argumentExpression;

                                if (argumentExpression.NodeType == ExpressionType.Call)
                                {
                                    processedArgumentExpression = ResolveNext(argumentExpression, ref context);
                                }
                                if (!ReferenceEquals(argumentExpression, processedArgumentExpression))
                                {
                                    modified = true;
                                }

                                argumentExpressionBuffer[index++] = processedArgumentExpression;
                            }

                            if (modified)
                            {
                                //TODO 优化数组创建
                                var constructor = newExpression.Constructor.Required();
                                processedExpression = newExpression.Members is null
                                                      ? Expression.New(constructor, argumentExpressionBuffer[..index].ToArray())
                                                      : Expression.New(constructor, argumentExpressionBuffer[..index].ToArray(), newExpression.Members);
                            }
                        }

                        if (!ReferenceEquals(originExpression, processedExpression))
                        {
                            return Expression.MakeUnary(unaryType: unaryExpression.NodeType,
                                                        operand: Expression.Lambda(body: processedExpression,
                                                                                   name: lambdaExpression.Name,
                                                                                   tailCall: lambdaExpression.TailCall,
                                                                                   parameters: lambdaExpression.Parameters),
                                                        type: unaryExpression.Type);
                        }
                    }
                    break;
                }

#if NET10_0_OR_GREATER

            case QueryParameterExpression or ParameterExpression:
                //忽略参数表达式
                break;
#endif

            default:
                //忽略参数表达式
                if (expression.NodeType is ExpressionType.Parameter or ExpressionType.MemberAccess or ExpressionType.Constant)
                {
                    break;
                }
                //其它表达式类型暂不支持，输出日志
                LogUnsupportedExpression(logger, expression.Type, expression);
                break;
        }

        return expression;
    }

    private Expression ResolveNext(Expression expression, ref ExpressionResolveContext context)
    {
        var filterContext = context.FilterContext;
        var nextContext = new ExpressionResolveContext()
        {
            ParameterValues = context.ParameterValues,
            ParameterCount = ref context.ParameterCount,
            IgnoreQueryFilters = ref context.IgnoreQueryFilters,
            ExpressionTypeStack = [],
            FilterContext = new()
            {
                //将外部的筛选器传入到内部，保证子查询也能正确应用筛选器
                HeadQueryFilters = filterContext.HeadQueryFilters,
                TailQueryFilters = filterContext.TailQueryFilters,
            },
        };
        return Resolve(expression, ref nextContext);
    }

    #region logging

    [LoggerMessage(Level = LogLevel.Warning, Message = "Expression \"{Type}\" that do not support resolve => {Expression}")]
    private static partial void LogUnsupportedExpression(ILogger logger, Type type, Expression expression);

    #endregion logging

    #endregion Private 方法
}
