using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Middleware;
using ADOTTA.Projects.Suite.Api.Services;
using ADOTTA.Projects.Suite.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Microsoft.OpenApi.Models;

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
builder.Services.AddScoped<IInitializationService, InitializationService>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<ProjectValidator>();

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

// Configure the HTTP request pipeline
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SAPSessionMiddleware>();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();

