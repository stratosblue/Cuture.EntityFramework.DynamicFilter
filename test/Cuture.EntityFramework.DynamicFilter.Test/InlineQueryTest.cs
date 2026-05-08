using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class InlineQueryTest : SimpleQueryTestBase
{
    #region Public 属性

    public TestContext TestContext { get; set; }

    #endregion Public 属性

    #region Public 方法

    [TestMethod]
    public async Task Should_Any_Success()
    {
        var dbContext = GetTestEFDbContext();

        Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken));

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken));

            ChangeTenant(1);

            Assert.IsFalse(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken));

            ChangeTenant(tenantId);

            Assert.IsTrue(await dbContext.Articles.AnyAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken));
        }
    }

    [TestMethod]
    public async Task Should_Count_Success()
    {
        var dbContext = GetTestEFDbContext();

        var allArticleCount = await dbContext.Articles.IgnoreQueryFilters().CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken);

        Assert.AreEqual(SeedData.Articles.Count(), allArticleCount);

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            var count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken);
            Assert.AreEqual(SeedData.Articles.Count(m => SeedData.Users.Any(n => n.Id == m.UserId && !n.IsDeleted) && !m.IsDeleted), count);

            ChangeTenant(1);

            count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken);
            Assert.AreEqual(0, count);

            ChangeTenant(tenantId);

            count = await dbContext.Articles.CountAsync(m => dbContext.Users.Any(n => n.Id == m.UserId), TestContext.CancellationToken);
            Assert.AreEqual(SeedData.Articles.Count(m => m.TenantId == tenantId && SeedData.Users.Any(n => n.Id == m.UserId && n.TenantId == tenantId && !n.IsDeleted) && !m.IsDeleted), count);
        }
    }

    [TestMethod]
    public async Task Should_GroupOrdered_Select_Inline_Sum_Success()
    {
        var dbContext = GetTestEFDbContext();

        var notDeletedSum = await dbContext.Articles.IgnoreQueryFilters()
                                                    .Where(m => !m.IsDeleted && !dbContext.Users.Any(n => n.Id == m.UserId && n.IsDeleted))
                                                    .SumAsync(m => m.Title.Length, TestContext.CancellationToken);
        Assert.AreNotEqual(notExpected: notDeletedSum,
                           actual: await dbContext.Articles.IgnoreQueryFilters().SumAsync(m => m.Title.Length, TestContext.CancellationToken));

        foreach (var userGroup in SeedData.Users.GroupBy(m => m.TenantId))
        {
            var tenantId = userGroup.Key;

            ChangeTenant(null);

            Assert.AreEqual(notDeletedSum, await GetTitleLengthSumAsync(dbContext));

            ChangeTenant(tenantId);

            var currentValue = await GetTitleLengthSumAsync(dbContext);

            Assert.AreNotEqual(notDeletedSum, currentValue);

            var currentNotDeletedSum = await dbContext.Articles.IgnoreQueryFilters()
                                                               .Where(m => m.TenantId == tenantId && !m.IsDeleted && !dbContext.Users.Any(n => n.Id == m.UserId && n.IsDeleted))
                                                               .SumAsync(m => m.Title.Length, TestContext.CancellationToken);

            Assert.AreEqual(currentNotDeletedSum, currentValue);
        }

        async Task<int> GetTitleLengthSumAsync(TestEFDbContext dbContext)
        {
            var query = dbContext.Users.Where(m => m.Id > -1)
                                       .GroupBy(m => m.Id)
                                       .OrderByDescending(g => g.Max(m => m.CreateTime));

            var typeItems = await query.Select(m => new ArticleInfo(m.Key, dbContext.Articles.Where(n => n.UserId == m.Key).Sum(n => n.Title.Length)))
                                       .ToListAsync(TestContext.CancellationToken);

            var anonymousItems = await query.Select(m => new
            {
                m.Key,
                Count = dbContext.Articles.Where(n => n.UserId == m.Key).Sum(n => n.Title.Length)
            })
            .ToListAsync(TestContext.CancellationToken);

            var typeSum = typeItems.Sum(n => n.TitleLength);
            var anonymousSum = anonymousItems.Sum(n => n.Count);

            Assert.AreEqual(typeSum, anonymousSum);
            Assert.AreNotEqual(0, typeSum);

            return typeSum;
        }
    }

    private record ArticleInfo(int UserId, int TitleLength);

    #endregion Public 方法
}
