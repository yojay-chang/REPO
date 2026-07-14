using CMS.API.Data;
using CMS.API.Repositories;

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
builder.Services.AddScoped<ILookupRepository, LookupRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS API v1");
});

app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in the test project.
public partial class Program { }
