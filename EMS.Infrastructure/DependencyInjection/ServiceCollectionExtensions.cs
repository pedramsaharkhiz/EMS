using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Infrastructure.Logging;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Validators.RefreshToken;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Identity.Seed;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Security;
using EMS.Infrastructure.Services.Identity;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using EMS.Infrastructure.Services.User;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EMS.Application.Validators.User;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace EMS.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
        services.AddScoped<IAuditEventLogger, AuditEventLogger>();
        services.Configure<AuditTrailOptions>(configuration.GetSection(AuditTrailOptions.SectionName));
        services.Configure<RefreshTokenCleanupOptions>(configuration.GetSection(RefreshTokenCleanupOptions.SectionName));
        services.AddHostedService<EMS.Infrastructure.Maintenance.RefreshTokenCleanupBackgroundService>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        var slowQueryThresholdMs = configuration.GetValue("Logging:SlowQueryThresholdMs", 500);
        services.AddSingleton(sp => new SlowQueryLoggingInterceptor(
            sp.GetRequiredService<ILogger<SlowQueryLoggingInterceptor>>(), slowQueryThresholdMs));
        services.AddDbContext<EMSDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("EMS"));
            // EF warns that e.g. Equipment (not filtered) requires EquipmentStatus (filtered). This is deliberate: an active
            // record can never reference a deactivated catalog row (deactivation is blocked while active dependents exist),
            // and queries that must show history use IncludingInactive(). See docs/CATALOGS.md.
            options.ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<SlowQueryLoggingInterceptor>());
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        { 
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true; 
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5; }
        ).AddErrorDescriber<PersianIdentityErrorDescriber>().AddEntityFrameworkStores<EMSDbContext>().AddDefaultTokenProviders();

        // Cookie attributes of the Identity cookie schemes come from appsettings (Security:Cookies), not from library defaults
        // (defaults would be: SameAsRequest, SameSite=Lax, 14 days sliding).
        services.Configure<CookieSecurityOptions>(configuration.GetSection(CookieSecurityOptions.SectionName));
        foreach (var scheme in new[]
        {
            IdentityConstants.ApplicationScheme,
            IdentityConstants.ExternalScheme,
            IdentityConstants.TwoFactorRememberMeScheme,
            IdentityConstants.TwoFactorUserIdScheme
        })
        {
            services.AddOptions<CookieAuthenticationOptions>(scheme)
                .Configure<IOptions<CookieSecurityOptions>>((cookie, security) =>
                {
                    var settings = security.Value;
                    cookie.Cookie.HttpOnly = settings.HttpOnly;
                    cookie.Cookie.SecurePolicy = settings.SecurePolicy;
                    cookie.Cookie.SameSite = settings.SameSite;
                });
        }

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<CookieSecurityOptions>>((cookie, security) =>
            {
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(security.Value.ExpireMinutes);
                cookie.SlidingExpiration = security.Value.SlidingExpiration;
                // This is an API: answer 401/403 instead of redirecting to a login page.
                cookie.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                cookie.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt settings not found.");

        services.AddAuthentication(options =>
        { 
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => 
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters 
            { 
                ValidateIssuer = true, 
                ValidateAudience = true,
                ValidateLifetime = true, 
                ValidateIssuerSigningKey = true, 
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                // The signature/lifetime are already valid here. Re-check the user's *current* state so that
                // deactivation and role/permission changes take effect immediately, not when the token expires.
                OnTokenValidated = async context =>
                {
                    var userIdText = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    var stamp = context.Principal?.FindFirst(TokenService.SecurityStampClaimType)?.Value;

                    if (!long.TryParse(userIdText, out var userId))
                    {
                        context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>()
                            .AuthenticationFailed("token without a valid user id");
                        context.Fail("Invalid user id.");
                        return;
                    }

                    var validator = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenValidator>();
                    var result = await validator.ValidateAsync(userId, stamp, context.HttpContext.RequestAborted);
                    if (result == AccessTokenValidationResult.Valid)
                        return;

                    var reason = result switch
                    {
                        AccessTokenValidationResult.UserNotFoundOrInactive => "user is inactive or no longer exists",
                        AccessTokenValidationResult.StampMismatch => "access changed since the token was issued (roles, permissions or password); refresh required",
                        _ => "token has no security stamp; sign in again"
                    };
                    context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>()
                        .AuthenticationFailed(reason);
                    context.Fail(reason);
                },
                // Fires only when a token was presented but rejected (expired, bad signature, ...).
                // The token itself is never logged.
                OnAuthenticationFailed = context =>
                {
                    var reason = context.Exception switch
                    {
                        SecurityTokenExpiredException => "token expired",
                        SecurityTokenInvalidSignatureException => "invalid signature",
                        SecurityTokenInvalidAudienceException => "invalid audience",
                        SecurityTokenInvalidIssuerException => "invalid issuer",
                        _ => context.Exception.GetType().Name
                    };

                    context.HttpContext.RequestServices
                        .GetRequiredService<ISecurityEventLogger>()
                        .AuthenticationFailed(reason);

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        { 
            foreach (var permission in EMS.Application.Common.Security.Permissions.All) 
            { 
                options.AddPolicy($"Permission:{permission}",
                    policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            } 
        });

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAccessTokenValidator, AccessTokenValidator>();
        services.AddScoped<ISecurityStampInvalidator, SecurityStampInvalidator>();

        // "ready" health check: verifies that the database is reachable.
        services.AddHealthChecks()
            .AddDbContextCheck<EMSDbContext>("database", tags: new[] { "ready" });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));


        services.Scan(scan => scan.FromAssemblyOf<Repository<object>>()
        .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Repository") && !t.IsGenericType)).AsMatchingInterface().WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblyOf<ICurrentUserService>()
        .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Service") && t != typeof(ICurrentUserService))).AsImplementedInterfaces().WithScopedLifetime());


        services.AddScoped<IIdentitySeeder, IdentitySeeder>();
        services.AddScoped<IAuthService, AuthService>(); 
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IUserRoleAssignmentService, UserRoleAssignmentService>();
        return services;
    }
}
