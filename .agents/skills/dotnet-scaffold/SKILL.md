# .NET Clean Architecture Scaffold Skill

---
name: dotnet-scaffold
description: >
  Scaffolds a complete .NET 10 Clean Architecture solution matching the Hmz.Kolafi project
  structure — including domain, use-cases, infrastructure, web (FastEndpoints + Scalar),
  .NET Aspire, and four test projects. Applies DDD, CQRS, Domain Events, Specification
  Pattern, EF Core, Mediator source generator, Serilog logging, Vogen value objects, and
  Ardalis packages exactly as used in the example project.
---

## Overview

This skill creates a production-ready .NET 10 solution with **Clean Architecture** / **DDD**
conventions copied exactly from the `Hmz.Kolafi` reference project.

### Architecture Applied
| Pattern | Implementation |
|---------|---------------|
| Clean Architecture | Core → UseCases → Infrastructure → Web layers |
| DDD | Aggregate roots, value objects (Vogen), SmartEnum, domain events |
| CQRS | Mediator source-generator (`ICommand<T>` / `INotification`) |
| Domain Events | `DomainEventBase`, `EventDispatchInterceptor`, Mediator `INotificationHandler` |
| Specification Pattern | `Ardalis.Specification` + `RepositoryBase<T>` |
| EF Core | `AppDbContext`, `IEntityTypeConfiguration`, Vogen converters, `SaveChangesInterceptor` |
| FastEndpoints | `Endpoint<TReq, TRes>`, `Validator<T>`, per-endpoint Summary/Description/Tags |
| Scalar API Docs | `MapScalarApiReference()` wired via `UseSwaggerGen` |
| Logging | Serilog + OpenTelemetry (Aspire ServiceDefaults) |
| .NET Aspire | AppHost + ServiceDefaults; SQL Server via Aspire orchestration |
| Testing | xUnit + Shouldly + NSubstitute + Testcontainers + coverlet |

---

## Prerequisites

- .NET 10 SDK (`dotnet --version` ≥ 10.0.100)
- Docker Desktop (for Testcontainers / Aspire SQL Server)

---

## Instructions

When asked to scaffold a new project or when the user provides a **company prefix** and
**project name** (e.g., `Acme.MyApp`), perform the following steps **exactly in order**.

### Step 0 — Collect Parameters

Ask the user (or use defaults) for:

| Variable | Example | Description |
|----------|---------|-------------|
| `$PREFIX` | `Hmz` | Company/namespace prefix |
| `$PROJECT` | `Kolafi` | Project/product name |
| `$ROOT` | current dir | Root directory for the solution |

All further steps substitute `{PREFIX}.{PROJECT}` wherever `Hmz.Kolafi` appears in the
reference project.

---

### Step 1 — Run the PowerShell scaffold script

Execute the script at `scripts/scaffold.ps1` (in this skill folder) with:

```powershell
.\scripts\scaffold.ps1 -Prefix "{PREFIX}" -Project "{PROJECT}" -Root "{ROOT}"
```

This script:
1. Creates folder structure
2. Creates all `.csproj` files with correct NuGet references
3. Creates solution file (`.slnx`)
4. Creates `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config`
5. Creates all source code stub files per the architecture patterns below
6. Creates test project stubs

---

### Step 2 — Architecture Code Patterns to Apply

After running the script, ensure every generated file matches the patterns below.

#### 2a. Domain Layer (`{PREFIX}.{PROJECT}.Core`)

**Entity Base Class Hierarchy** (SharedKernel):

The project provides a layered entity hierarchy for cross-cutting concerns:

| Base Class | Provides | When to use |
|------------|----------|-------------|
| `EntityBase<T, TId>` (Ardalis) | ID, domain events | No auditing needed |
| `AuditableEntity<T, TId>` | + CreatedAt/By, ModifiedAt/By | Audit trail only |
| `FullAuditableEntity<T, TId>` | + IsDeleted, DeletedAt/By, RowVersion | Most entities (recommended) |

```csharp
// SharedKernel/AuditableEntity.cs
public abstract class AuditableEntity<TEntity, TId>
  : EntityBase<TEntity, TId>, IAuditable
  where TEntity : AuditableEntity<TEntity, TId>
  where TId : struct, IEquatable<TId>
{
  public DateTimeOffset CreatedAt { get; private set; }
  public string? CreatedBy { get; private set; }
  public DateTimeOffset ModifiedAt { get; private set; }
  public string? ModifiedBy { get; private set; }
  public void SetCreated(DateTimeOffset at, string? by) { ... }
  public void SetModified(DateTimeOffset at, string? by) { ... }
}

// SharedKernel/FullAuditableEntity.cs
public abstract class FullAuditableEntity<TEntity, TId>
  : AuditableEntity<TEntity, TId>, ISoftDeletable, IHasRowVersion
  where TEntity : FullAuditableEntity<TEntity, TId>
  where TId : struct, IEquatable<TId>
{
  public bool IsDeleted { get; private set; }
  public DateTimeOffset? DeletedAt { get; private set; }
  public string? DeletedBy { get; private set; }
  public uint RowVersion { get; private set; } // PostgreSQL xmin
  public void SoftDelete(DateTimeOffset at, string? by) { ... }
  public void Restore() { ... }
}
```

**Aggregate Root** (one per domain concept, inherits from FullAuditableEntity):
```csharp
// {Entity}.cs in {Entity}Aggregate/
public class {Entity}(/* value-object params */) 
    : FullAuditableEntity<{Entity}, {Entity}Id>, IAggregateRoot
{
    // Private setters – immutable by default
    // Methods mutate state and RegisterDomainEvent(new XyzEvent(this))
}
```

**Strongly-Typed ID** (Vogen):
```csharp
[ValueObject<int>]
public readonly partial struct {Entity}Id
{
    private static Validation Validate(int value)
        => value > 0 ? Validation.Ok : Validation.Invalid("{Entity}Id must be positive.");
}
```

**Value Object** (Vogen or record):
```csharp
[ValueObject<string>]
public readonly partial struct {Property}Name { /* max-length validation */ }
```

**SmartEnum** (for enumerations):
```csharp
public class {Entity}Status : SmartEnum<{Entity}Status>
{
    public static readonly {Entity}Status Active = new(nameof(Active), 1);
    protected {Entity}Status(string name, int value) : base(name, value) { }
}
```

**Domain Event**:
```csharp
// Events/{EntityPropertyName}Event.cs
public sealed class {Entity}NameUpdatedEvent({Entity} entity) : DomainEventBase
{
    public {Entity} {Entity} { get; init; } = entity;
}
```

