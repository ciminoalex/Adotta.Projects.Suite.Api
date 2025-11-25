using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Middleware;
using ADOTTA.Projects.Suite.Api.Services;
using ADOTTA.Projects.Suite.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Use enum as string with exact name matching (ON_GOING, CRITICAL, etc.)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ADOTTA Projects Suite API", Version = "v1" });

    var sessionScheme = new OpenApiSecurityScheme
    {
        Name = "X-SAP-Session-Id",
        Description = "Inserisci il SessionId di SAP Business One",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Session" }
    };

    c.AddSecurityDefinition("Session", sessionScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { sessionScheme, new List<string>() }
    });
});

// Configure SAP Settings
builder.Services.Configure<SAPSettings>(builder.Configuration.GetSection("SAPSettings"));

// Register services
builder.Services.AddHttpClient<ISAPServiceLayerClient, SAPServiceLayerClient>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var sapOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SAPSettings>>().Value;
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SSL");
        var cookieContainer = new System.Net.CookieContainer();

        if (sapOptions.AllowUntrustedServerCertificate)
        {
            logger.LogWarning("AllowUntrustedServerCertificate is ENABLED for SAP ServiceLayer client. DO NOT use in production.");
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                UseCookies = true,
                CookieContainer = cookieContainer,
                AllowAutoRedirect = true
            };
        }

        return new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true
        };
    });
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInitializationService, InitializationService>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<ProjectValidator>();

// Configure ForwardedHeaders for reverse proxy scenarios (IIS, nginx, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // In production, you might want to restrict this to known proxies
    // options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0 || Array.IndexOf(allowedOrigins, "*") >= 0)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure path base if specified (for deployment behind reverse proxy with subpath)
var pathBase = builder.Configuration["PathBase"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") ?? string.Empty;
var swaggerPathBase = string.IsNullOrEmpty(pathBase) ? string.Empty : pathBase.TrimEnd('/');

// Middleware to rewrite Swagger UI asset requests BEFORE UsePathBase processes them
// Swagger UI generates relative URLs, so when the page is at /api/swagger/index.html,
// it requests assets as /api/swagger/swagger-ui.css
// Swashbuckle serves assets from /swagger-ui/ (without path base), so we need to rewrite:
// /api/swagger/swagger-ui.css -> /swagger-ui/swagger-ui.css (remove path base for assets)
if (!string.IsNullOrEmpty(swaggerPathBase))
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Check if this is a Swagger UI asset request from the wrong path
        // This runs BEFORE UsePathBase, so we see the full path including /api
        if (path.StartsWith($"{swaggerPathBase}/swagger/swagger-ui", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith($"{swaggerPathBase}/swagger/swagger-ui-", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith($"{swaggerPathBase}/swagger/favicon-", StringComparison.OrdinalIgnoreCase))
        {
            // Remove path base and rewrite: /api/swagger/swagger-ui.css -> /swagger-ui/swagger-ui.css
            var pathWithoutBase = path.Substring(swaggerPathBase.Length);
            var newPath = pathWithoutBase.Replace("/swagger/", "/swagger-ui/");
            context.Request.Path = newPath;
        }
        
        await next();
    });
}

if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// Configure the HTTP request pipeline
// IMPORTANT: UseForwardedHeaders must be called BEFORE UseHttpsRedirection
// This allows the app to recognize HTTPS requests coming through a reverse proxy
app.UseForwardedHeaders();

// Enable Swagger in all environments
// Configure Swagger to work with path base
app.UseSwagger(c =>
{
    if (!string.IsNullOrEmpty(swaggerPathBase))
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{swaggerPathBase}" }
            };
        });
    }
});

app.UseSwaggerUI(c =>
{
    var swaggerJsonPath = string.IsNullOrEmpty(swaggerPathBase) 
        ? "/swagger/v1/swagger.json" 
        : $"{swaggerPathBase}/swagger/v1/swagger.json";
    c.SwaggerEndpoint(swaggerJsonPath, "ADOTTA Projects Suite API v1");
    c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger or /api/swagger
});

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SAPSessionMiddleware>();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();

