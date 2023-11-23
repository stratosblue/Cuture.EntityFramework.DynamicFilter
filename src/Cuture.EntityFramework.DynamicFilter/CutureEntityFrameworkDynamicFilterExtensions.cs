using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.EntityFrameworkCore.Extensions;

/// <summary>
///
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CutureEntityFrameworkDynamicFilterExtensions
{
    #region Public 方法

    /// <summary>
    /// 添加 EntityFrameworkDynamicFilter 支持
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddEntityFrameworkDynamicQueryFilter(this IServiceCollection services, Action<EntityFrameworkDynamicFilterOptions> setupAction)
    {
        services.TryAddScoped<DynamicFilterQueryExpressionInterceptor>();

        services.AddOptions<EntityFrameworkDynamicFilterOptions>()
                .Configure(setupAction);

        services.TryAddSingleton<DynamicQueryFilterFactoryContainer>();

        services.TryAddScoped<DynamicQueryFilterFactoryScopeContainer>();

        return services;
    }

    /// <summary>
    /// 使用 DynamicQuery
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static DbContextOptionsBuilder UseDynamicQueryFilter(this DbContextOptionsBuilder builder)
    {
        builder.ReplaceService<IQueryCompiler, ParameterValuesHookQueryCompiler>();

        return builder;
    }

    #endregion Public 方法
}