**Domain Event Handler** (inside Core):
```csharp
// Handlers/{EventName}Handler.cs
public class {EventName}Handler(ILogger<{EventName}Handler> logger, IEmailSender emailSender)
    : INotificationHandler<{EventName}>
{
    public async ValueTask Handle({EventName} domainEvent, CancellationToken ct)
    {
        logger.LogInformation("Handling {Event}", nameof({EventName}));
        await emailSender.SendEmailAsync(/* ... */);
    }
}
```

**Specification**:
```csharp
// Specifications/{Entity}ByIdSpec.cs
public class {Entity}ByIdSpec : Specification<{Entity}>
{
    public {Entity}ByIdSpec({Entity}Id id) =>
        Query.Where(x => x.Id == id);
}
```

**Domain Service** (optional, for cross-aggregate or complex logic):
```csharp
// Services/Delete{Entity}Service.cs
public class Delete{Entity}Service(IRepository<{Entity}> repo, IMediator mediator, ILogger<...> logger)
    : IDelete{Entity}Service
{
    public async ValueTask<Result> Delete{Entity}({Entity}Id id) { ... }
}
```

**GlobalUsings.cs** (Core):
```csharp
global using Ardalis.GuardClauses;
global using Ardalis.Result;
global using Ardalis.SharedKernel;
global using Ardalis.SmartEnum;
global using Ardalis.Specification;
global using Mediator;
global using Microsoft.Extensions.Logging;
```

---

#### 2b. Use Cases Layer (`{PREFIX}.{PROJECT}.UseCases`)

One subfolder per entity. Inside each: Create / Get / List / Update / Delete subfolders.

**Command** (CQRS write):
```csharp
// {Entity}/Create/Create{Entity}Command.cs
public record Create{Entity}Command({Entity}Name Name, string? PhoneNumber)
    : ICommand<Result<{Entity}Id>>;
```

**Query** (CQRS read):
```csharp
// {Entity}/List/List{Entity}sQuery.cs
public record List{Entity}sQuery(int? Skip, int? Take) : IQuery<Result<IEnumerable<{Entity}DTO>>>;
```

**Handler**:
```csharp
public class Create{Entity}Handler(IRepository<{Entity}> _repository)
    : ICommandHandler<Create{Entity}Command, Result<{Entity}Id>>
{
    public async ValueTask<Result<{Entity}Id>> Handle(Create{Entity}Command cmd, CancellationToken ct)
    {
        var entity = new {Entity}(cmd.Name);
        var created = await _repository.AddAsync(entity, ct);
        return created.Id;
    }
}
```

**DTO**:
> **Note on DTO Placement**: If DTOs (Data Transfer Objects) are used across multiple layers, they MUST be defined in the `{PREFIX}.{PROJECT}.Core` project (inward). Otherwise, they should be placed within the specific project layer (e.g., `UseCases` or `Web`) so that outward layers can refer to them. Every common/cross-concern usage should be defined inward.

```csharp
public record {Entity}DTO(int Id, string Name);
```

**Utility classes** (project root):
```csharp
// Constants.cs
namespace {PREFIX}.{PROJECT}.UseCases;
public static class Constants
{
    public const int DEFAULT_PAGE_SIZE = 10;
}

// PagedResult.cs
namespace {PREFIX}.{PROJECT}.UseCases;
public record PagedResult<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize);
```

**GlobalUsings.cs** (UseCases):
```csharp
global using Ardalis.Result;
global using Ardalis.SharedKernel;
global using Mediator;
```

---

#### 2c. Infrastructure Layer (`{PREFIX}.{PROJECT}.Infrastructure`)

**AppDbContext**:
```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<{Entity}> {Entity}s => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
```

**EfRepository** (Ardalis.Specification):
```csharp
public class EfRepository<T>(AppDbContext dbContext)
    : RepositoryBase<T>(dbContext), IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot { }
```

**Entity Type Configuration** (Vogen converters + value generators):
```csharp
public class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.Property(x => x.Id)
            .HasValueGenerator<VogenIdValueGenerator<AppDbContext, {Entity}, {Entity}Id>>()
            .HasVogenConversion().IsRequired();

        builder.Property(x => x.Name)
            .HasVogenConversion().HasMaxLength({Entity}Name.MaxLength).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(x => x.Value, x => {Entity}Status.FromValue(x));
    }
}
```

**EventDispatchInterceptor** (Domain Events via SaveChanges):
```csharp
public class EventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data,
        int result, CancellationToken ct = default)
    {
        var context = data.Context;
        if (context is AppDbContext appDbContext)
        {
            var entities = appDbContext.ChangeTracker.Entries<HasDomainEventsBase>()
                .Select(e => e.Entity).Where(e => e.DomainEvents.Any()).ToArray();
            await dispatcher.DispatchAndClearEvents(entities);
        }
        return await base.SavedChangesAsync(data, result, ct);
    }
}
```

**InfrastructureServiceExtensions**:
```csharp
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, ConfigurationManager config, ILogger logger)
  {
    // ── Shared infrastructure (always registered) ──────────────────────
    string? conn = config.GetConnectionString("cleanarchitecture")
                ?? config.GetConnectionString("DefaultConnection")
                ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(conn);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<AuditableInterceptor>();
    services.AddScoped<SoftDeleteInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) => { /* DB setup */ });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
            .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
    services.AddHybridCache();

    // Cross-cutting auth infrastructure (Logto) — always registered
    services.Configure<LogtoConfiguration>(config.GetSection(LogtoConfiguration.SectionName));
    services.AddScoped<ILogtoUserService, LogtoUserService>();

    // ── Business modules ──────────────────────────────────────────────
    AddContributorsModule(services, config);
    AddUsersModule(services, config);

    logger.LogInformation("{Project} services registered", "Infrastructure");
    return services;
  }

  // Each module method contains ONLY business-domain services.
  private static void AddContributorsModule(IServiceCollection services, ConfigurationManager config)
  {
    services.AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
            .AddScoped<IDeleteContributorService, DeleteContributorService>();
  }

  private static void AddUsersModule(IServiceCollection services, ConfigurationManager config)
  {
    services.AddScoped<ICachedUserProfileService, CachedUserProfileService>();
  }
}
```

**GlobalUsings.cs** (Infrastructure):
```csharp
global using System.Net.Mail;
global using System.Reflection;
global using Ardalis.GuardClauses;
global using Ardalis.SharedKernel;
global using Ardalis.Specification.EntityFrameworkCore;
global using MailKit.Net.Smtp;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using MimeKit;
```

---

