using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

internal sealed class DynamicFilterQueryExpressionInterceptor
{
    #region Public 字段

    public static readonly ImmutableHashSet<string> SupportMethodNames = ImmutableHashSet.Create(nameof(Queryable.Where),
                                                                                                 nameof(Queryable.Any),
                                                                                                 nameof(Queryable.First),
                                                                                                 nameof(Queryable.FirstOrDefault),
                                                                                                 nameof(Queryable.Last),
                                                                                                 nameof(Queryable.LastOrDefault));

    public static readonly ImmutableHashSet<MethodInfo> SupportMethods;

    #endregion Public 字段

    #region Private 字段

    private static readonly MethodInfo s_queryableWhereMethod;

    private readonly DynamicQueryFilterFactoryScopeContainer _queryFilterFactoryScopeContainer;

    #endregion Private 字段

    #region Public 构造函数

    static DynamicFilterQueryExpressionInterceptor()
    {
        var queryMethodParameterTypes = new Type[]
        {
            typeof(IQueryable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), typeof(bool)))
        };

        SupportMethods = SupportMethodNames.Select(name => typeof(Queryable).GetMethod(name, queryMethodParameterTypes)!).ToImmutableHashSet();

        s_queryableWhereMethod = typeof(Queryable).GetMethod(nameof(Queryable.Where), queryMethodParameterTypes)!;
    }

    public DynamicFilterQueryExpressionInterceptor(DynamicQueryFilterFactoryScopeContainer queryFilterFactoryScopeContainer)
    {
        _queryFilterFactoryScopeContainer = queryFilterFactoryScopeContainer ?? throw new ArgumentNullException(nameof(queryFilterFactoryScopeContainer));
    }

    #endregion Public 构造函数

    #region Public 方法

    public Expression Resolve(Expression expression, IParameterValues parameterValues)
    {
        int parameterCount = 0;
        bool ignoreQueryFilters = false;
        var context = new ExpressionResolveContext()
        {
            ParameterValues = parameterValues,
            ParameterCount = ref parameterCount,
            IgnoreQueryFilters = ref ignoreQueryFilters,
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
                    if (methodCallExpression.Method.IsGenericMethod
                        && methodCallExpression.Arguments.Count == 2
                        && methodCallExpression.Method.GetGenericMethodDefinition() is { } targetMethod)   //当前方法可能为支持的查询方法
                    {
                        if (SupportMethods.Contains(targetMethod))  //当前方法为支持的查询方法
                        {
                            var isCurrentLast = context.LastExpression is null;
                            if (isCurrentLast)
                            {
                                context.LastExpression = methodCallExpression;
                            }
                            context.FirstExpression = methodCallExpression;

                            //所有的支持方法都是两个参数，第一个为前一个表达式，第二个为查询表达式
                            var preExpression = methodCallExpression.Arguments[0];
                            var queryExpression = methodCallExpression.Arguments[1];

                            var processedPreExpression = Resolve(preExpression, ref context);
                            var processedQueryExpression = queryExpression;

                            var isQueryNotModified = true;
                            if (isCurrentLast && context.TailQueryFilters is not null)
                            {
                                //尝试解析内部是否有子查询
                                processedQueryExpression = ResolveNext(processedQueryExpression, ref context);
                                processedQueryExpression = QueryFilterLambdaExpressionCombiner.AndAlso(processedQueryExpression, context.TailQueryFilters!, ref context);
                                isQueryNotModified = false;
                            }
                            if (ReferenceEquals(context.FirstExpression, methodCallExpression) && context.HeadQueryFilters is not null)
                            {
                                //尝试解析内部是否有子查询
                                processedQueryExpression = ResolveNext(processedQueryExpression, ref context);
                                processedQueryExpression = QueryFilterLambdaExpressionCombiner.AndAlso(processedQueryExpression, context.HeadQueryFilters!, ref context);
                                isQueryNotModified = false;
                            }

                            if (isQueryNotModified) //查询未修改，尝试解析内部是否有子查询
                            {
                                processedQueryExpression = ResolveNext(processedQueryExpression, ref context);
                            }

                            if (!ReferenceEquals(preExpression, processedPreExpression)
                                || !ReferenceEquals(queryExpression, processedQueryExpression))
                            {
                                return Expression.Call(methodCallExpression.Method,
                                                       processedPreExpression,
                                                       processedQueryExpression);
                            }
                        }
                        else if (targetMethod == EntityFrameworkDynamicFilterQueryableExtensions.IgnoreQueryFilterByNameMethodInfo)  //当前方法为按名称忽略筛选器
                        {
                            Debug.Assert(methodCallExpression.Arguments.Count == 2);

                            if (methodCallExpression.Arguments[1] is not ParameterExpression parameterExpression
                                || !context.ParameterValues.ParameterValues.TryGetValue(parameterExpression.Name!, out var filterNameObject)
                                || filterNameObject is not string filterName
                                || string.IsNullOrEmpty(filterName))
                            {
                                throw new InvalidOperationException($"Invalid ignore query filter expression \"{methodCallExpression}\".");
                            }

                            context.AddIgnoreFilter(filterName);

                            return Resolve(methodCallExpression.Arguments[0], ref context);
                        }
                        else //当前方法为其它方法，尝试解析内部是否有子查询
                        {
                            //第一个为前一个表达式，第二个为查询表达式
                            var preExpression = methodCallExpression.Arguments[0];
                            var queryExpression = methodCallExpression.Arguments[1];

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
                    else if (methodCallExpression.Method.GetGenericMethodDefinition() == EntityFrameworkDynamicFilterQueryableExtensions.IgnoreQueryFilterByTypeMethodInfo)  //当前方法为按类型忽略筛选器
                    {
                        var genericArguments = methodCallExpression.Method.GetGenericArguments();
                        Debug.Assert(genericArguments.Length == 2);

                        context.AddIgnoreFilter(genericArguments[1]);

                        return Resolve(methodCallExpression.Arguments[0], ref context);
                    }
                    else if (methodCallExpression.Method.GetGenericMethodDefinition() == EntityFrameworkDynamicFilterQueryableExtensions.EFIgnoreQueryFiltersMethodInfo)  //当前方法为EF的忽略所有QueryFilter
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
                    if (context.IgnoreQueryFilters)
                    {
                        break;
                    }
                    //查询根节点，获取当前查询的相关信息
                    var queryFilters = _queryFilterFactoryScopeContainer.GetFilters(entityQueryRootExpression.ElementType);
                    if (queryFilters is not null)
                    {
                        List<string>? ignoreFilterNames = context.IgnoreFilterNames;
                        List<Type>? ignoreFilterTypes = context.IgnoreFilterTypes;

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

                        context.TailQueryFilters = queryFilters.Where(m => m.IsEnable && !IsIgnoredFilter(m) && m.Place == DynamicQueryFilterPlace.Tail)
                                                               .OrderBy(static m => m.Order)
                                                               .ToList();
                        context.HeadQueryFilters = queryFilters.Where(m => m.IsEnable && !IsIgnoredFilter(m) && m.Place != DynamicQueryFilterPlace.Tail)
                                                                .OrderByDescending(static m => m.Order)
                                                                .ToList();

                        //没有表达式，则为裸查询，直接添加筛选
                        if (context.FirstExpression is null)
                        {
                            Expression? queryExpression = null;
                            ParameterExpression? parameter = null;

                            foreach (var queryFilter in context.HeadQueryFilters.Reverse().Concat(context.TailQueryFilters))
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
                    if (unaryExpression.Operand is LambdaExpression lambdaExpression
                        && lambdaExpression.Body is MethodCallExpression methodCallExpression
                        && Resolve(methodCallExpression, ref context) is { } processedMethodCallExpression
                        && !ReferenceEquals(methodCallExpression, processedMethodCallExpression))
                    {
                        return Expression.MakeUnary(unaryType: unaryExpression.NodeType,
                                                    operand: Expression.Lambda(body: processedMethodCallExpression,
                                                                               name: lambdaExpression.Name,
                                                                               tailCall: lambdaExpression.TailCall,
                                                                               parameters: lambdaExpression.Parameters),
                                                    type: unaryExpression.Type);
                    }
                    break;
                }

            default:
                throw new InvalidOperationException($"Unsupported expression type \"{expression.Type}\" => {expression}");
        }

        return expression;
    }

    private Expression ResolveNext(Expression expression, ref ExpressionResolveContext context)
    {
        var nextContext = new ExpressionResolveContext()
        {
            ParameterValues = context.ParameterValues,
            ParameterCount = ref context.ParameterCount,
            IgnoreQueryFilters = ref context.IgnoreQueryFilters,
            IgnoreFilterNames = context.IgnoreFilterNames,
            IgnoreFilterTypes = context.IgnoreFilterTypes,
        };
        return Resolve(expression, ref nextContext);
    }

    #endregion Private 方法
}
