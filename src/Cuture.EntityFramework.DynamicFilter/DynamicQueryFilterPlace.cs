namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
/// 动态过滤器位置枚举
/// </summary>
public enum DynamicQueryFilterPlace
{
    /// <summary>
    /// 默认(当前默认为头部)
    /// </summary>
    Default,

    /// <summary>
    /// 头部
    /// </summary>
    Head,

    /// <summary>
    /// 尾部
    /// </summary>
    Tail,
}