#### 2d. Web Layer (`{PREFIX}.{PROJECT}.Web`)

**Program.cs**:
```csharp
using {PREFIX}.{PROJECT}.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()   // OpenTelemetry via Aspire
       .AddLoggerConfigs();    // Serilog console

using var loggerFactory = LoggerFactory.Create(c => c.AddConsole());
var startupLogger = loggerFactory.CreateLogger<Program>();

startupLogger.LogInformation("Starting web host");

builder.Services.AddOptionConfigs(builder.Configuration, startupLogger, builder);
builder.Services.AddServiceConfigs(startupLogger, builder);
builder.Services.AddAuthenticationConfigs(builder.Configuration, startupLogger);

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o => { o.ShortSchemaNames = true; });

var app = builder.Build();

await app.UseAppMiddlewareAndSeedDatabase();

app.MapDefaultEndpoints(); // Aspire health checks

app.Run();

public partial class Program { }
```

**Configurations/LoggerConfigs.cs**:
```csharp
public static WebApplicationBuilder AddLoggerConfigs(this WebApplicationBuilder builder)
{
    builder.Logging.AddSerilog(new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
        .WriteTo.Console()
        .CreateLogger());
    return builder;
}
```

**Configurations/MiddlewareConfig.cs**:
```csharp
public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
{
    if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }
    else { app.UseDefaultExceptionHandler(); app.UseHsts(); }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFastEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerGen(o => { o.Path = "/openapi/{documentName}.json"; });
        app.MapScalarApiReference(); // Scalar API docs
    }

    var shouldMigrate = app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
    if (shouldMigrate) { /* MigrateDatabaseAsync + SeedDatabaseAsync */ }

    return app;
}
```

**Configurations/MediatorConfig.cs** (Mediator source-generator pipeline):
```csharp
public static IServiceCollection AddMediatorSourceGen(
    this IServiceCollection services, ILogger logger)
{
    logger.LogInformation("Registering Mediator SourceGen and Behaviors");
    services.AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.Assemblies =
        [
            typeof({Entity}),                     // Core
            typeof(Create{Entity}Command),        // UseCases
            typeof(InfrastructureServiceExtensions), // Infrastructure
            typeof(MediatorConfig)               // Web
        ];
        options.PipelineBehaviors = [ typeof(LoggingBehavior<,>) ];
    });
    return services;
}
```

**FastEndpoint** (one file per endpoint, example for Create):
```csharp
public class Create(IMediator mediator)
    : Endpoint<CreateRequest,
              Results<Created<CreateResponse>, ValidationProblem, ProblemHttpResult>>
{
    private readonly IMediator _mediator = mediator;

    public override void Configure()
    {
        Post(CreateRequest.Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new {entity}";
            s.ExampleRequest = new CreateRequest { Name = "Example" };
        });
        Tags("{Entity}s");
        Description(b =>
            b.Accepts<CreateRequest>("application/json")
            .Produces<CreateResponse>(201, "application/json")
            .ProducesProblem(400)
            .ProducesProblem(500));
    }

    public override async Task<Results<Created<CreateResponse>, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(CreateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new Create{Entity}Command(...));
        return result.ToCreatedResult(id => $"/{Entity}s/{id}", id => new CreateResponse(...));
    }
}

public class CreateRequest { public const string Route = "/{Entity}s"; public string Name { get; set; } = string.Empty; }
public class CreateValidator : Validator<CreateRequest> { /* FluentValidation rules */ }
public class CreateResponse(int id, string name) { public int Id { get; set; } = id; public string Name { get; set; } = name; }
```

**GlobalUsings.cs** (Web):
```csharp
global using Ardalis.Result;
global using FastEndpoints;
global using FastEndpoints.Swagger;
global using Mediator;
global using Microsoft.EntityFrameworkCore;
global using Serilog;
global using Serilog.Extensions.Logging;
```

---

#### 2e. Aspire Projects

**AspireHost** (`src/{PREFIX}.{PROJECT}.AspireHost`):
- References: `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`
- Project reference: `../{PREFIX}.{PROJECT}.Web`
- `IsAspireHost = true`, `OutputType = Exe`

**ServiceDefaults** (`src/{PREFIX}.{PROJECT}.ServiceDefaults`):
- `IsAspireSharedProject = true`
- `FrameworkReference`: `Microsoft.AspNetCore.App`
- Packages: `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery`,
  `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
  `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`,
  `OpenTelemetry.Instrumentation.Runtime`

---

### Step 3 — Self-Verification Checklist

After scaffolding, verify all of the following:

#### ✅ 3a. Folder & Project Structure
```
{Root}/
├── .editorconfig
├── .gitignore
├── .runsettings
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── {PREFIX}.{PROJECT}.slnx
├── src/
│   ├── {PREFIX}.{PROJECT}.Core/
│   ├── {PREFIX}.{PROJECT}.UseCases/
│   ├── {PREFIX}.{PROJECT}.Infrastructure/
│   ├── {PREFIX}.{PROJECT}.Web/
│   ├── {PREFIX}.{PROJECT}.AspireHost/          ← _aspire folder in solution
│   └── {PREFIX}.{PROJECT}.ServiceDefaults/     ← _aspire folder in solution
└── tests/
    ├── {PREFIX}.{PROJECT}.UnitTests/
    ├── {PREFIX}.{PROJECT}.IntegrationTests/
    ├── {PREFIX}.{PROJECT}.FunctionalTests/
    └── {PREFIX}.{PROJECT}.AspireTests/
