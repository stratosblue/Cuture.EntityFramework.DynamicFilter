#pragma warning disable CS9107

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

internal sealed class ParameterValuesHookQueryCompiler(IQueryContextFactory queryContextFactory,
                                                       ICompiledQueryCache compiledQueryCache,
                                                       ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
                                                       IDatabase database,
                                                       IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                                       ICurrentDbContext currentContext,
                                                       IEvaluatableExpressionFilter evaluatableExpressionFilter,
                                                       IModel model)
    : QueryCompiler(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, evaluatableExpressionFilter, model)
{
    #region Public 方法

#if NET10_0_OR_GREATER

    /// <inheritdoc/>
    public override Expression ExtractParameters(Expression query,
                                                 ParameterValues parameters,
                                                 IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                                 bool compiledQuery = false,
                                                 bool generateContextAccessors = false)
    {
        if (currentContext.Context.GetService<DynamicFilterQueryExpressionInterceptor>() is not { } interceptor)
        {
            throw new InvalidOperationException($"There is no \"{nameof(DynamicFilterQueryExpressionInterceptor)}\" found in current DbContext.");
        }

        query = base.ExtractParameters(query, parameters, logger, compiledQuery, generateContextAccessors);

        return interceptor.Resolve(query, parameters);
    }

#else

    /// <inheritdoc/>
    public override Expression ExtractParameters(Expression query,
                                                 ParameterValues parameterValues,
                                                 IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                                 bool parameterize = true,
                                                 bool generateContextAccessors = false)
    {
        if (currentContext.Context.GetService<DynamicFilterQueryExpressionInterceptor>() is not { } interceptor)
        {
            throw new InvalidOperationException($"There is no \"{nameof(DynamicFilterQueryExpressionInterceptor)}\" found in current DbContext.");
        }

        query = base.ExtractParameters(query, parameterValues, logger, parameterize, generateContextAccessors);

        return interceptor.Resolve(query, parameterValues);
    }

#endif

    #endregion Public 方法
}
