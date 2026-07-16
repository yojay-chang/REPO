using System.Text;
using CMS.API.Auditing;
using CMS.API.Data;
using CMS.API.Middleware;
using CMS.API.Repositories;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// Map SQL Server `date` columns to DateOnly (Course.ScheduleOn/ScheduleOff). Must run before any query.
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "LocalhostCors";

builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CMS API",
        Version = "v1"
    });
});

// CORS — allow any localhost origin (any port) during local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin =>
              {
                  if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                      return uri.Host is "localhost" or "127.0.0.1";
                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Data + repositories
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IAppRoleRepository, AppRoleRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IPublishStatusRepository, PublishStatusRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<ICourseGroupRepository, CourseGroupRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IFeaturedPromoItemRepository, FeaturedPromoItemRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRowAuditRepository, RowAuditRepository>();

// Cross-cutting row-audit writer + the HTTP-context accessor it reads the acting user from.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRowAuditWriter, RowAuditWriter>();

// JWT bearer authentication. The signing key is resolved at validation time from SysConfig
// ('appConfig'.symmetricSecurityKey) via IAuthRepository — the same key the AuthController signs with.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IServiceScopeFactory>((options, scopeFactory) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Resolve the symmetric signing key per validation from the DB-backed config.
            IssuerSigningKeyResolver = (_, _, _, _) =>
            {
                using var scope = scopeFactory.CreateScope();
                var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
                var signingKey = authRepository.GetSigningKeyAsync().GetAwaiter().GetResult();
                return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))];
            },
        };
    });

// Require an authenticated user on every endpoint by default; AuthController opts out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// First in the pipeline: convert any unhandled exception downstream into a safe 500 JSON response.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS API v1");
});

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// After authentication/authorization so context.User is populated: a token still on the default password
// may only reach the change-password flow — everything else under /api gets 403 until it is changed.
app.UseMiddleware<PasswordChangeRequiredMiddleware>();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in the test project.
public partial class Program { }