```

#### ✅ 3b. Project Reference Chain
```
Web → Infrastructure → UseCases → Core
Web → UseCases (direct)
Web → ServiceDefaults
Infrastructure → Core
Infrastructure → UseCases
AspireHost → Web
AspireHost → ServiceDefaults (IsAspireProjectResource=false)
UnitTests → Core, UseCases
IntegrationTests → Infrastructure
FunctionalTests → Infrastructure, UseCases, Web
AspireTests → AspireHost
```

#### ✅ 3c. NuGet Packages (verify in Directory.Packages.props)
| Package | Version |
|---------|---------|
| Ardalis.GuardClauses | 5.0.0 |
| Ardalis.HttpClientTestExtensions | 4.2.0 |
| Ardalis.ListStartupServices | 1.1.4 |
| Ardalis.Result | 10.1.0 |
| Ardalis.Result.AspNetCore | 10.1.0 |
| Ardalis.SharedKernel | 4.1.0 |
| Ardalis.SmartEnum | 8.2.0 |
| Ardalis.Specification | 9.3.1 |
| Ardalis.Specification.EntityFrameworkCore | 9.3.1 |
| coverlet.collector | 8.0.0 |
| FastEndpoints | 7.1.1 |
| FastEndpoints.Swagger | 7.1.1 |
| MailKit | 4.15.1 |
| MimeKit | 4.15.1 |
| Mediator.Abstractions | 3.0.1 |
| Mediator.SourceGenerator | 3.0.1 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.0 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.0 |
| Microsoft.EntityFrameworkCore.Design | 10.0.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.0 |
| Microsoft.Extensions.Caching.Hybrid | 10.4.0 |
| Microsoft.Extensions.Configuration | 10.0.0 |
| Microsoft.Extensions.Logging | 10.0.0 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.0 |
| Microsoft.NET.Test.Sdk | 18.3.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 |
| NSubstitute | 5.3.0 |
| ReportGenerator | 5.5.0 |
| Scalar.AspNetCore | 2.13.6 |
| Serilog.AspNetCore | 10.0.0 |
| Serilog.Sinks.OpenTelemetry | 4.2.0 |
| Shouldly | 4.3.0 |
| SQLite | 3.13.0 |
| Testcontainers | 4.3.0 |
| Testcontainers.PostgreSql | 4.3.0 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.5 |
| Aspire.Hosting.AppHost | 13.1.2 |
| Aspire.Hosting.PostgreSQL | 13.1.2 |
| Microsoft.Extensions.Http.Resilience | 10.0.0 |
| Microsoft.Extensions.ServiceDiscovery | 10.0.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.0 |
| OpenTelemetry.Extensions.Hosting | 1.15.0 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.1 |
| OpenTelemetry.Instrumentation.Http | 1.15.0 |
| OpenTelemetry.Instrumentation.Runtime | 1.15.0 |
| Aspire.Hosting.Testing | 13.1.2 |
| Vogen | 8.0.5 |

#### ✅ 3d. Key Conventions
- `Directory.Build.props`: `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=true`, `ManagePackageVersionsCentrally=true`
- `global.json`: `"version": "10.0.200"`, `"rollForward": "latestMajor"`
- `.editorconfig`: file-scoped namespaces (`csharp_style_namespace_declarations = file_scoped:warning`), 2-space indent, `TreatWarningsAsErrors` 
- `.runsettings`: parallel xUnit test execution (`MaxCpuCount=0`, `ParallelizeAssembly=true`)
- All `*.cs` use **file-scoped namespaces**
- All entities use **primary constructors**
- Private fields use **`_camelCase`** prefix convention
- `AspireTests` project targets `net9.0` (Aspire.Hosting.Testing limitation) with its own `TargetFramework` override

#### ✅ 3e. CLI Commands — run in sequence
```powershell
# 1. Verify solution builds
dotnet build {PREFIX}.{PROJECT}.slnx

# 2. Run unit tests
dotnet test tests/{PREFIX}.{PROJECT}.UnitTests

# 3. Run integration tests
dotnet test tests/{PREFIX}.{PROJECT}.IntegrationTests

# 4. Run functional tests (requires Docker for Testcontainers)
dotnet test tests/{PREFIX}.{PROJECT}.FunctionalTests

# 5. EF Core initial migration
dotnet ef migrations add InitialCreate `
  --project src/{PREFIX}.{PROJECT}.Infrastructure `
  --startup-project src/{PREFIX}.{PROJECT}.Web `
  --output-dir Data/Migrations

# 6. Run the web app (SQLite fallback if no SQL Server)
dotnet run --project src/{PREFIX}.{PROJECT}.Web

# 7. Open Scalar API docs
# Navigate to https://localhost:{PORT}/scalar/v1
```

---

### Step 4 — Additional Files Generated

| File | Purpose |
|------|---------|
| `.gitignore` | Standard VS/dotnet gitignore (dotnet new gitignore) |
| `.editorconfig` | Code style rules (file-scoped ns, 2-space indent, etc.) |
| `.runsettings` | Parallel xUnit execution config |
| `nuget.config` | nuget.org source + optional LocalNuget source |
| `README.md` | Project overview with architecture diagram |
| `src/{Project}.Core/README.md` | Core layer notes |
| `src/{Project}.UseCases/README.md` | UseCases layer notes |
| `src/{Project}.Infrastructure/README.md` | Infrastructure layer notes |
| `PARALLEL_TEST_EXECUTION.md` | Notes on parallel test setup |
| `TESTCONTAINERS_IMPLEMENTATION.md` | Testcontainers setup notes |

> **Note:** No `Dockerfile` was present in the reference project. Add one if containerisation is required, using `mcr.microsoft.com/dotnet/aspnet:10.0` as the runtime image.

---

### Step 5 — Flags & Known Gotchas

⚠️ **Mediator.SourceGenerator** must have `PrivateAssets=all` and the full `IncludeAssets` list
in `Web.csproj` to work correctly as an analyzer/source generator.

⚠️ **EF Core Design** must have `ExcludeAssets="contentFiles" PrivateAssets="all"` in
`Infrastructure.csproj` to avoid conflicts.

⚠️ **Vogen** in `Core.csproj` requires removing the auto-generated `.targets` content file:
```xml
<Content Remove="...\.nuget\packages\vogen\8.0.2\contentFiles\any\netstandard2.0\Vogen.targets" />
```

⚠️ **Vogen + PostgreSQL EF Core Identity**:
When using PostgreSQL identity columns (via `.UseIdentityByDefaultColumn()`), EF Core initially creates entities with `default` Vogen structs before the database assigns the ID. To prevent `ValueObjectValidationException`:
1. Add `deserializationStrictness: DeserializationStrictness.AllowAnything` to the `[assembly: VogenDefaults(...)]` attribute (usually in your ID or Core assembly).
2. Explicitly tell Vogen to generate the EF Core value converter by adding `[EfCoreConverter<YourId>]` in the Infrastructure project's `VogenEfCoreConverters.cs`.
3. In your Entity Configuration, use `.HasVogenConversion()` instead of a manual `.HasConversion(...)` for the ID property.

⚠️ **Database Agnosticism (Raw SQL)**:
Avoid `FromSqlRaw` or hardcoded queries (e.g. Dapper) for basic projections. PostgreSQL requires quoted identifiers (e.g., `"Id"`) which breaks compatibility with SQL Server. Always prefer pure EF Core LINQ `.Select()` projections which correctly translate to any database provider's dialect.

⚠️ **Connection string priority**: Infrastructure resolves in order:
1. `"cleanarchitecture"` (Aspire orchestration)
2. `"DefaultConnection"` (SQL Server explicit)
3. `"SqliteConnection"` (SQLite local fallback)

