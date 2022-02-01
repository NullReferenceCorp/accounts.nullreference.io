namespace IdentityServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Boxed.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Duende.IdentityServer;
using IdentityServer.Constants;
using IdentityServer.Pages;
using IdentityServer.ConfigureOptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using IdentityServer.Data;
using IdentityServer.Models;

public class Startup
{
    private readonly IConfiguration configuration;
    private readonly IWebHostEnvironment webHostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration, where key value pair settings are stored. See
    /// http://docs.asp.net/en/latest/fundamentals/configuration.html</param>
    /// <param name="webHostEnvironment">The environment the application is running under. This can be Development,
    /// Staging or Production by default. See http://docs.asp.net/en/latest/fundamentals/environments.html</param>
    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        this.configuration = configuration;
        this.webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// Configures the services to add to the ASP.NET Core Injection of Control (IoC) container. This method gets
    /// called by the ASP.NET runtime. See
    /// http://blogs.msdn.com/b/webdev/archive/2014/06/17/dependency-injection-in-asp-net-vnext.aspx
    /// </summary>
    /// <param name="services">The services.</param>
    public virtual void ConfigureServices(IServiceCollection services) => services
            .ConfigureOptions<ConfigureRequestLoggingOptions>()
            .AddRazorPages().Services
            .AddResponseCompression()
            .AddCustomHealthChecks(this.webHostEnvironment, this.configuration)
            .AddCustomOpenTelemetryTracing(this.webHostEnvironment)
            .AddServerTiming()
            .AddDbContext<ApplicationDbContext>(b =>
            {
                _ = b.UseMySql(this.configuration.GetConnectionString("ApplicationStoreConnectionString"), ServerVersion.AutoDetect(this.configuration.GetConnectionString("ApplicationStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                if (this.webHostEnvironment.IsDevelopment())
                {
                    _ = b.LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                }
            })
            .AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders().Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/basics/resources
                options.EmitStaticAudienceClaim = true;
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddTestUsers(TestUsers.Users)
            // this adds the config data from DB (clients, resources, CORS)
            .AddConfigurationStore(options => options.ConfigureDbContext = b =>
                {
                    _ = b.UseMySql(this.configuration.GetConnectionString("ConfigurationStoreConnectionString"), ServerVersion.AutoDetect(this.configuration.GetConnectionString("ConfigurationStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
                    if (this.webHostEnvironment.IsDevelopment())
                    {
                        _ = b.LogTo(Console.WriteLine, LogLevel.Information)
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors();
                    }
                })
            // this is something you will want in production to reduce load on and requests to the DB
            //.AddConfigurationStoreCache()
            //
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                {
                    _ = b.UseMySql(this.configuration.GetConnectionString("OperationalStoreConnectionString"), ServerVersion.AutoDetect(this.configuration.GetConnectionString("OperationalStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));

                    if (this.webHostEnvironment.IsDevelopment())
                    {
                        _ = b.LogTo(Console.WriteLine, LogLevel.Information)
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors();
                    }
                };

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = true;
                options.RemoveConsumedTokens = true;
            })

        // this is only needed for the JAR and JWT samples and adds supports for JWT-based client authentication
        .AddJwtBearerClientAuthentication().Services

        .AddAuthentication()
            .AddOpenIdConnect("Google", "Sign-in with Google", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ForwardSignOut = IdentityServerConstants.DefaultCookieAuthenticationScheme;

                options.Authority = "https://accounts.google.com/";
                options.ClientId = "708778530804-rhu8gc4kged3he14tbmonhmhe7a43hlp.apps.googleusercontent.com";

                options.CallbackPath = "/signin-google";
                options.Scope.Add("email");
            }).Services
        .AddProjectRepositories()
        .AddProjectServices();

    public void Configure(IApplicationBuilder app) => app.UseSerilogRequestLogging()
.UseIf(
            this.webHostEnvironment.IsDevelopment(),
            x => x.UseServerTiming())
            .UseIf(
            this.webHostEnvironment.IsDevelopment(),
            x => x.UseServerTiming())
    .UseForwardedHeaders()
        .UseRouting()
        .UseCors(CorsPolicyName.AllowAny)
        .UseAuthentication()
        .UseAuthorization()
        .UseResponseCaching()
        .UseResponseCompression()
        .UseIf(
            this.webHostEnvironment.IsDevelopment(),
            x => x.UseDeveloperExceptionPage())

        .UseStaticFiles()
        .UseIdentityServer()
        .UseAuthorization()
          .UseEndpoints(
                endpoints =>
                {
                    _ = endpoints.MapControllers().RequireCors(CorsPolicyName.AllowAny);
                    _ = endpoints
                        .MapHealthChecks("/status")
                        .RequireCors(CorsPolicyName.AllowAny);
                    _ = endpoints
                        .MapHealthChecks("/status/self", new HealthCheckOptions() { Predicate = _ => false })
                        .RequireCors(CorsPolicyName.AllowAny);

                    _ = endpoints.MapRazorPages();
                });
    
}
