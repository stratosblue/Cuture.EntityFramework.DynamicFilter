using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cuture.EntityFramework.DynamicFilter.Test;

public abstract class SimpleQueryTestBase : DbContextTestBase
{
    #region Protected 方法

    protected override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddEntityFrameworkDynamicQueryFilter(options =>
        {
            options.AddHeadFilter<User>(provider =>
            {
                var currentTenant = provider.GetRequiredService<ICurrentTenant>();
                return currentTenant.Id is null
                       ? n => true
                       : n => n.TenantId == currentTenant.Id;
            }, order: 1);
            options.AddHeadFilter<User>(provider =>
            {
                var currentOrganization = provider.GetRequiredService<ICurrentOrganization>();
                return currentOrganization.Id is null
                       ? n => true
                       : n => n.OrganizationId == currentOrganization.Id;
            }, order: 2);
            options.AddTailFilter<User>("SoftDeletion", n => n.IsDeleted == false);

            options.AddHeadFilter<Article>(provider =>
            {
                var currentTenant = provider.GetRequiredService<ICurrentTenant>();
                return currentTenant.Id is null
                       ? n => true
                       : n => n.TenantId == currentTenant.Id;
            }, order: 1);
            options.AddHeadFilter<Article>(provider =>
            {
                var currentOrganization = provider.GetRequiredService<ICurrentOrganization>();
                return currentOrganization.Id is null
                       ? n => true
                       : n => n.OrganizationId == currentOrganization.Id;
            }, order: 2);
            options.AddTailFilter<Article>("SoftDeletion", n => n.IsDeleted == false);
        });

        return services;
    }

    #endregion Protected 方法
}