⚠️ **`public partial class Program { }`** — must be at the end of `Program.cs` for integration
test `WebApplicationFactory` to reference the correct assembly.

---

## Section 6 — Feature Development Workflow (Zero to Legend)

Follow this checklist every time you add a new domain entity or feature. The order matters —
lower layers must be stable before upper layers build on them.

### 6a. Define the Domain (Core project)

```
src/{ns}.Core/{Entity}Aggregate/
├── {Entity}.cs                           ← Aggregate root
├── {Entity}Id.cs                         ← Vogen strongly-typed ID
├── {Entity}Name.cs                       ← Vogen value object(s)
├── {Entity}Status.cs                     ← SmartEnum
├── PhoneNumber.cs                        ← Owned value object (EF owned type)
├── Events/
│   ├── {Entity}CreatedEvent.cs           ← Domain event
│   └── {Entity}NameUpdatedEvent.cs
├── Handlers/
│   └── {Entity}NameUpdatedHandler.cs     ← INotificationHandler<TEvent>
└── Specifications/
    └── {Entity}ByIdSpec.cs               ← Ardalis.Specification
```

Checklist:
- [ ] Aggregate root inherits `FullAuditableEntity<T, TId>, IAggregateRoot` (or `AuditableEntity` for audit-only)
- [ ] Every state-changing method calls `RegisterDomainEvent(new XyzEvent(this))`
- [ ] All setters are `private set`
- [ ] IDs use `[ValueObject<int>]` with positive validation
- [ ] Enumerations use `SmartEnum<T>`
- [ ] Define interface for any domain service: `Interfaces/IDelete{Entity}Service.cs`

### 6b. Add Use Cases (UseCases project)

```
src/{ns}.UseCases/{Entity}s/
├── {Entity}DTO.cs
├── Create/
│   ├── Create{Entity}Command.cs          ← record : ICommand<Result<{Entity}Id>>
│   └── Create{Entity}Handler.cs          ← ICommandHandler<TCmd, TResult>
├── Get/
│   ├── Get{Entity}Query.cs               ← record : IQuery<Result<{Entity}DTO>>
│   └── Get{Entity}Handler.cs
├── List/
│   ├── List{Entity}sQuery.cs
│   ├── List{Entity}sHandler.cs
│   └── I{Entity}QueryService.cs          ← raw SQL / Dapper interface (optional)
├── Update/
│   ├── Update{Entity}Command.cs
│   └── Update{Entity}Handler.cs
└── Delete/
    ├── Delete{Entity}Command.cs
    └── Delete{Entity}Handler.cs
```

Rules:
- Commands return `Result` or `Result<T>` (never throw)
- Queries use `IReadRepository<T>` — never `IRepository<T>`
- Complex list queries implement a dedicated `I{Entity}QueryService` interface (prefer EF Core LINQ `.Select()` projections over Raw SQL to maintain database provider agnosticism)
- Handlers are registered automatically by `Mediator.SourceGenerator`

### 6c. Implement Infrastructure (Infrastructure project)

```
src/{ns}.Infrastructure/Data/
├── Config/{Entity}Configuration.cs       ← IEntityTypeConfiguration<T>
├── Queries/{Entity}QueryService.cs       ← implements I{Entity}QueryService
└── Migrations/                           ← dotnet ef migrations add
```

Checklist:
- [ ] Add `DbSet<{Entity}> {Entity}s => Set<{Entity}>();` to `AppDbContext`
- [ ] Create `{Entity}Configuration` using `.HasVogenConversion()` and `.UseIdentityByDefaultColumn()` (PostgreSQL identity)
- [ ] Register query service in the module's `Add{Module}Module` method in `InfrastructureServiceExtensions`
- [ ] Run migration: `dotnet ef migrations add Add{Entity} --project ... --startup-project ...`

### 6d. Expose via Web API (Web project)

```
src/{ns}.Web/{Entity}s/
├── Create.cs                             ← POST /{Entity}s
├── GetById.cs                            ← GET /{Entity}s/{id}
├── List.cs                               ← GET /{Entity}s
├── Update.cs                             ← PUT /{Entity}s/{id}
└── Delete.cs                             ← DELETE /{Entity}s/{id}
```

Checklist:
- [ ] Each endpoint file contains: `Endpoint<TReq, TRes>`, `Validator<TReq>`, request/response records

---

## Section 7 — Complete Testing Guide

### 7a. Unit Tests (`{ns}.UnitTests`) — Fast, no I/O

**Test domain logic, specifications, services, and use-case handlers in isolation.**
Use NSubstitute for mocks, Shouldly for assertions.

```csharp
// Tests/{Entity}Aggregate/{Entity}Tests.cs
public class {Entity}Tests
{
    [Fact]
    public void UpdateName_WhenNameChanges_RaisesDomainEvent()
    {
        var entity = new {Entity}({Entity}Name.From("Original"));

        entity.UpdateName({Entity}Name.From("Updated"));

        entity.DomainEvents.ShouldHaveSingleItem()
              .ShouldBeOfType<{Entity}NameUpdatedEvent>();
    }
}
```

```csharp
// Tests/UseCases/Create{Entity}HandlerTests.cs
public class Create{Entity}HandlerTests
{
    private readonly IRepository<{Entity}> _repo = Substitute.For<IRepository<{Entity}>>();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithId()
    {
        var entity = new {Entity}({Entity}Name.From("Test"));
        _repo.AddAsync(Arg.Any<{Entity}>(), Arg.Any<CancellationToken>())
             .Returns(entity);

        var handler = new Create{Entity}Handler(_repo);
        var result = await handler.Handle(
            new Create{Entity}Command({Entity}Name.From("Test"), null), default);

        result.IsSuccess.ShouldBeTrue();
    }
}
```

**Run:** `dotnet test tests/{ns}.UnitTests --no-build`

---

### 7b. Integration Tests (`{ns}.IntegrationTests`) — EF Core + real DB logic

**Test repositories, DbContext, and EF configurations against an in-memory or real database.**

```csharp
// Uses Microsoft.EntityFrameworkCore.InMemory for speed
public class {Entity}RepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly EfRepository<{Entity}> _repository;

    public {Entity}RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
        _repository = new EfRepository<{Entity}>(_dbContext);
    }

    [Fact]
    public async Task AddAsync_Persists{Entity}()
    {
        var entity = new {Entity}({Entity}Name.From("Integration"));
        await _repository.AddAsync(entity);

        var fetched = await _repository.GetByIdAsync(entity.Id);
        fetched.ShouldNotBeNull();
        fetched!.Name.Value.ShouldBe("Integration");
    }

    public void Dispose() => _dbContext.Dispose();
}
```

