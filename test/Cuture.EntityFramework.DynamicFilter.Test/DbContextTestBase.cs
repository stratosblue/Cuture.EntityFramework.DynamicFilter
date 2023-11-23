using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cuture.EntityFramework.DynamicFilter.Test;

public abstract class DbContextTestBase
{
    #region Protected 字段

    protected ServiceProvider RootServiceProvider = null!;

    protected (List<User> Users, List<Article> Articles) SeedData;

    protected AsyncServiceScope ServiceScope;

    #endregion Protected 字段

    #region Private 字段

    private SqliteConnection _connection = null!;

    private bool _enableLogger = false;

    #endregion Private 字段

    #region Protected 属性

    protected IServiceProvider ScopeServiceProvider => ServiceScope.ServiceProvider;

    #endregion Protected 属性

    #region Public 方法

    [TestCleanup]
    public virtual async Task TestCleanupAsync()
    {
        await ServiceScope.DisposeAsync();
        await RootServiceProvider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestInitialize]
    public virtual async Task TestInitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        IServiceCollection services = new ServiceCollection();

        services.AddLogging(builder => builder.AddSimpleConsole().AddFilter((category, level) => _enableLogger && level > LogLevel.Debug));
        services.AddSqlite<TestEFDbContext>(connectionString: null,
                                            sqliteOptionsAction: null,
                                            optionsAction: builder =>
                                            {
                                                builder.UseSqlite(_connection);
                                                builder.UseDynamicQueryFilter();
                                            });

        services.AddScoped<CurrentTenantSetter>();
        services.AddScoped<CurrentOrganizationSetter>();

        services.AddScoped<ICurrentTenantSetter, CurrentTenantSetter>(provider => provider.GetRequiredService<CurrentTenantSetter>());
        services.AddScoped<ICurrentOrganizationSetter, CurrentOrganizationSetter>(provider => provider.GetRequiredService<CurrentOrganizationSetter>());

        services.AddScoped<ICurrentTenant>(provider => provider.GetRequiredService<CurrentTenantSetter>().CurrentTenant);
        services.AddScoped<ICurrentOrganization>(provider => provider.GetRequiredService<CurrentOrganizationSetter>().CurrentOrganization);

        services = ConfigureServices(services);

        RootServiceProvider = services.BuildServiceProvider();
        ServiceScope = RootServiceProvider.CreateAsyncScope();

        await using var asyncServiceScope = RootServiceProvider.CreateAsyncScope();
        using var dbContext = asyncServiceScope.ServiceProvider.GetRequiredService<TestEFDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        _enableLogger = false;
        await LoadSeedDataAsync(dbContext);
        _enableLogger = true;
    }

    #endregion Public 方法

    #region Protected 方法

    protected void ChangeOrganization(ulong? id)
    {
        Service<ICurrentOrganizationSetter>().Set(id);
    }

    protected void ChangeTenant(ulong? id)
    {
        Service<ICurrentTenantSetter>().Set(id);
    }

    protected abstract IServiceCollection ConfigureServices(IServiceCollection services);

    protected TestEFDbContext GetTestEFDbContext() => Service<TestEFDbContext>();

    protected virtual async Task LoadSeedDataAsync(TestEFDbContext dbContext)
    {
        var userId = 30000;
        var articleId = 40000;

        List<User> users = new();
        List<Article> articles = new();

        for (ulong tenantId = 10001; tenantId < 10011; tenantId++)
        {
            for (ulong organizationId = 20001; organizationId < 20006; organizationId++)
            {
                for (int addUserIndex = 0; addUserIndex < 10; addUserIndex++)
                {
                    var user = new User() { Id = ++userId, Name = $"User-{userId}", IsDeleted = addUserIndex > 8, OrganizationId = organizationId, TenantId = tenantId };
                    users.Add(user);
                    dbContext.Users.Add(user);

                    for (int addArticleIndex = 0; addArticleIndex < 5; addArticleIndex++)
                    {
                        var article = new Article() { Id = ++articleId, Title = $"Article-{articleId}", Content = $"Article-{articleId}-Content", UserId = userId, IsDeleted = addArticleIndex > 3, OrganizationId = organizationId, TenantId = tenantId };
                        articles.Add(article);
                        dbContext.Articles.Add(article);
                    }
                }
            }
        }
        await dbContext.SaveChangesAsync(default);

        SeedData = (users, articles);
    }

    protected T Service<T>() where T : notnull => ScopeServiceProvider.GetRequiredService<T>();

    #endregion Protected 方法
}
