namespace Cuture.EntityFramework.DynamicFilter.Test.DatabaseContext;

internal interface ISoftDeletion
{
    #region Public 属性

    public bool IsDeleted { get; set; }

    #endregion Public 属性
}
