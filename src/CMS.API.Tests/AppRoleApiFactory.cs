using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CMS.API.Tests;

/// <summary>
/// Factory for controller-behavior tests. Relaxes the global auth requirement (clears the fallback
/// policy) so these tests can exercise controller logic without minting a token per request.
/// Auth enforcement itself is covered separately by <see cref="AuthorizationApiFactory"/>.
/// </summary>
public class AppRoleApiFactory : FakeRepositoryApiFactory
{
    protected override void ConfigureAuthorization(IServiceCollection services)
    {
        // Runs after Program's AddAuthorization(...) Configure, so the null wins → anonymous allowed.
        services.PostConfigure<AuthorizationOptions>(options => options.FallbackPolicy = null);
    }
}
