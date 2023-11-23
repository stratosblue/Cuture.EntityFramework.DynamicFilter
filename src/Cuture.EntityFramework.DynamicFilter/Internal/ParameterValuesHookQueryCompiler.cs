using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Extensions.Internal;

internal sealed class ParameterValuesHookQueryCompiler : QueryCompiler
{
    #region Private 字段

    private readonly ICurrentDbContext _currentContext;

    #endregion Private 字段

    #region Public 构造函数

    public ParameterValuesHookQueryCompiler(IQueryContextFactory queryContextFactory,
                                            ICompiledQueryCache compiledQueryCache,
                                            ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
                                            IDatabase database,
                                            IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                            ICurrentDbContext currentContext,
                                            IEvaluatableExpressionFilter evaluatableExpressionFilter,
                                            IModel model)
        : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, evaluatableExpressionFilter, model)
    {
        _currentContext = currentContext;
    }

    #endregion Public 构造函数

    #region Public 方法

    public override Expression ExtractParameters(Expression query,
                                                 IParameterValues parameterValues,
                                                 IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                                 bool parameterize = true,
                                                 bool generateContextAccessors = false)
    {
        if (_currentContext.Context.GetService<DynamicFilterQueryExpressionInterceptor>() is not { } interceptor)
        {
            throw new InvalidOperationException($"There is no \"{nameof(DynamicFilterQueryExpressionInterceptor)}\" found in current DbContext.");
        }

        query = base.ExtractParameters(query, parameterValues, logger, parameterize, generateContextAccessors);

        return interceptor.Resolve(query, parameterValues);
    }

    #endregion Public 方法
}
