namespace Cuture.EntityFramework.DynamicFilter.Test;

public interface ICurrentOrganization
{
    #region Public 属性

    public ulong? Id { get; }

    #endregion Public 属性
}

public interface ICurrentOrganizationSetter
{
    #region Public 方法

    public void Set(ulong? id);

    #endregion Public 方法
}

public interface ICurrentTenant
{
    #region Public 属性

    public ulong? Id { get; }

    #endregion Public 属性
}

public interface ICurrentTenantSetter
{
    #region Public 方法

    public void Set(ulong? id);

    #endregion Public 方法
}

internal class CurrentOrganization : ICurrentOrganization
{
    #region Public 属性

    public ulong? Id { get; set; }

    #endregion Public 属性
}

internal class CurrentTenant : ICurrentTenant
{
    #region Public 属性

    public ulong? Id { get; set; }

    #endregion Public 属性
}

internal class CurrentTenantSetter : ICurrentTenantSetter
{
    #region Public 属性

    public CurrentTenant CurrentTenant { get; } = new();

    #endregion Public 属性

    #region Public 方法

    public void Set(ulong? id)
    {
        CurrentTenant.Id = id;
    }

    #endregion Public 方法
}

internal class CurrentOrganizationSetter : ICurrentOrganizationSetter
{
    #region Public 属性

    public CurrentOrganization CurrentOrganization { get; } = new();

    #endregion Public 属性

    #region Public 方法

    public void Set(ulong? id)
    {
        CurrentOrganization.Id = id;
    }

    #endregion Public 方法
}
