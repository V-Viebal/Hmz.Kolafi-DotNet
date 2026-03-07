using Hmz.Kolafi.Core.FeatureFlags;
using Hmz.Kolafi.Core.Interfaces;
using Hmz.Kolafi.Core.Services;
using Hmz.Kolafi.Core.UserAggregate;
using Hmz.Kolafi.Infrastructure.Data;
using Hmz.Kolafi.Infrastructure.Data.Queries;
using Hmz.Kolafi.Infrastructure.Identity;
using Hmz.Kolafi.UseCases.Contributors.List;

namespace Hmz.Kolafi.Infrastructure;
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    // ── Shared infrastructure (always registered) ──────────────────────

    // Try to get connection strings in order of priority:
    // 1. "cleanarchitecture" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional connection (PostgreSQL or SQL Server)
    // 3. "SqliteConnection" - fallback to SQLite (dev only)
    string? connectionString = config.GetConnectionString("cleanarchitecture")
                               ?? config.GetConnectionString("DefaultConnection")
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<AuditableInterceptor>();
    services.AddScoped<SoftDeleteInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();
      var auditableInterceptor = provider.GetRequiredService<AuditableInterceptor>();
      var softDeleteInterceptor = provider.GetRequiredService<SoftDeleteInterceptor>();

      // Determine which DB provider to use based on connection string name
      if (config.GetConnectionString("cleanarchitecture") != null ||
          config.GetConnectionString("DefaultConnection") != null)
      {
        // Use PostgreSQL (Npgsql) as the primary database
        options.UseNpgsql(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(auditableInterceptor, softDeleteInterceptor, eventDispatchInterceptor);
    });

    // Generic repositories — shared across all modules
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // HybridCache for user profile caching
    services.AddHybridCache();

    // ── Cross-cutting identity infrastructure (always registered) ──────
    // Auth/identity (Logto) is infrastructure, NOT a business domain module.
    // It must always be available regardless of feature flags.
    services.Configure<LogtoConfiguration>(config.GetSection(LogtoConfiguration.SectionName));
    services.AddHttpClient("LogtoManagement", (sp, client) =>
    {
      var logtoConfig = config.GetSection(LogtoConfiguration.SectionName).Get<LogtoConfiguration>();
      if (logtoConfig is not null && !string.IsNullOrEmpty(logtoConfig.ManagementApiEndpoint))
      {
        client.BaseAddress = new Uri(logtoConfig.ManagementApiEndpoint);
      }
    });
    services.AddScoped<ILogtoUserService, LogtoUserService>();

    // ── Feature-flagged business modules ───────────────────────────────

    // For each module, we list the interfaces it exposes to Mediator handlers.
    // If disabled, DispatchProxy creates throwing fakes of these interfaces to satisfy DI.
    services.AddModuleIf(FeatureFlags.ContributorsModule, config, logger, AddContributorsModule,
      typeof(IListContributorsQueryService), typeof(IDeleteContributorService));
      
    services.AddModuleIf(FeatureFlags.UsersModule, config, logger, AddUsersModule,
      typeof(ICachedUserProfileService));

    // ───────────────────────────────────────────────────────────────────

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }

  // ── Module registration methods ────────────────────────────────────────
  // Each method contains ONLY business-domain services for that module.
  // Cross-cutting infrastructure (DB, auth, caching, logging) stays in the
  // shared section above — never behind a feature flag.

  /// <summary>
  /// Registers Contributors module services (query services, domain services).
  /// </summary>
  private static void AddContributorsModule(IServiceCollection services, ConfigurationManager config)
  {
    services.AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
            .AddScoped<IDeleteContributorService, DeleteContributorService>();
  }

  /// <summary>
  /// Registers Users module business services (profile caching).
  /// Note: Logto/identity infrastructure is always registered (cross-cutting).
  /// </summary>
  private static void AddUsersModule(IServiceCollection services, ConfigurationManager config)
  {
    services.AddScoped<ICachedUserProfileService, CachedUserProfileService>();
  }

  // ── Helper ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Conditionally registers a module's services if its feature flag is enabled.
  /// If disabled, registers "fake" throwing proxies for the provided interface types so that 
  /// unconditionally generated Mediator handlers still pass ASP.NET Core DI validation on startup.
  /// </summary>
  private static void AddModuleIf(
    this IServiceCollection services,
    string featureFlagName,
    ConfigurationManager config,
    ILogger logger,
    Action<IServiceCollection, ConfigurationManager> registerModule,
    params Type[] moduleInterfaces)
  {
    var isEnabled = config.GetSection("FeatureManagement")
                         .GetValue<bool>(featureFlagName);

    if (isEnabled)
    {
      registerModule(services, config);
      logger.LogInformation("Module {Module} services registered", featureFlagName);
    }
    else
    {
      logger.LogInformation("Module {Module} is DISABLED — registering empty proxies", featureFlagName);
      
      // We must register a fake instance for each interface to satisfy ASP.NET Core DI ValidateOnBuild
      // because Mediator dynamically registers all handlers, which demand these interfaces.
      foreach (var type in moduleInterfaces)
      {
         var proxyType = typeof(DisabledServiceProxy<>).MakeGenericType(type);
         var createMethod = proxyType.GetMethod(nameof(DisabledServiceProxy<object>.Create), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
         var proxyInstance = createMethod!.Invoke(null, null);

         services.AddScoped(type, _ => proxyInstance!);
      }
    }
  }

  /// <summary>
  /// Generates a proxy that implements a given interface but throws an exception on any method call.
  /// This ensures disabled modules cannot be accidentally invoked, while keeping the DI graph valid.
  /// </summary>
  public class DisabledServiceProxy<T> : System.Reflection.DispatchProxy
  {
      protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
      {
          throw new InvalidOperationException($"The module containing {typeof(T).Name} is currently disabled by a feature flag.");
      }

      public static T Create()
      {
          return Create<T, DisabledServiceProxy<T>>();
      }
  }
}