**Run:** `dotnet test tests/{ns}.IntegrationTests`

---

### 7c. Functional Tests (`{ns}.FunctionalTests`) — Full HTTP pipeline + SQL Server

**Test actual HTTP endpoints with a real SQL Server (via Testcontainers). These are the
most complete single-service tests.**

```csharp
// Tests Fixtures
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder().Build();

    public async Task InitializeAsync() => await _sqlContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DbContext, add Testcontainers SQL Server connection
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));
        });
    }

    public async Task DisposeAsync() => await _sqlContainer.DisposeAsync();
}

// Endpoint test
public class Create{Entity}Tests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Post_{Entity}s_Returns201()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/{Entity}s",
            new { Name = "Test {Entity}" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Create{Entity}Response>();
        body!.Id.ShouldBeGreaterThan(0);
    }
}
```

**Run:** `dotnet test tests/{ns}.FunctionalTests` (requires Docker)

---

### 7d. E2E / Aspire Tests (`{ns}.AspireTests`) — Full orchestration

**Test the complete distributed system — app + SQL Server — via Aspire orchestration.**

```csharp
public class {Entity}ApiTests
{
    [Fact]
    public async Task AppHost_Starts_And_{Entity}Endpoint_Responds()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.{Prefix}_{Project}_AspireHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("{project-web-resource-name}");
        var response = await httpClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

> ⚠️ Aspire E2E tests are slow — run these in CI only, not on every local build.

**Run:** `dotnet test tests/{ns}.AspireTests` (requires Docker + Aspire workload)

---

### 7e. Test Coverage & Reporting

```powershell
# Run all non-Aspire tests with coverage
dotnet test {ns}.slnx `
  --collect:"XPlat Code Coverage" `
  --results-directory TestResults `
  --filter "FullyQualifiedName!~AspireTests"

# Generate HTML report
dotnet tool run reportgenerator `
  -reports:"TestResults/**/coverage.cobertura.xml" `
  -targetdir:"TestResults/CoverageReport" `
  -reporttypes:Html
```

---

## Section 8 — Microservice Scaling Strategy

### 8a. When to Split: Monolith → Microservices

Start as a modular monolith. Extract a new microservice only when:
- A feature team needs independent deploy cadence
- A bounded context has radically different scaling needs
- The domain model has diverged enough to avoid shared schema

**Bounded Context → Service mapping:**
```
{ns}.Core  (domain)        → shared kernel / internal NuGet (Ardalis.SharedKernel)
{ns}.UseCases              → stays inside the service
{ns}.Infrastructure        → stays inside the service
{ns}.Web                   → the HTTP surface (one per microservice)
New: {ns}.Contracts        → integration events shared across services (NuGet package)
```

### 8b. Adding a Contracts Project for Cross-Service Events

When splitting services, add a `.Contracts` class library that both producer and consumer reference:

```
src/{ns}.Contracts/
├── {ns}.Contracts.csproj   ← no dependencies, just POCO
└── IntegrationEvents/
    ├── {Entity}CreatedIntegrationEvent.cs
    └── {Entity}DeletedIntegrationEvent.cs
```

```csharp
// IntegrationEvents/{Entity}CreatedIntegrationEvent.cs
// This is a DTO — must be serializable, no domain references
public record {Entity}CreatedIntegrationEvent(
    int Id,
    string Name,
    DateTimeOffset OccurredOn);
```

**Csproj** — zero dependencies, publish as NuGet package:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>true</IsPackable>
    <PackageId>{ns}.Contracts</PackageId>
  </PropertyGroup>
</Project>
```

### 8c. Domain Event → Integration Event Pipeline (Outbox Pattern)

**Rule:** Domain events are in-process. Integration events cross service boundaries.
Use the **Outbox Pattern** to reliably publish after the DB transaction commits.

```
Domain Event fires (in-process, synchronous)
       ↓
Domain Event Handler (in Core) writes to Outbox table
       ↓
Background Worker polls Outbox table
       ↓
Integration Event published to Message Broker (RabbitMQ / Azure Service Bus)
       ↓
Consumer Microservice receives event → handles it
```

**Step 1 — Add Outbox to Infrastructure:**
```csharp
// Infrastructure/Data/Outbox/OutboxMessage.cs
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;   // event type name
    public string Payload { get; set; } = string.Empty; // JSON
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
```

**Step 2 — Domain Event Handler writes to Outbox:**
```csharp
public class {Entity}CreatedOutboxHandler(AppDbContext db)
    : INotificationHandler<{Entity}CreatedEvent>
{
    public async ValueTask Handle({Entity}CreatedEvent e, CancellationToken ct)
    {
        var integrationEvent = new {Entity}CreatedIntegrationEvent(
            e.Entity.Id.Value, e.Entity.Name.Value, DateTimeOffset.UtcNow);

        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = nameof({Entity}CreatedIntegrationEvent),
            Payload = JsonSerializer.Serialize(integrationEvent)
        });
        // No SaveChanges here — the interceptor handles it atomically
    }
}
```

**Step 3 — Background Worker dispatches from Outbox:**
```csharp
// Infrastructure/Messaging/OutboxProcessor.cs
public class OutboxProcessor(AppDbContext db, IMessageBus bus, ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedOn == null)
                .OrderBy(m => m.OccurredOn)
                .Take(20)
                .ToListAsync(ct);

            foreach (var msg in messages)
            {
                try
                {
                    await bus.PublishAsync(msg.Type, msg.Payload, ct);
                    msg.ProcessedOn = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    msg.Error = ex.Message;
                    logger.LogError(ex, "Failed to process outbox message {Id}", msg.Id);
                }
            }
            await db.SaveChangesAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
```

---

## Section 9 — Event Driven Design: RabbitMQ / MassTransit

### 9a. Add Packages for Message Broker

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="MassTransit"                          Version="8.3.6" />
<PackageVersion Include="MassTransit.RabbitMQ"                Version="8.3.6" />
<PackageVersion Include="MassTransit.EntityFrameworkCore"     Version="8.3.6" />
```

Add to `Infrastructure.csproj`:
```xml
<PackageReference Include="MassTransit" />
<PackageReference Include="MassTransit.RabbitMQ" />
<PackageReference Include="MassTransit.EntityFrameworkCore" />
```

Add to `AspireHost.csproj` (for Aspire orchestration of RabbitMQ):
```xml
<!-- Add to Directory.Packages.props -->
<PackageVersion Include="Aspire.Hosting.RabbitMQ" Version="13.0.0" />

