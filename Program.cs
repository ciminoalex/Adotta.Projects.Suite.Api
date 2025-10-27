using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Middleware;
using ADOTTA.Projects.Suite.Api.Services;
using ADOTTA.Projects.Suite.Api.Validators;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SAP Settings
builder.Services.Configure<SAPSettings>(builder.Configuration.GetSection("SAPSettings"));

// Register services
builder.Services.AddHttpClient<ISAPServiceLayerClient, SAPServiceLayerClient>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ILookupService, LookupService>();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<ProjectValidator>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        policy.WithOrigins(allowedOrigins ?? new[] { "http://localhost:4200" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SAPSessionMiddleware>();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

app.Run();

