using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class InlineQueryTest : SimpleQueryTestBase
{
    #region Public 方法

    [TestMethod]
    public async Task Should_Any_Success()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId)));

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId)));

            ChangeTenant(1);

            Assert.IsFalse(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId)));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId)));
        }
    }

    [TestMethod]
    public async Task Should_Count_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allArticleCount = await dbContext.Articles.IgnoreQueryFilters().CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId));

        Assert.AreEqual(SeedData.Articles.Count(), allArticleCount);

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            var count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId));
            Assert.AreEqual(SeedData.Articles.Count(m => SeedData.Users.Any(n => n.Id == m.UserId && !n.IsDeleted) && !m.IsDeleted), count);

            ChangeTenant(1);

            count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId));
            Assert.AreEqual(0, count);

            ChangeTenant(tenantId);

            count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId));
            Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId && SeedData.Users.Any(n => n.Id == m.UserId && n.TenantId == tenantId && !n.IsDeleted) && !m.IsDeleted), count);
        }
    }

    #endregion Public 方法
}
