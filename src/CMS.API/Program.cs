using CMS.API.Controllers;
using CMS.API.Infrastructure;
using CMS.API.Repositories;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

// Dapper cannot bind DateOnly/TimeOnly as parameters out of the box (v2.1.66) — register handlers
// so `date`/`time` columns round-trip. Must run before any query executes.
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// --- Services ------------------------------------------------------------

builder.Services.AddControllers();

// Swagger / OpenAPI (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CMS API",
        Version = "v1"
    });
});

// CORS — allow the local Angular dev server (any localhost port).
const string CorsPolicy = "LocalhostCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Data access — Dapper (no EF).
builder.Services.AddSingleton<IDbConnectionFactory>(
    new SqlConnectionFactory(builder.Configuration.GetConnectionString("CMS")!));
builder.Services.AddScoped<IAppRoleRepository, AppRoleRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IPublishStatusRepository, PublishStatusRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<ICourseGroupRepository, CourseGroupRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IFeaturedPromoItemRepository, FeaturedPromoItemRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();

// --- Authentication / Authorization -------------------------------------
// JWT bearer, validated with the SAME SysConfig['appConfig'].symmetricSecurityKey used to issue
// tokens in AuthController (both go through ISigningKeyProvider). Read at runtime, never hard-coded.
builder.Services.AddSingleton<ISigningKeyProvider, SigningKeyProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<ISigningKeyProvider>((options, signingKeys) =>
    {
        options.MapInboundClaims = false; // keep "role"/"sub" claim names verbatim
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            // Resolve the key lazily per validation so it comes from SysConfig at runtime (cached).
            IssuerSigningKeyResolver = (_, _, _, _) => [signingKeys.GetSecurityKey()],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = AuthController.RoleClaimType,
            NameClaimType = "userName",
        };
    });

// Secure by default: every endpoint requires an authenticated user unless it opts out with
// [AllowAnonymous] (only AuthController does).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// --- Pipeline ------------------------------------------------------------

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS API v1");
    options.RoutePrefix = "swagger"; // UI at /swagger
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the test host (WebApplicationFactory) can reference the entry point.
public partial class Program { }
