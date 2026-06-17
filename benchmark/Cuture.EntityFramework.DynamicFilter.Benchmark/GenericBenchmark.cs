using BenchmarkDotNet.Attributes;
using Cuture.EntityFramework.DynamicFilter.Test.DatabaseContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cuture.EntityFramework.DynamicFilter.Benchmark;

[SimpleJob]
[MemoryDiagnoser]
public class GenericBenchmark
{
    #region Private 字段

    private TestEFDbContext _dynamicDbContext = null!;

    private TestEFDbContext _originDbContext = null!;

    private readonly List<object> _references = [];

    #endregion Private 字段

    #region Public 方法

    [Benchmark]
    public void DynamicQuery()
    {
        var users = _dynamicDbContext.Users.Where(m => m.TenantId == 1)
                                           .ToListAsync();
    }

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        _originDbContext = await CreateDbContextAsync(null, null);

        _dynamicDbContext = await CreateDbContextAsync(services =>
        {
            services.AddEntityFrameworkDynamicQueryFilter(options =>
            {
                options.Entity<User>(builder =>
                {
                    builder.AddTailFilter("IsDeleted", m => !m.IsDeleted);
                });
            });
        }, builder =>
        {
            builder.UseDynamicQueryFilter();
        });

        var users1 = await _originDbContext.Users.Where(m => m.TenantId == 1)
                                                 .Where(m => !m.IsDeleted)
                                                 .OrderBy(m => m.Id)
                                                 .ToListAsync();

        var users2 = await _originDbContext.Users.Where(m => m.TenantId == 1)
                                                 .OrderBy(m => m.Id)
                                                 .ToListAsync();

        if (!users1.Zip(users2).ToList().TrueForAll((m) => m.First.Equals(m.Second)))
        {
            throw new InvalidOperationException("测试数据查询不一致");
        }

        async Task<TestEFDbContext> CreateDbContextAsync(Action<IServiceCollection>? servicesAction, Action<DbContextOptionsBuilder>? optionsAction)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            IServiceCollection services = new ServiceCollection();

            services.AddLogging(builder => builder.ClearProviders());
            services.AddSqlite<TestEFDbContext>(connectionString: null,
                                                sqliteOptionsAction: null,
                                                optionsAction: builder =>
                                                {
                                                    builder.UseSqlite(connection);
                                                    optionsAction?.Invoke(builder);
                                                });

            servicesAction?.Invoke(services);

            var provider = services.BuildServiceProvider();

            _references.Add(provider);
            var scope = provider.CreateAsyncScope();
            _references.Add(scope);
            var dbContext = scope.ServiceProvider.GetRequiredService<TestEFDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Users.AddRange([new User
            {
                Id = 1,
                Name = "User1",
                TenantId = 1,
                IsDeleted = false,
                CreateTime = DateTime.MinValue,
            }, new User
            {
                Id = 2,
                Name = "User2",
                TenantId = 1,
                IsDeleted = true,
                CreateTime = DateTime.MinValue,
            }, new User
            {
                Id = 3,
                Name = "User3",
                TenantId = 2,
                IsDeleted = false,
                CreateTime = DateTime.MinValue,
            }]);

            return dbContext;
        }
    }

    [Benchmark]
    public void ManualQuery()
    {
        var users = _originDbContext.Users.Where(m => m.TenantId == 1)
                                          .Where(m => !m.IsDeleted)
                                          .ToListAsync();
    }

    #endregion Public 方法
}
