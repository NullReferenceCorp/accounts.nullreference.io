namespace IdentityServer;

using IdentityServer.Pages.Admin.ApiScopes;
using IdentityServer.Pages.Admin.Clients;
using IdentityServer.Pages.Admin.IdentityScopes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods add project services.
/// </summary>
/// <remarks>
/// AddSingleton - Only one instance is ever created and returned.
/// AddScoped - A new instance is created and returned for each request/response cycle.
/// AddTransient - A new instance is created and returned each time.
/// </remarks>
internal static class ProjectServiceCollectionExtensions
{
    public static IServiceCollection AddProjectRepositories(this IServiceCollection services) =>
        services.AddScoped<ClientRepository>()
        .AddScoped<IdentityScopeRepository>()
        .AddScoped<ApiScopeRepository>();

    public static IServiceCollection AddProjectServices(this IServiceCollection services) =>
        services;

    public static IServiceCollection AddHostedServices(this IServiceCollection services) =>
     services;
}
