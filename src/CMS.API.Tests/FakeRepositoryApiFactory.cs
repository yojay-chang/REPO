using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// Boots the real API pipeline (routing, model binding, controllers, authentication/authorization)
/// but swaps the data repositories for in-memory fakes so no SQL Server is required.
/// A fresh factory per test class gives each class an isolated dataset.
/// </summary>
public abstract class FakeRepositoryApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppRoleRepository>();
            services.RemoveAll<IAppUserRepository>();
            services.RemoveAll<IPublishStatusRepository>();
            services.RemoveAll<IPartnerRepository>();
            services.RemoveAll<ICourseGroupRepository>();
            services.RemoveAll<ICourseRepository>();
            services.RemoveAll<IFeaturedPromoItemRepository>();
            services.RemoveAll<ILookupRepository>();
            services.RemoveAll<IAuthRepository>();
            services.RemoveAll<IRowAuditRepository>();

            // Singleton so state persists across requests within one factory instance.
            services.AddSingleton<IAppRoleRepository, FakeAppRoleRepository>();
            services.AddSingleton<IAppUserRepository, FakeAppUserRepository>();
            services.AddSingleton<IPublishStatusRepository, FakePublishStatusRepository>();
            services.AddSingleton<IPartnerRepository, FakePartnerRepository>();
            services.AddSingleton<ICourseGroupRepository, FakeCourseGroupRepository>();
            services.AddSingleton<ICourseRepository, FakeCourseRepository>();
            services.AddSingleton<IFeaturedPromoItemRepository, FakeFeaturedPromoItemRepository>();
            services.AddSingleton<ILookupRepository, FakeLookupRepository>();
            services.AddSingleton<IAuthRepository, FakeAuthRepository>();
            services.AddSingleton<IRowAuditRepository, FakeRowAuditRepository>();

            ConfigureAuthorization(services);
        });
    }

    /// <summary>Hook for subclasses to tighten or relax the global authorization policy.</summary>
    protected virtual void ConfigureAuthorization(IServiceCollection services)
    {
    }
}