<!-- In AspireHost.csproj -->
<PackageReference Include="Aspire.Hosting.RabbitMQ" />
```

### 9b. Register MassTransit with RabbitMQ

```csharp
// Infrastructure/Messaging/MessagingServiceExtensions.cs
public static IServiceCollection AddMessagingServices(
    this IServiceCollection services, IConfiguration config)
{
    services.AddMassTransit(x =>
    {
        // Outbox: guarantees at-least-once delivery using EF Core
        x.AddEntityFrameworkOutbox<AppDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox(); // Route publishes through outbox table
        });

        // Register all consumers in this assembly
        x.AddConsumers(Assembly.GetExecutingAssembly());

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(config.GetConnectionString("rabbitmq") ?? "amqp://guest:guest@localhost/");

            // Auto-configure endpoints based on consumers
            cfg.ConfigureEndpoints(ctx);
        });
    });

    return services;
}
```

### 9c. Publishing Integration Events (Producer)

```csharp
// UseCases/{Entity}/Create/Create{Entity}Handler.cs (updated)
public class Create{Entity}Handler(
    IRepository<{Entity}> _repository,
    IPublishEndpoint _bus)           // ← MassTransit publisher
    : ICommandHandler<Create{Entity}Command, Result<{Entity}Id>>
{
    public async ValueTask<Result<{Entity}Id>> Handle(Create{Entity}Command cmd, CancellationToken ct)
    {
        var entity = new {Entity}(cmd.Name);
        var created = await _repository.AddAsync(entity, ct);

        // Publish via outbox — atomic with the SaveChanges
        await _bus.Publish(new {Entity}CreatedIntegrationEvent(
            created.Id.Value, created.Name.Value, DateTimeOffset.UtcNow), ct);

        return created.Id;
    }
}
```

### 9d. Consuming Integration Events (Consumer Microservice)

```csharp
// Infrastructure/Messaging/Consumers/{Entity}CreatedConsumer.cs
public class {Entity}CreatedConsumer(ILogger<{Entity}CreatedConsumer> logger)
    : IConsumer<{Entity}CreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<{Entity}CreatedIntegrationEvent> context)
    {
        var @event = context.Message;
        logger.LogInformation(
            "Received {Entity}Created: Id={Id}, Name={Name}",
            nameof({Entity}), @event.Id, @event.Name);

        // Handle the integration event — update read model, send notification, etc.
        await Task.CompletedTask;
    }
}
```

### 9e. Register RabbitMQ in Aspire AppHost

```csharp
// AspireHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
                      .WithManagementPlugin(); // adds RabbitMQ Management UI

var sql = builder.AddSqlServer("sql")
                 .AddDatabase("cleanarchitecture");

builder.AddProject<Projects.{Prefix}_{Project}_Web>("web")
       .WithReference(sql)
       .WithReference(rabbitMq)  // ← injects ConnectionStrings:rabbitmq
       .WaitFor(sql)
       .WaitFor(rabbitMq);

await builder.Build().RunAsync();
```

### 9f. Integration Event Testing

```csharp
// FunctionalTests/Messaging/{Entity}CreatedConsumerTests.cs
public class {Entity}CreatedConsumerTests
{
    [Fact]
    public async Task Consumer_Handles_{Entity}CreatedEvent()
    {
        await using var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer<{Entity}CreatedConsumer>();

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new {Entity}CreatedIntegrationEvent(1, "Test Entity", DateTimeOffset.UtcNow));

            (await consumerHarness.Consumed.Any<{Entity}CreatedIntegrationEvent>())
                .ShouldBeTrue();
        }
        finally
        {
            await harness.Stop();
        }
    }
}
```

Add to `FunctionalTests.csproj`:
```xml
<PackageVersion Include="MassTransit.Testing"   Version="8.3.6" />
<!-- in csproj -->
<PackageReference Include="MassTransit.Testing" />
```

---

## Section 10 — Recommended NuGet Additions for Microservices

Add these to `Directory.Packages.props` when scaling to microservices or EDD:

| Package | Version | Purpose |
|---------|---------|---------|
| `MassTransit` | 8.3.6 | Messaging abstraction |
| `MassTransit.RabbitMQ` | 8.3.6 | RabbitMQ transport |
| `MassTransit.EntityFrameworkCore` | 8.3.6 | EF Core Outbox support |
| `MassTransit.Testing` | 8.3.6 | In-memory test harness |
| `Aspire.Hosting.RabbitMQ` | 13.0.0 | Aspire RabbitMQ resource |
| `Polly` | 8.5.0 | Resilience policies (retry, circuit-breaker) |
| `Refit` | 8.0.0 | Typed HTTP clients for service-to-service calls |

---

## Section 11 — Architecture Decision Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| In-process messaging | Mediator source-gen | Zero reflection, AOT-compatible, fastest |
| Domain event dispatch | `SaveChangesInterceptor` | Atomic with DB commit |
| Cross-service messaging | MassTransit + RabbitMQ | Pluggable transport, Outbox support |
| Reliability | EF Core Outbox via MassTransit | At-least-once delivery guarantee |
| Service discovery | Aspire ServiceDefaults | `Microsoft.Extensions.ServiceDiscovery` |
| Observability | OpenTelemetry + Aspire dashboard | Traces, metrics, logs unified |
| Test data isolation | Testcontainers (per test class) | Real DB, no shared state |
| Aspire E2E | `DistributedApplicationTestingBuilder` | Full orchestration in CI |

---

## Section 12 — CQRS: Separating Read & Write Databases (Pragmatic Enterprise Path)

> **Philosophy:** Don't over-engineer from day one. Follow these three stages — each is a
> complete, working state. Migrate only when load, latency, or team size actually justifies it.

---

### Stage 1 — Foundation (Current state, already in place)

The scaffold already enforces the read/write split **at the interface level** via
`Ardalis.Specification`:

```csharp
// IRepository<T>     → write side (Add, Update, Delete + read)
// IReadRepository<T> → read side (query only, no mutation)

// RULE enforced by convention:
//   - Command handlers inject IRepository<T>
//   - Query handlers inject IReadRepository<T>
```

Both still point to the **same `EfRepository<T>` / same SQL Server** database. This is the
correct starting point. The interfaces are the seam you'll expand later.

**Also available:** For complex list/report queries, define a **raw-SQL query service**:
```csharp
// UseCases/{Entity}s/List/IList{Entity}sQueryService.cs
public interface IList{Entity}sQueryService
{
    Task<IEnumerable<{Entity}DTO>> ListAsync(int? skip, int? take, CancellationToken ct);
}

