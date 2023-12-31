# Cuture.EntityFramework.DynamicFilter
## 1. Intro

An extension library for `EntityFrameworkCore` to support dynamic global filters. 一个 `EntityFrameworkCore` 的拓展库，用于支持动态的全局过滤器。

 - 动态全局过滤器;
 - 支持自由组合多个过滤器;
 - 全局过滤器支持排序与位置设置（针对联合索引场景，`EntityFrameworkCore` 的 `QueryFilter` 只能附加到查询头部，对联合索引不友好）;
 - 支持查询时忽略部分过滤器;
 - 目标框架 `net7.0`+;

### NOTE!!!
 - 会替换掉 `EntityFrameworkCore` 的 `IQueryCompiler`, 可能与其它通过此逻辑实现功能的库冲突；
 - 与 `EntityFrameworkCore` 的 `QueryFilter` 属于不同的实现，能够共存，内部实现了对 `EntityFrameworkCore` 的 `IgnoreQueryFilters` 的支持，但只针对单个子查询；
 
## 2. 快速开始

 - 任何已引用 `EntityFrameworkCore` 的项目, 引用包:
```xml
<ItemGroup>
  <PackageReference Include="Cuture.EntityFramework.DynamicFilter" Version="1.0.0-*" />
</ItemGroup>
```

 - 配置 `DbContext` 时启用 `DynamicQueryFilter`
```C#
services.AddDbContext<XXXXContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder options) =>
{
    //Other options
    options.UseDynamicQueryFilter();
});
```

 - 向 `DI` 容器注册并配置全局过滤器
```C#
services.AddEntityFrameworkDynamicQueryFilter(options =>
{
    // 配置实体 User 的过滤器
    options.Entity<User>(builder =>
    {
        //添加头部表达式过滤器，表达式过滤器根据查询时 ServiceProvider 中的服务状态动态构造，表达式将在查询 User 时拼接到查询头部
        builder.AddHeadFilter(provider =>
        {
            var currentTenant = provider.GetRequiredService<ICurrentTenant>();
            return currentTenant.Id is null
                    ? n => true
                    : n => n.TenantId == currentTenant.Id;
        });

        //添加过滤器 MyQueryFilter（从DI容器中获取），其名称、位置、排序由 MyQueryFilter 内部确定
        builder.AddFilter<MyQueryFilter>();

        //添加尾部表达式过滤器，其名称为 SoftDeletion，表达式将在查询 User 时拼接到查询末尾
        builder.AddTailFilter("SoftDeletion", n => n.IsDeleted == false);

        //Other options
    });
});
```

至此已完成所有配置，在使用对应的 `DbContext` 查询时将会自动应用声明的查询过滤器

-------

## 3. 查询时禁用全局过滤器

 - 使用 `EntityFrameworkCore` 的 `IgnoreQueryFilters` 禁用过滤器
```
var query = context.Users.IgnoreQueryFilters();
```

 - 根据名称禁用过滤器
```
var query = context.Users.IgnoreQueryFilter("SoftDeletion");
```

 - 根据过滤器类型禁用过滤器
```
var query = context.Users.IgnoreQueryFilter(typeof(MyClassQueryFilter));
```

## 4. 实现基于类型的过滤器

实现 `IDynamicQueryFilter<T>` 接口，或直接从 `DynamicQueryFilter<T>`派生；

## 未完待续。。。
