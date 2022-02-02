using Duende.IdentityServer;
using IdentityServer.Pages.Admin.ApiScopes;
using IdentityServer.Pages.Admin.Clients;
using IdentityServer.Pages.Admin.IdentityScopes;
using IdentityServerHost.Data;
using IdentityServerHost.Models;
using IdentityServerHost.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("ApplicationStoreConnectionString");

builder.Services.AddDbContext<ApplicationDbContext>(b =>
 {
     _ = b.UseMySql(builder.Configuration.GetConnectionString("ApplicationStoreConnectionString"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ApplicationStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
     if (builder.Environment.IsDevelopment())
     {
         _ = b.LogTo(Console.WriteLine, LogLevel.Information)
         .EnableSensitiveDataLogging()
         .EnableDetailedErrors();
     }
 });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ClaimsFactory<ApplicationUser>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services
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
       // this adds the config data from DB (clients, resources, CORS)
       .AddConfigurationStore(options => options.ConfigureDbContext = b =>
       {
           _ = b.UseMySql(builder.Configuration.GetConnectionString("ConfigurationStoreConnectionString"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ConfigurationStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
           if (builder.Environment.IsDevelopment())
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
               _ = b.UseMySql(builder.Configuration.GetConnectionString("OperationalStoreConnectionString"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("OperationalStoreConnectionString")), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));

               if (builder.Environment.IsDevelopment())
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
       .AddJwtBearerClientAuthentication().Services
       .AddAuthentication()
                .AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to https://localhost:5001/signin-google
                    options.ClientId = "copy client ID from Google here";
                    options.ClientSecret = "copy client secret from Google here";
                }).Services
        .AddControllersWithViews();

builder.Services
    .AddScoped<ApiScopeRepository>()
    .AddScoped<ClientRepository>()
    .AddScoped<IdentityScopeRepository>();


builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();
app.UseIdentityServer();

//app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});
app.Run();
