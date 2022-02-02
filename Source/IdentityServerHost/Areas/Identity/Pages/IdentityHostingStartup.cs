using IdentityServerHost.Areas.Identity.Pages;

[assembly: HostingStartup(typeof(IdentityHostingStartup))]

namespace IdentityServerHost.Areas.Identity.Pages;
public class IdentityHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => _ = builder.ConfigureServices((context, services) => { });
}
