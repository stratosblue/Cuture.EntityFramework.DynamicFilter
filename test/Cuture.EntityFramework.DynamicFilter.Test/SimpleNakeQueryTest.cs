using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class SimpleNakeQueryTest : SimpleQueryTestBase
{
    #region Public 方法

    [TestMethod]
    public async Task Should_Any_Success()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Users.AnyAsync());
        Assert.IsTrue(await dbContext.Articles.AnyAsync());

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId));
            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId));

            ChangeTenant(tenantId + 1);

            Assert.IsFalse(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId));
            Assert.IsFalse(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId));
            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId));
        }
    }

    [TestMethod]
    public async Task Should_Count_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUserCount = await dbContext.Users.IgnoreQueryFilters().CountAsync();
        var allArticleCount = await dbContext.Articles.IgnoreQueryFilters().CountAsync();

        Assert.AreEqual(SeedData.Users.Count(), allUserCount);
        Assert.AreEqual(SeedData.Articles.Count(), allArticleCount);

        var userCount = await dbContext.Users.CountAsync();
        var articleCount = await dbContext.Articles.CountAsync();

        Assert.AreEqual(SeedData.Users.Count(m => !m.IsDeleted), userCount);
        Assert.AreEqual(SeedData.Articles.Count(m => !m.IsDeleted), articleCount);
    }

    [TestMethod]
    public async Task Should_Dynamic_Query_Parameter_Success()
    {
        var dbContext = GetTestEFDbContext();

        foreach (var tenantGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = tenantGroup.Key;
            foreach (var organizationGroup in SeedData.Users.GroupBy(m => m.OrganizationId))
            {
                var organizationId = organizationGroup.Key;
                ChangeTenant(tenantId);
                ChangeOrganization(null);

                var users = await dbContext.Users.ToListAsync();
                var articles = await dbContext.Articles.ToListAsync();

                Assert.AreEqual(SeedData.Users.Count(m => m.TenantId == tenantId && !m.IsDeleted), users.Count);
                Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId && !m.IsDeleted), articles.Count);

                users = await dbContext.Users.IgnoreQueryFilter("SoftDeletion").ToListAsync();
                articles = await dbContext.Articles.IgnoreQueryFilter("SoftDeletion").ToListAsync();

                Assert.AreEqual(SeedData.Users.Count(m => m.TenantId == tenantId), users.Count);
                Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId), articles.Count);

                ChangeOrganization(organizationId);

                users = await dbContext.Users.ToListAsync();
                articles = await dbContext.Articles.ToListAsync();

                Assert.AreEqual(SeedData.Users.Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), users.Count);
                Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), articles.Count);

                users = await dbContext.Users.IgnoreQueryFilter("SoftDeletion").ToListAsync();
                articles = await dbContext.Articles.IgnoreQueryFilter("SoftDeletion").ToListAsync();

                Assert.AreEqual(SeedData.Users.Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), users.Count);
                Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), articles.Count);
            }
        }
    }

    [TestMethod]
    public async Task Should_EF_IgnoreQueryFilters_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.IgnoreQueryFilters().ToListAsync();
        var allArticles = await dbContext.Articles.IgnoreQueryFilters().ToListAsync();

        Assert.AreEqual(SeedData.Users.Count(), allUsers.Count);
        Assert.AreEqual(SeedData.Articles.Count(), allArticles.Count);

        var users = await dbContext.Users.ToListAsync();
        var articles = await dbContext.Articles.ToListAsync();

        Assert.AreEqual(SeedData.Users.Count(m => !m.IsDeleted), users.Count);
        Assert.AreEqual(SeedData.Articles.Count(m => !m.IsDeleted), articles.Count);
    }

    [TestMethod]
    public async Task Should_IgnoreQueryFilter_By_Name_Success()
    {
        var dbContext = GetTestEFDbContext();

        var users = await dbContext.Users.IgnoreQueryFilter("SoftDeletion").ToListAsync();
        var articles = await dbContext.Articles.IgnoreQueryFilter("SoftDeletion").ToListAsync();

        Assert.AreEqual(SeedData.Users.Count(), users.Count);
        Assert.AreEqual(SeedData.Articles.Count(), articles.Count);
    }

    [TestMethod]
    public async Task Should_OrderByDescending_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync();
        var allArticles = await dbContext.Articles.OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync();

        var seedUsers = SeedData.Users.OrderByDescending(m => m.Id).ToList();
        var seedArticles = SeedData.Articles.OrderByDescending(m => m.Id).ToList();

        Assert.AreEqual(seedUsers.Count(), allUsers.Count);
        Assert.AreEqual(seedArticles.Count(), allArticles.Count);

        for (int i = 0; i < seedUsers.Count; i++)
        {
            Assert.AreEqual(seedUsers[i].Id, allUsers[i].Id);
        }

        for (int i = 0; i < seedArticles.Count; i++)
        {
            Assert.AreEqual(seedArticles[i].Id, allArticles[i].Id);
        }

        var users = await dbContext.Users.OrderByDescending(m => m.Id).ToListAsync();
        var articles = await dbContext.Articles.OrderByDescending(m => m.Id).ToListAsync();

        seedUsers = SeedData.Users.Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();
        seedArticles = SeedData.Articles.Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();

        Assert.AreEqual(seedUsers.Count(), users.Count);
        Assert.AreEqual(seedArticles.Count(), articles.Count);

        for (int i = 0; i < seedUsers.Count; i++)
        {
            Assert.AreEqual(seedUsers[i].Id, users[i].Id);
        }

        for (int i = 0; i < seedArticles.Count; i++)
        {
            Assert.AreEqual(seedArticles[i].Id, articles[i].Id);
        }
    }

    #endregion Public 方法
}
