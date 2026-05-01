using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class ExecuteUpdateTest : SimpleQueryTestBase
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

            //ExecuteUpdate软删除测试
            var userCount = await dbContext.Users.Where(m => m.Name.Length > 1).CountAsync(m => m.TenantId == tenantId, TestContext.CancellationToken);
            var articleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).CountAsync(m => m.TenantId == tenantId, TestContext.CancellationToken);

            var deleteUserCount = await dbContext.Users.Where(m => m.Name.Length > 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), TestContext.CancellationToken);
            var deleteArticleCount = await dbContext.Articles.Where(m => m.Title.Length > 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), TestContext.CancellationToken);

            Assert.AreEqual(userCount, deleteUserCount);
            Assert.AreEqual(articleCount, deleteArticleCount);

            Assert.IsFalse(await dbContext.Users.Where(m => m.Name.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsFalse(await dbContext.Articles.Where(m => m.Title.Length > 1).AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
        }

        var count = await dbContext.Users.IgnoreQueryFilters().CountAsync(TestContext.CancellationToken);
        var softDeletedCount = await dbContext.Users.IgnoreQueryFilters().Where(m => m.IsDeleted).CountAsync(TestContext.CancellationToken);

        Assert.AreEqual(count, softDeletedCount);
    }

    [TestMethod]
    public async Task Should_Any_Success_Root()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Users.AnyAsync(TestContext.CancellationToken));
        Assert.IsTrue(await dbContext.Articles.AnyAsync(TestContext.CancellationToken));

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));

            ChangeTenant(tenantId + 1);

            Assert.IsFalse(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsFalse(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));

            //ExecuteUpdate软删除测试
            var userCount = await dbContext.Users.CountAsync(m => m.TenantId == tenantId, TestContext.CancellationToken);
            var articleCount = await dbContext.Articles.CountAsync(m => m.TenantId == tenantId, TestContext.CancellationToken);

            var deleteUserCount = await dbContext.Users.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), TestContext.CancellationToken);
            var deleteArticleCount = await dbContext.Articles.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), TestContext.CancellationToken);

            Assert.AreEqual(userCount, deleteUserCount);
            Assert.AreEqual(articleCount, deleteArticleCount);

            Assert.IsFalse(await dbContext.Users.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
            Assert.IsFalse(await dbContext.Articles.AnyAsync(m => m.TenantId == tenantId, TestContext.CancellationToken));
        }

        var count = await dbContext.Users.IgnoreQueryFilters().CountAsync(TestContext.CancellationToken);
        var softDeletedCount = await dbContext.Users.IgnoreQueryFilters().Where(m => m.IsDeleted).CountAsync(TestContext.CancellationToken);

        Assert.AreEqual(count, softDeletedCount);
    }

    #endregion Public 方法
}
