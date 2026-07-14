using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// Boots the real API pipeline (routing, model binding, controllers) but swaps the
/// data repositories for in-memory fakes so no SQL Server is required.
/// A fresh factory per test class gives each class an isolated dataset.
/// </summary>
public class AppRoleApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppRoleRepository>();
            services.RemoveAll<ILookupRepository>();

            // Singleton so state persists across requests within one factory instance.
            services.AddSingleton<IAppRoleRepository, FakeAppRoleRepository>();
            services.AddSingleton<ILookupRepository, FakeLookupRepository>();
        });
    }
}
