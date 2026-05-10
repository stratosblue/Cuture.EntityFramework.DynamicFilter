using System.Buffers;
using System.ComponentModel.RuntimeValidation;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace Cuture.EntityFramework.DynamicFilter.Internal;

internal sealed partial class DynamicFilterQueryExpressionInterceptor(DynamicQueryFilterFactoryScopeContainer queryFilterFactoryScopeContainer,
                                                                      ILogger<DynamicFilterQueryExpressionInterceptor> logger)
{
    #region Private 字段

    private readonly DynamicQueryFilterFactoryScopeContainer _queryFilterFactoryScopeContainer = queryFilterFactoryScopeContainer.Required();

    #endregion Private 字段

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

                        if (LinqMethodInfoCache.SupportMethods.Contains(targetMethod))  //当前方法为支持的查询方法
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
                        else if (LinqMethodInfoCache.SkipMethods.Contains(targetMethod))   //需要跳过的方法，不处理当前查询表达式
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
                    if (TryProcessExpressionRoot(targetElementType: entityQueryRootExpression.ElementType,
                                                 targetElementGroupingType: null,
                                                 expression: entityQueryRootExpression,
                                                 context: ref context,
                                                 processedExpression: out var processedExpression))
                    {
                        return processedExpression;
                    }
                    break;
                }

            case UnaryExpression unaryExpression:
                {
                    //展开处理内部的表达式
                    var originExpression = unaryExpression.Operand;
                    var processedExpression = Resolve(originExpression, ref context);

                    if (!ReferenceEquals(originExpression, processedExpression))
                    {
                        return Expression.MakeUnary(unaryType: unaryExpression.NodeType,
                                                    operand: processedExpression,
                                                    type: unaryExpression.Type);
                    }
                    break;
                }

            case NewExpression newExpression:
                {
                    var argumentsCount = newExpression.Arguments.Count;

                    using var argumentExpressionMemory = MemoryPool<Expression>.Shared.Rent(argumentsCount);
                    var argumentExpressionBuffer = argumentExpressionMemory.Memory.Span;

                    var index = 0;
                    var modified = false;

                    foreach (var argumentExpression in newExpression.Arguments)
                    {
                        var processedArgumentExpression = argumentExpression;

                        if (argumentExpression.NodeType == ExpressionType.Call
                            || argumentExpression.NodeType == ExpressionType.Coalesce
                            || argumentExpression is BinaryExpression or UnaryExpression)
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
                        return newExpression.Members is null
                               ? Expression.New(constructor, argumentExpressionBuffer[..index].ToArray())
                               : Expression.New(constructor, argumentExpressionBuffer[..index].ToArray(), newExpression.Members);
                    }
                    break;
                }

            case LambdaExpression lambdaExpression:
                {
                    var originExpression = lambdaExpression.Body;
                    var processedExpression = Resolve(originExpression, ref context);

                    if (!ReferenceEquals(originExpression, processedExpression))
                    {
                        return Expression.Lambda(body: processedExpression,
                                                 name: lambdaExpression.Name,
                                                 tailCall: lambdaExpression.TailCall,
                                                 parameters: lambdaExpression.Parameters);
                    }
                    break;
                }

            case BinaryExpression binaryExpression:
                {
                    var processedLeft = Resolve(binaryExpression.Left, ref context);
                    var processedRight = Resolve(binaryExpression.Right, ref context);
                    if (!ReferenceEquals(binaryExpression.Left, processedLeft)
                        || !ReferenceEquals(binaryExpression.Right, processedRight))
                    {
                        return Expression.MakeBinary(binaryType: binaryExpression.NodeType,
                                                     left: processedLeft,
                                                     right: processedRight,
                                                     liftToNull: binaryExpression.IsLiftedToNull,
                                                     method: binaryExpression.Method,
                                                     conversion: binaryExpression.Conversion);
                    }
                    break;
                }

#if NET10_0_OR_GREATER

            case QueryParameterExpression queryParameterExpression:
                //忽略参数表达式
                break;

#endif

            case ParameterExpression parameterExpression:
                {
                    var parameterType = parameterExpression.Type;
                    //特化支持对 IGrouping 的处理
                    if (parameterType.IsGenericType
                        && parameterType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                    {
                        var targetElementType = parameterType.GenericTypeArguments[1];
                        if (TryProcessExpressionRoot(targetElementType: targetElementType,
                                                     targetElementGroupingType: parameterType,
                                                     expression: parameterExpression,
                                                     context: ref context,
                                                     processedExpression: out var processedExpression))
                        {
                            return processedExpression;
                        }
                    }
                    //忽略参数表达式
                    break;
                }

            default:
                //忽略参数表达式
                if (expression.NodeType is ExpressionType.Parameter or ExpressionType.MemberAccess or ExpressionType.Constant)
                {
                    break;
                }
                //其它表达式类型暂不支持，输出日志
                LogUnsupportedExpression(logger, expression.NodeType, expression.Type, expression);
                break;
        }

        return expression;
    }

    private Expression ResolveNext(Expression expression, ref ExpressionResolveContext context)
    {
        var filterContext = context.FilterContext;
        var ignoreQueryFilters = context.IgnoreQueryFilters;
        var nextContext = new ExpressionResolveContext()
        {
            ParameterValues = context.ParameterValues,
            ParameterCount = ref context.ParameterCount,
            IgnoreQueryFilters = ref ignoreQueryFilters,
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

    /// <summary>
    /// 尝试处理表达式根
    /// </summary>
    /// <param name="targetElementType">目标元素类型</param>
    /// <param name="targetElementGroupingType">目标元素分组类型 (仅在分组时传递)</param>
    /// <param name="expression">原表达式</param>
    /// <param name="context">解析上下文</param>
    /// <param name="processedExpression">处理后的表达式</param>
    /// <returns></returns>
    private bool TryProcessExpressionRoot(Type targetElementType,
                                          Type? targetElementGroupingType,
                                          Expression expression,
                                          ref ExpressionResolveContext context,
                                          [NotNullWhen(true)] out Expression? processedExpression)
    {
        processedExpression = null;

        //所有查询都会在根节点收敛
        if (context.IgnoreQueryFilters)
        {
            return false;
        }

        //查询根节点，获取当前查询的相关信息
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

                    MethodInfo method;
                    Expression predicateExpression;

                    if (targetElementGroupingType is null)
                    {
                        method = LinqMethodInfoCache.QueryableWhereMethod.MakeGenericMethod(targetElementType);
                        predicateExpression = Expression.MakeUnary(ExpressionType.Quote, lambdaExpression, lambdaExpression.GetType());
                    }
                    else
                    {
                        method = LinqMethodInfoCache.EnumerableWhereMethod.MakeGenericMethod(targetElementType);
                        predicateExpression = lambdaExpression;
                    }

                    processedExpression = Expression.Call(method: method,
                                                          arg0: expression,
                                                          arg1: predicateExpression);
                    return true;
                }
            }
        }

        return false;
    }

    #region logging

    [LoggerMessage(Level = LogLevel.Warning, Message = "Expression [{NodeType}] \"{Type}\" that do not support resolve => {Expression}")]
    private static partial void LogUnsupportedExpression(ILogger logger, ExpressionType nodeType, Type type, Expression expression);

    #endregion logging

    #endregion Private 方法
}
