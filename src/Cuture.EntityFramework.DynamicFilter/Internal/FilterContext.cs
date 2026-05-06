namespace Cuture.EntityFramework.DynamicFilter.Internal;

/// <summary>
/// 过滤器上下文
/// </summary>
internal class FilterContext
{
    #region Public 属性

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
        IgnoreFilterTypes ??= [];
        IgnoreFilterTypes.Add(filterType);
    }

    /// <summary>
    /// 添加对名称为 <paramref name="filterName"/> 的过滤器的忽略
    /// </summary>
    /// <param name="filterName"></param>
    public void AddIgnoreFilter(string filterName)
    {
        IgnoreFilterNames ??= [];
        IgnoreFilterNames.Add(filterName);
    }

    #endregion Public 方法
}
