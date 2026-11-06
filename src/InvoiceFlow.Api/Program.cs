using System.Globalization;
using System.Text.Json.Serialization;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();

    // Readable text while developing; one JSON object per event everywhere else, ready for a log store.
    if (context.HostingEnvironment.IsDevelopment())
    {
        logger.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture);
    }
    else
    {
        logger.WriteTo.Console(new RenderedCompactJsonFormatter());
    }
});

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecurityTransformer>());
builder.Services.AddHealthChecks();

// Secure by default: every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Request logging wraps the exception handler, so it records the final status (e.g. 409), not the exception.
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

// API docs are always on: this is a demo application. A real deployment would gate them by environment.
app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference(options => options.WithTitle("InvoiceFlow API")).AllowAnonymous();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>()
        .InitializeAsync(seedDemoData: app.Configuration.GetValue<bool>("Database:SeedDemoData"));
}

await app.RunAsync();
