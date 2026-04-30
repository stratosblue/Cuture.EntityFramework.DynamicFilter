namespace Cuture.EntityFramework.DynamicFilter.Internal;

/// <summary>
/// 投影状态
/// </summary>
internal enum ProjectionState : byte
{
    /// <summary>
    /// 默认
    /// </summary>
    Default,

    /// <summary>
    /// 在投影之前
    /// </summary>
    BeforeProjection,

    /// <summary>
    /// 已投影
    /// </summary>
    Projected,
}
