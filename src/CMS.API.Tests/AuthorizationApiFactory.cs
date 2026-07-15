namespace CMS.API.Tests;

/// <summary>
/// Factory that keeps the real global authorization policy (authenticated user required on every
/// endpoint except AuthController). Used to prove the JWT enforcement end-to-end.
/// </summary>
public class AuthorizationApiFactory : FakeRepositoryApiFactory
{
}