// Infrastructure/Data/Queries/List{Entity}sQueryService.cs
public class List{Entity}sQueryService(AppDbContext db)
    : IList{Entity}sQueryService
{
    public async Task<IEnumerable<{Entity}DTO>> ListAsync(int? skip, int? take, CancellationToken ct)
    {
        // Use raw SQL / Dapper / compiled queries — bypass change tracking entirely
        return await db.Database
            .SqlQuery<{Entity}DTO>(
                $"SELECT Id, Name FROM {Entity}s ORDER BY Id OFFSET {skip ?? 0} ROWS FETCH NEXT {take ?? 20} ROWS ONLY")
            .ToListAsync(ct);
    }
}
```

This keeps queries fast (no change tracking, no navigation loading) without any infrastructure change.

---

### Stage 2 — Read DbContext (Recommended for medium-scale apps)

When read queries become complex and you want **true separation of the EF model** for reads
(e.g., read-optimised projections, views, denormalised columns), introduce a second `DbContext`
dedicated to reads — still pointing to the **same database**:

```
src/{ns}.Infrastructure/Data/
├── AppDbContext.cs              ← write side (full model + change tracking)
└── ReadDbContext.cs             ← read side (projections, views, no tracking)
```

```csharp
// Infrastructure/Data/ReadDbContext.cs
public class ReadDbContext(DbContextOptions<ReadDbContext> options) : DbContext(options)
{
    // Only exposes the shapes needed for queries — can map database views
    public DbSet<{Entity}ReadModel> {Entity}s => Set<{Entity}ReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map to a DB view or same table — read-only projections
        modelBuilder.Entity<{Entity}ReadModel>()
                    .ToView("vw_{Entity}s")  // or .ToTable(...)
                    .HasNoKey();             // keyless entity = no change tracking
    }
}

// Infrastructure/Data/ReadModels/{Entity}ReadModel.cs
public class {Entity}ReadModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    // Denormalised / enriched columns from JOINs
}
```

**Register both DbContexts in `InfrastructureServiceExtensions`:**
```csharp
// Write context — full model, interceptors, events
services.AddDbContext<AppDbContext>((provider, options) =>
{
    var interceptor = provider.GetRequiredService<EventDispatchInterceptor>();
    options.UseSqlServer(writeConnectionString);
    options.AddInterceptors(interceptor);
});

// Read context — no interceptors, no change tracking by default
services.AddDbContext<ReadDbContext>(options =>
{
    options.UseSqlServer(readConnectionString)  // same DB for now — swap to replica later
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
```

**Update the query service to use `ReadDbContext`:**
```csharp
public class List{Entity}sQueryService(ReadDbContext db)
    : IList{Entity}sQueryService
{
    public async Task<IEnumerable<{Entity}DTO>> ListAsync(int? skip, int? take, CancellationToken ct) =>
        await db.{Entity}s
                .Select(x => new {Entity}DTO(x.Id, x.Name))
                .Skip(skip ?? 0)
                .Take(take ?? 20)
                .ToListAsync(ct);
}
```

At this stage `readConnectionString` and `writeConnectionString` are **the same value** — you've
paid no infrastructure cost but gained clean architectural separation.

---

### Stage 3 — True Read Replica (Enterprise scale)

When you're ready to route reads to a SQL Server read replica (or Azure SQL geo-replica):

**Step 1 — Add connection strings:**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection":  "Server=primary-sql;Database=MyApp;...",
    "ReadConnection":     "Server=replica-sql;Database=MyApp;ApplicationIntent=ReadOnly;..."
  }
}
```

**Step 2 — Aspire AppHost wires both:**
```csharp
var sqlWrite = builder.AddSqlServer("sql-write").AddDatabase("cleanarchitecture");
var sqlRead  = builder.AddSqlServer("sql-read").AddDatabase("cleanarchitecture-read");

builder.AddProject<Projects.{Prefix}_{Project}_Web>("web")
       .WithReference(sqlWrite)   // injected as ConnectionStrings:cleanarchitecture
       .WithReference(sqlRead);   // injected as ConnectionStrings:cleanarchitecture-read
```

**Step 3 — Infrastructure resolves separately:**
```csharp
string? writeConn = config.GetConnectionString("cleanarchitecture")
                 ?? config.GetConnectionString("DefaultConnection");

string? readConn  = config.GetConnectionString("cleanarchitecture-read")
                 ?? config.GetConnectionString("ReadConnection")
                 ?? writeConn; // graceful fallback to write DB if no replica configured

services.AddDbContext<AppDbContext>(options => options.UseSqlServer(writeConn!));
services.AddDbContext<ReadDbContext>(options =>
    options.UseSqlServer(readConn!)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
```

That single change in `InfrastructureServiceExtensions` is **all that changes** — the rest of the
codebase (use cases, handlers, endpoints) is untouched because they depend only on interfaces.

---

### Summary: Migration Cost at Each Stage

| Stage | Infrastructure change | Code change | When to do it |
|-------|-----------------------|-------------|---------------|
| **1** Foundation | None | None (already done) | ✅ Always |
| **2** Read DbContext | Add `ReadDbContext`, update registrations | Query services use `ReadDbContext` | Read queries become complex / need DB views |
| **3** Read Replica | Add second connection string | Nothing — config only | Read load > 60%, latency matters, or HA required |

---

### Key Rules to Enforce (Linting / Code Review)

```
✅  Command handlers  → inject IRepository<T>     (write side)
✅  Query handlers    → inject IReadRepository<T>  (read side, Stage 1)
✅  Query services    → inject ReadDbContext        (read side, Stage 2+)
❌  Query handlers must NEVER call repo.AddAsync / UpdateAsync / DeleteAsync
❌  Command handlers must NEVER inject ReadDbContext
```

> 💡 **Tip:** Enforce the read/write split with an Architecture Test using `NetArchTest.Rules`
> in `UnitTests`:
> ```csharp
> // ArchitectureTests/CqrsRulesTests.cs
> [Fact]
> public void QueryHandlers_ShouldNotDependOn_IRepository()
> {
>     var result = Types.InAssembly(typeof(List{Entity}sQuery).Assembly)
>         .That().HaveNameEndingWith("Handler")
>         .And().ImplementInterface(typeof(IQueryHandler<,>))
>         .ShouldNot().HaveDependencyOn("Ardalis.SharedKernel.IRepository`1")
>         .GetResult();
>
>     result.IsSuccessful.ShouldBeTrue();
> }
> ```
> Add `NetArchTest.Rules` to `UnitTests.csproj` for this.


