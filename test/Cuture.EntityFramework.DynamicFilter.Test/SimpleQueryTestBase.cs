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
            options.Entity<User>(builder =>
            {
                builder.AddHeadFilter(provider =>
                {
                    var currentTenant = provider.GetRequiredService<ICurrentTenant>();
                    return currentTenant.Id is null
                           ? n => true
                           : n => n.TenantId == currentTenant.Id;
                }, order: 1);
                builder.AddHeadFilter(provider =>
                {
                    var currentOrganization = provider.GetRequiredService<ICurrentOrganization>();
                    return currentOrganization.Id is null
                           ? n => true
                           : n => n.OrganizationId == currentOrganization.Id;
                }, order: 2);
                builder.AddTailFilter("SoftDeletion", n => n.IsDeleted == false);
            });

            options.Entity<Article>(builder =>
            {
                builder.AddHeadFilter(provider =>
                {
                    var currentTenant = provider.GetRequiredService<ICurrentTenant>();
                    return currentTenant.Id is null
                           ? n => true
                           : n => n.TenantId == currentTenant.Id;
                }, order: 1);
                builder.AddHeadFilter(provider =>
                {
                    var currentOrganization = provider.GetRequiredService<ICurrentOrganization>();
                    return currentOrganization.Id is null
                           ? n => true
                           : n => n.OrganizationId == currentOrganization.Id;
                }, order: 2);
                builder.AddTailFilter("SoftDeletion", n => n.IsDeleted == false);
            });
        });

        return services;
    }

    #endregion Protected 方法
}
