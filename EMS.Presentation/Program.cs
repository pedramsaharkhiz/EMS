using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Extensions;
using EMS.Infrastructure.DependencyInjection;
using EMS.Infrastructure.Identity.Seed;
using EMS.Infrastructure.Persistence.Seed;
using EMS.Presentation.Errors;
using EMS.Presentation.Conventions;
using EMS.Presentation.Middlewares;
using EMS.Presentation.Security;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Text.Json;

// Bootstrap logger: captures failures that happen before the host (and the configured logger) exists.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog is configured from the "Serilog" section of appsettings (levels, sinks, retention).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EMS.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    // Security headers come from appsettings (Security:Headers); a malformed configuration stops start-up instead of
    // silently running without protection.
    builder.Services.AddOptions<SecurityHeadersOptions>()
        .Bind(builder.Configuration.GetSection(SecurityHeadersOptions.SectionName))
        .Validate(options => options.Validate() is null, "Security:Headers configuration is invalid.")
        .ValidateOnStart();

    // Do not advertise the server software ("Server: Kestrel") unless configured otherwise.
    builder.WebHost.ConfigureServerHeaderFromConfiguration();

    // Add services to the container.
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // CORS Policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCorsPolicy", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:4200",
                    "https://localhost:3000",
                    "https://localhost:4200",
                    "https://localhost:5173",
                    "http://localhost:7224",
                    "https://localhost:7224")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new ApiResponseConvention());

            // Non-nullable properties are not implicitly [Required] (that produced English "The X field is required." next to our
            // own Persian validator messages); required fields are declared in the FluentValidation validators.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

            // MVC's binding messages (a query string like ?pageSize=abc) are English by default.
            var binding = options.ModelBindingMessageProvider;
            binding.SetValueIsInvalidAccessor(value => $"مقدار «{value}» معتبر نیست.");
            binding.SetAttemptedValueIsInvalidAccessor((value, field) => $"مقدار «{value}» برای «{field}» معتبر نیست.");
            binding.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"مقدار «{value}» معتبر نیست.");
            binding.SetValueMustBeANumberAccessor(field => $"«{field}» باید عدد باشد.");
            binding.SetNonPropertyValueMustBeANumberAccessor(() => "مقدار باید عدد باشد.");
            binding.SetMissingBindRequiredValueAccessor(field => $"«{field}» الزامی است.");
            binding.SetMissingKeyOrValueAccessor(() => "مقدار الزامی است.");
            binding.SetMissingRequestBodyRequiredValueAccessor(() => ApiErrors.BodyInvalidMessage);
            binding.SetUnknownValueIsInvalidAccessor(field => $"مقدار «{field}» معتبر نیست.");
            binding.SetValueMustNotBeNullAccessor(value => $"مقدار «{value}» الزامی است.");
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        })
        // Model-validation failures (FluentValidation, malformed JSON) use the same error envelope as every other error.
        .ConfigureApiBehaviorOptions(options =>
        {
            // Without this MVC turns a bare 415 (body is not JSON) into its own English ProblemDetails; UseStatusCodePages
            // below then writes the error envelope for it instead.
            options.SuppressMapClientErrors = true;

            options.InvalidModelStateResponseFactory = actionContext =>
                new ApiErrorResult(StatusCodes.Status400BadRequest, ApiErrors.FromModelState(actionContext.HttpContext, actionContext.ModelState));
        });

    // OpenAPI Service
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Enter The JWT Token ..."
            };

            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
            });

            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    var headerOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecurityHeadersOptions>>().Value;
    if (headerOptions.Enabled && headerOptions.Values.Count == 0)
        Log.Warning("Security headers are enabled but none are configured under {Section}:Values", SecurityHeadersOptions.SectionName);
    if (!headerOptions.Enabled)
        Log.Warning("Security headers are DISABLED ({Section}:Enabled = false)", SecurityHeadersOptions.SectionName);

    Log.Information(
        "Starting EMS API in {EnvironmentName} environment (logs directory: {LogsDirectory})",
        app.Environment.EnvironmentName,
        Path.Combine(Directory.GetCurrentDirectory(), "logs"));

    // Configure the HTTP request pipeline.
    // Order matters: correlation id first (so everything below is tagged with it), then request logging
    // (sees the final status code), then the exception middleware (turns exceptions into responses).
    // Very first, so that every response (errors, 401/403, CORS pre-flights) carries the security headers.
    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            var path = httpContext.Request.Path;
            if (path.StartsWithSegments("/health") ||
                path.StartsWithSegments("/scalar") ||
                path.StartsWithSegments("/openapi"))
            {
                return LogEventLevel.Verbose;
            }

            var statusCode = httpContext.Response.StatusCode;
            if (ex is not null || statusCode >= 500)
                return LogEventLevel.Error;
            if (statusCode == 499) // client closed the request
                return LogEventLevel.Information;
            if (statusCode >= 400)
                return LogEventLevel.Warning;
            return LogEventLevel.Information;
        };
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserAgent", LogSanitizer.Sanitize(httpContext.Request.Headers.UserAgent.ToString(), 200));

            var currentUser = httpContext.RequestServices.GetService<ICurrentUserService>();
            if (currentUser is { IsAuthenticated: true })
            {
                diagnosticContext.Set("UserId", currentUser.UserId);
                diagnosticContext.Set("UserName", LogSanitizer.Sanitize(currentUser.UserName));
            }
        };
    });

    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Responses that have an error status but no body (401/403 from authentication/authorization, 404 for an unknown
    // route, 405, ...) get the same error document. Responses that already carry a body are left alone.
    app.UseStatusCodePages(statusContext => ApiErrors.WriteAsync(
        statusContext.HttpContext,
        statusContext.HttpContext.Response.StatusCode,
        ApiErrors.ForStatus(statusContext.HttpContext, statusContext.HttpContext.Response.StatusCode)));

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("EMS API")
                .WithTheme(ScalarTheme.Solarized)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .AddPreferredSecuritySchemes("Bearer")
                .AddHttpAuthentication("Bearer", auth =>
                {
                    auth.Description = "Enter The JWT Token ";
                });
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("DefaultCorsPolicy");

    app.UseAuthentication();
    app.UseMiddleware<UserLogContextMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Liveness: the process is up. Readiness: dependencies (database) are reachable.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
        await seeder.SeedAsync();

        // The three system posts always exist (re-created if someone removed them directly in the database).
        await scope.ServiceProvider.GetRequiredService<ISystemPostsGuard>().EnsureAsync();

        // The Tables registry follows the [Attachable] entities (a new attachable entity needs no seed of its own).
        await scope.ServiceProvider.GetRequiredService<IAttachmentTableSeeder>().SeedAsync();

        // Optional reference data (Seed:Geography:Enabled): the provinces of Iran with their capitals. Off by default.
        if (app.Configuration.GetValue<bool>("Seed:Geography:Enabled"))
            await scope.ServiceProvider.GetRequiredService<IGeographySeeder>().SeedAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not ("HostAbortedException" or "StopTheHostException"))
{
    // HostAbortedException / StopTheHostException are thrown on purpose by EF Core design-time tooling
    // to stop the host after it was built; they are not failures and are deliberately not caught here.
    Log.Fatal(ex, "EMS API terminated unexpectedly during startup or shutdown");
    throw;
}
finally
{
    Log.Information("EMS API is shutting down");
    Log.CloseAndFlush();
}

// Exposes the entry point to WebApplicationFactory in integration tests.
public partial class Program { }
