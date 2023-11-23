using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

/// <summary>
/// 表达式处理上下文
/// </summary>
internal ref struct ExpressionResolveContext
{
    #region Public 字段

    /// <summary>
    /// 是否已启用 EF 的 IgnoreQueryFilters
    /// </summary>
    public ref bool IgnoreQueryFilters;

    /// <summary>
    /// 已解析到的参数数量
    /// </summary>
    public ref int ParameterCount;

    #endregion Public 字段

    #region Public 属性

    /// <summary>
    /// 当前表达式处理上下文的第一个表达式
    /// </summary>
    public Expression? FirstExpression { get; set; }

    /// <summary>
    /// 头部查询过滤器
    /// </summary>
    public IList<IDynamicQueryFilter>? HeadQueryFilters { get; set; }

    /// <summary>
    /// 忽略的过滤器名称列表
    /// </summary>
    public List<string>? IgnoreFilterNames { get; set; }

    /// <summary>
    /// 忽略的过滤器类型列表
    /// </summary>
    public List<Type>? IgnoreFilterTypes { get; set; }

    /// <summary>
    /// 当前表达式处理上下文的最后一个表达式
    /// </summary>
    public Expression? LastExpression { get; set; }

    /// <summary>
    /// 当前查询的 <see cref="IParameterValues"/>
    /// </summary>
    public required IParameterValues ParameterValues { get; init; }

    /// <summary>
    /// 尾部查询过滤器
    /// </summary>
    public IList<IDynamicQueryFilter>? TailQueryFilters { get; set; }

    #endregion Public 属性

    #region Public 方法

    /// <summary>
    /// 添加对类型为 <paramref name="filterType"/> 的过滤器的忽略
    /// </summary>
    /// <param name="filterType"></param>
    public void AddIgnoreFilter(Type filterType)
    {
        if (IgnoreFilterTypes is null)
        {
            IgnoreFilterTypes ??= [filterType];
        }
        else
        {
            IgnoreFilterTypes.Add(filterType);
        }
    }

    /// <summary>
    /// 添加对名称为 <paramref name="filterName"/> 的过滤器的忽略
    /// </summary>
    /// <param name="filterName"></param>
    public void AddIgnoreFilter(string filterName)
    {
        if (IgnoreFilterNames is null)
        {
            IgnoreFilterNames = [filterName];
        }
        else
        {
            IgnoreFilterNames.Add(filterName);
        }
    }

    #endregion Public 方法
}
