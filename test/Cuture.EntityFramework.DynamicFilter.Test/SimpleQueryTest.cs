using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class SimpleQueryTest : SimpleQueryTestBase
{
    #region Public 方法

    [TestMethod]
    public async Task Should_Any_Success()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync());
        Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync());

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId));
            Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId));

            ChangeTenant(tenantId + 1);

            Assert.IsFalse(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId));
            Assert.IsFalse(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId));
            Assert.IsTrue(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId));
        }
    }

    [TestMethod]
    public async Task Should_Count_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUserCount = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilters().CountAsync();
        var allArticleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilters().CountAsync();

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(), allUserCount);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), allArticleCount);

        var userCount = await dbContext.Users.Where(m => m.Name.Length > 1).CountAsync();
        var articleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).Where(m => m.Title.Length > 1).CountAsync();

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

                var users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync();
                var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync();

                Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && !m.IsDeleted), users.Count);
                Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && !m.IsDeleted), articles.Count);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();

                Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId), users.Count);
                Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId), articles.Count);

                ChangeOrganization(organizationId);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync();
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync();

                Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), users.Count);
                Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId && !m.IsDeleted), articles.Count);

                users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();
                articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();

                Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), users.Count);
                Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => m.TenantId == tenantId && m.OrganizationId == organizationId), articles.Count);
            }
        }
    }

    [TestMethod]
    public async Task Should_EF_IgnoreQueryFilters_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilters().ToListAsync();
        var allArticles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilters().ToListAsync();

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(), allUsers.Count);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), allArticles.Count);

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).ToListAsync();
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).ToListAsync();

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(m => !m.IsDeleted), users.Count);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(m => !m.IsDeleted), articles.Count);
    }

    [TestMethod]
    public async Task Should_IgnoreQueryFilter_By_Name_Success()
    {
        var dbContext = GetTestEFDbContext();

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).IgnoreQueryFilter("SoftDeletion").ToListAsync();

        Assert.AreEqual(SeedData.Users.Where(m => m.Name.Length > 1).Count(), users.Count);
        Assert.AreEqual(SeedData.Articles.Where(m => m.Title.Length > 1).Count(), articles.Count);
    }

    [TestMethod]
    public async Task Should_OrderByDescending_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allUsers = await dbContext.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync();
        var allArticles = await dbContext.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).IgnoreQueryFilters().ToListAsync();

        var seedUsers = SeedData.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).ToList();
        var seedArticles = SeedData.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).ToList();

        Assert.AreEqual(seedUsers.Count, allUsers.Count);
        Assert.AreEqual(seedArticles.Count, allArticles.Count);

        for (int i = 0; i < seedUsers.Count; i++)
        {
            Assert.AreEqual(seedUsers[i].Id, allUsers[i].Id);
        }

        for (int i = 0; i < seedArticles.Count; i++)
        {
            Assert.AreEqual(seedArticles[i].Id, allArticles[i].Id);
        }

        var users = await dbContext.Users.Where(m => m.Name.Length > 1).OrderByDescending(m => m.Id).ToListAsync();
        var articles = await dbContext.Articles.Where(m => m.Title.Length > 1).OrderByDescending(m => m.Id).ToListAsync();

        seedUsers = SeedData.Users.Where(m => m.Name.Length > 1).Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();
        seedArticles = SeedData.Articles.Where(m => m.Title.Length > 1).Where(m => !m.IsDeleted).OrderByDescending(m => m.Id).ToList();

        Assert.AreEqual(seedUsers.Count, users.Count);
        Assert.AreEqual(seedArticles.Count, articles.Count);

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
