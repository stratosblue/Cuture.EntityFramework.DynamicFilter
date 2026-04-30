using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class SimpleQueryTest : SimpleQueryTestBase
{
    #region Public 属性

    public TestContext TestContext { get; set; }

    #endregion Public 属性

    #region Public 方法

    [TestMethod]
    public async Task Should_Any_Success()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(TestContext.CancellationToken));
        Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(TestContext.CancellationToken));

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));

            ChangeTenant(tenantId + 1);

            Assert.IsFalse(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsFalse(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
        }
    }

    [TestMethod]
    public async Task Should_Any_Success_Root()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Users.AnyAsync(TestContext.CancellationToken));
        Assert.IsTrue(await dbContext.Articles.AnyAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Should_Any_With_Selected_Success()
    {
        var dbContext = GetTestEFDbContext();

        ChangeTenant(null);

        var value = await dbContext.Users.Where(m => m.Name.Length > 1)
                                         .Select(m => m.Name)
                                         .Where(m => !string.IsNullOrWhiteSpace(m))
                                         .AnyAsync(TestContext.CancellationToken);
        Assert.IsTrue(value);

        await dbContext.Users.ExecuteUpdateAsync(m => m.SetProperty(n => n.IsDeleted, true), TestContext.CancellationToken);

        value = await dbContext.Users.Where(m => m.Name.Length > 1)
                                     .Select(m => m.Name)
                                     .Where(m => !string.IsNullOrWhiteSpace(m))
                                     .AnyAsync(TestContext.CancellationToken);
        Assert.IsFalse(value);
    }

    [TestMethod]
    public async Task Should_Any_With_Selected_Success_Root()
    {
        var dbContext = GetTestEFDbContext();

        ChangeTenant(null);

        var value = await dbContext.Users.Select(m => m.Name)
                                         .Where(m => !string.IsNullOrWhiteSpace(m))
                                         .AnyAsync(TestContext.CancellationToken);
        Assert.IsTrue(value);

        await dbContext.Users.ExecuteUpdateAsync(m => m.SetProperty(n => n.IsDeleted, true), TestContext.CancellationToken);

        value = await dbContext.Users.Select(m => m.Name)
                                     .Where(m => !string.IsNullOrWhiteSpace(m))
                                     .AnyAsync(TestContext.CancellationToken);
        Assert.IsFalse(value);
    }

    [TestMethod]
    public async Task Should_Count_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUserCount = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilters().CountAsync(TestContext.CancellationToken);
        var allArticleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilters().CountAsync(TestContext.CancellationToken);

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(), allUserCount);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), allArticleCount);

        var userCount = await dbContext.Users.Where(m => m.Name.Length > 1).CountAsync(TestContext.CancellationToken);
        var articleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).Where(m => m.Title.Length > 1).CountAsync(TestContext.CancellationToken);

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => !m.IsDeleted), userCount);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => !m.IsDeleted), articleCount);
    }

    [TestMethod]
    public async Task Should_Dynamic_Query_Parameter_Success()
    {
        var dbContext = GetTestEFDbContext();

        foreach (var tenantGroup in SeedData.Users.Where(m => m.Name.Length > 1).GroupBy(m => m.TenantId))
        {
            var tenantId = tenantGroup.Key;
            foreach (var organizationGroup in SeedData.Users.Where(m => m.Name.Length > 1).GroupBy(m => m.OrganizationId))
            {
                var organizationId = organizationGroup.Key;
                ChangeTenant(tenantId);
                ChangeOrganization(null);

                var users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync(TestContext.CancellationToken);
                var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync(TestContext.CancellationToken);

                Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && !m.IsDeleted), users);
                Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && !m.IsDeleted), articles);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);

                Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId), users);
                Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId), articles);

                ChangeOrganization(organizationId);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync(TestContext.CancellationToken);
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync(TestContext.CancellationToken);

                Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), users);
                Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), articles);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);

                Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), users);
                Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), articles);
            }
        }
    }

    [TestMethod]
    public async Task Should_EF_IgnoreQueryFilters_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilters().ToListAsync(TestContext.CancellationToken);
        var allArticles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilters().ToListAsync(TestContext.CancellationToken);

        Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(), allUsers);
        Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), allArticles);

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync(TestContext.CancellationToken);
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync(TestContext.CancellationToken);

        Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => !m.IsDeleted), users);
        Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => !m.IsDeleted), articles);
    }

    [TestMethod]
    public async Task Should_IgnoreQueryFilter_By_Name_Success()
    {
        var dbContext = GetTestEFDbContext();

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync(TestContext.CancellationToken);

        Assert.HasCount(SeedData.Users.Where(m => m.Name.Length > 1).Count(), users);
        Assert.HasCount(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), articles);
    }

    [TestMethod]
    public async Task Should_OrderByDescending_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync(TestContext.CancellationToken);
        var allArticles = await dbContext.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync(TestContext.CancellationToken);

        var seedUsers = SeedData.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).ToList();
        var seedArticles = SeedData.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).ToList();

        Assert.HasCount(seedUsers.Count, allUsers);
        Assert.HasCount(seedArticles.Count, allArticles);

        for (int i = 0; i < seedUsers.Count; i++)
        {
            Assert.AreEqual(seedUsers[i].Id, allUsers[i].Id);
        }

        for (int i = 0; i < seedArticles.Count; i++)
        {
            Assert.AreEqual(seedArticles[i].Id, allArticles[i].Id);
        }

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).ToListAsync(TestContext.CancellationToken);
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).ToListAsync(TestContext.CancellationToken);

        seedUsers = SeedData.Users.Where(m => m.Name.Length > 1).Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();
        seedArticles = SeedData.Articles.Where(m => m.Title.Length > 1).Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();

        Assert.HasCount(seedUsers.Count, users);
        Assert.HasCount(seedArticles.Count, articles);

        for (int i = 0; i < seedUsers.Count; i++)
        {
            Assert.AreEqual(seedUsers[i].Id, users[i].Id);
        }

        for (int i = 0; i < seedArticles.Count; i++)
        {
            Assert.AreEqual(seedArticles[i].Id, articles[i].Id);
        }
    }

    [TestMethod]
    public async Task Should_Selected_Success()
    {
        var dbContext = GetTestEFDbContext();

        ChangeTenant(null);

        var items = await dbContext.Users.Where(m => m.Name.Length > 1)
                                         .Select(m => new
                                         {
                                             m.Name,
                                             m.IsDeleted,
                                         })
                                         .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                                         .ToListAsync(TestContext.CancellationToken);

        Assert.IsNotEmpty(items);
        Assert.IsTrue(items.All(m => m.IsDeleted == false));

        //TODO 支持ExecuteUpdate ExecuteDelete
        await dbContext.Users.ExecuteUpdateAsync(m => m.SetProperty(n => n.IsDeleted, true), TestContext.CancellationToken);

        items = await dbContext.Users.Where(m => m.Name.Length > 1)
                                     .Select(m => new
                                     {
                                         m.Name,
                                         m.IsDeleted,
                                     })
                                     .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                                     .ToListAsync(TestContext.CancellationToken);
        Assert.IsEmpty(items);
    }

    [TestMethod]
    public async Task Should_Selected_Success_Root()
    {
        var dbContext = GetTestEFDbContext();

        ChangeTenant(null);

        var items = await dbContext.Users.Select(m => new
        {
            m.Name,
            m.IsDeleted,
        })
        .Where(m => !string.IsNullOrWhiteSpace(m.Name))
        .ToListAsync(TestContext.CancellationToken);

        Assert.IsNotEmpty(items);
        Assert.IsTrue(items.All(m => m.IsDeleted == false));

        //TODO 支持ExecuteUpdate ExecuteDelete
        await dbContext.Users.ExecuteUpdateAsync(m => m.SetProperty(n => n.IsDeleted, true), TestContext.CancellationToken);

        items = await dbContext.Users.Select(m => new
        {
            m.Name,
            m.IsDeleted,
        })
        .Where(m => !string.IsNullOrWhiteSpace(m.Name))
        .ToListAsync(TestContext.CancellationToken);
        Assert.IsEmpty(items);
    }

    [TestMethod]
    public async Task Should_SkipTake_Success()
    {
        var dbContext = GetTestEFDbContext();
        var items = await dbContext.Articles.OrderBy(m => m.Id).Skip(10).Take(10).ToListAsync(TestContext.CancellationToken);
        Assert.HasCount(10, items);
        var items2 = await dbContext.Articles.OrderBy(m => m.Id).Skip(0).Take(20).ToListAsync(TestContext.CancellationToken);
        CollectionAssert.AreEqual(items2.Skip(10).Take(10).ToList(), items);
    }

    #endregion Public 方法
}
