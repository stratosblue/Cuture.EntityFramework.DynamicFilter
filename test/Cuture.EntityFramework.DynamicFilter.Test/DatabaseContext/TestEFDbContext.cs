using Microsoft.EntityFrameworkCore;

namespace Cuture.EntityFramework.DynamicFilter.Test.DatabaseContext;

public class TestEFDbContext : DbContext
{
    #region Public 属性

    public DbSet<Article> Articles { get; set; }

    public DbSet<User> Users { get; set; }

    #endregion Public 属性

    #region Public 构造函数

    public TestEFDbContext(DbContextOptions options) : base(options)
    {
    }

    public TestEFDbContext()
    {
    }

    #endregion Public 构造函数
}
