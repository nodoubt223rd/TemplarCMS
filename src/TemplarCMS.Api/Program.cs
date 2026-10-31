using Microsoft.OpenApi.Models;
using TemplarCMS.Api;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Observability;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Templates;
using TemplarCMS.Api.Media;
using TemplarCMS.Api.Commands;

var builder = WebApplication.CreateBuilder(args);
var openApiEnabled =
    builder.Configuration.GetValue<bool?>("OpenApi:Enabled")
    ?? builder.Environment.IsDevelopment();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "TemplarCMS API",
                Version = "v1"
            });
        options.AddSecurityDefinition(
            "AdminSession",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "__Host-TemplarAdmin",
                In = ParameterLocation.Cookie,
                Description = "Administrator session required. Change the temporary password before authoring; cookie mutations require X-Templar-CSRF."
            });
        options.OperationFilter<AuthoringSecurityOperationFilter>();
        options.SchemaFilter<DirectoryRoleSchemaFilter>();
    });
builder.Services.AddTemplarApiAuthoringSecurity(
    builder.Configuration);
builder.Services.AddTemplarCmsRuntime(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

if (AdminCommandRunner.IsCommand(args))
{
    Environment.ExitCode = await AdminCommandRunner.RunAsync(app.Services, args, Console.Out, Console.Error, app.Lifetime.ApplicationStopping);
    await app.DisposeAsync();
    return;
}

app.UseExceptionHandler();
app.UseTemplarRequestObservability();
app.Use(async (http, next) =>
{
    if (http.Request.Path.StartsWithSegments("/api/v1")) http.Response.Headers.CacheControl = "private, no-store";
    await next(http);
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthoringAntiforgeryMiddleware>();
app.UseAntiforgery();

if (openApiEnabled)
{
    app.UseSwagger(
        options =>
        {
            options.RouteTemplate = "openapi/{documentName}.json";
        });
    app.UseSwaggerUI(
        options =>
        {
            options.RoutePrefix = "openapi";
            options.SwaggerEndpoint("./v1.json", "TemplarCMS API v1");
        });
}

app.MapApiRootEndpoints(openApiEnabled);
app.MapHealthChecks("/health")
    .WithName("GetHealth")
    .WithTags("Operations")
    .AllowAnonymous();
app.MapContentLookupEndpoints();
app.MapFieldTypeEndpoints();
app.MapTemplateEndpoints();
app.MapTemplateOrganizationEndpoints();
app.MapContentOrderingEndpoints();
app.MapMediaEndpoints();
app.MapUserDirectoryEndpoints();
app.MapAuthEndpoints();
app.MapSwaggerUiAssetFallback();

app.Run();

public partial class Program
{
}
