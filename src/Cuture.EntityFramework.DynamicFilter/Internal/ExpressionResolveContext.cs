namespace Cuture.EntityFramework.DynamicFilter.Internal;

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

    /// <summary>
    /// 查询过滤器目标栈索引
    /// </summary>
    public int QueryFilterTargetStackIndex;

    #endregion Public 字段

    #region Public 属性

    /// <summary>
    /// 当前查询过滤器的目标方法类型
    /// </summary>
    public Type? CurrentPredicateFuncType { get; set; }

    /// <summary>
    /// 表达式类型栈
    /// </summary>
    public required List<Type> ExpressionTypeStack { get; init; }

    /// <summary>
    /// 过滤器上下文
    /// </summary>
    public required FilterContext FilterContext { get; set; }

    /// <summary>
    /// 当前查询的 <see cref="ParameterValues"/>
    /// </summary>
    public required ParameterValues ParameterValues { get; init; }

    #endregion Public 属性

    #region Public 构造函数

    /// <inheritdoc cref="ExpressionResolveContext"/>
    public ExpressionResolveContext()
    {
        QueryFilterTargetStackIndex = -1;
    }

    #endregion Public 构造函数
}
