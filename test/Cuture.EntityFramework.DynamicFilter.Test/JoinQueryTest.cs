using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test;

[TestClass]
public class JoinQueryTest : SimpleQueryTestBase
{
    #region Public 属性

    public TestContext TestContext { get; set; }

    #endregion Public 属性

    #region Public 方法

    [TestMethod]
    public async Task Should_Query_Success()
    {
        var dbContext = GetTestEFDbContext();

        await dbContext.Articles.ExecuteUpdateAsync(m => m.SetProperty(n => n.IsDeleted, true), TestContext.CancellationToken);

        var value = await dbContext.Articles.Join(dbContext.Users,
                                                  article => article.UserId,
                                                  user => user.Id,
                                                  (article, user) => new
                                                  {
                                                      Article = article,
                                                      UserId = user.Id,
                                                      UserName = user.Name,
                                                  })
                                            .Where(m => m.UserName.Length > 2)
                                            .ToListAsync(TestContext.CancellationToken);

        Assert.IsEmpty(value);
    }

    #endregion Public 方法
}
