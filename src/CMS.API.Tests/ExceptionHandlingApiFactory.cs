using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// Keeps the real global authorization policy (so 401/403 behave exactly as in production) but swaps the
/// AppRole repository for one that always throws — letting <see cref="ExceptionHandlingTests"/> drive the
/// global exception-handling middleware through a real controller call.
/// </summary>
public class ExceptionHandlingApiFactory : FakeRepositoryApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppRoleRepository>();
            services.AddSingleton<IAppRoleRepository, ThrowingAppRoleRepository>();
        });
    }
}
