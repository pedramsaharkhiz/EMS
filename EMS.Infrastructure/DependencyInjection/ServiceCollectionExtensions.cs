using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Infrastructure.Logging;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Mapping;
using EMS.Application.Validators.RefreshToken;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Identity.Seed;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Persistence.Identity.Security;
using EMS.Infrastructure.Persistence.Identity.Service;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using EMS.Infrastructure.Services.Identity;
using EMS.Infrastructure.Services.User;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EMS.Application.Validators.User;
using Microsoft.IdentityModel.Tokens;
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
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<EMSDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("EMS"));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
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
        ).AddEntityFrameworkStores<EMSDbContext>().AddDefaultTokenProviders();

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

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(new[] { "EMS.Domain", "EMS.Infrastructure", "EMS.Application" });
        });
        //services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
        return services;
    }
}
