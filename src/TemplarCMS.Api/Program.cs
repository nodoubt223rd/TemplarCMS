using Microsoft.OpenApi.Models;
using TemplarCMS.Api;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Templates;

var builder = WebApplication.CreateBuilder(args);
var openApiEnabled =
    builder.Configuration.GetValue<bool?>("OpenApi:Enabled")
    ?? builder.Environment.IsDevelopment();
var authoringSecurityHeaderName =
    builder.Configuration.GetValue<string>("AuthoringSecurity:ApiKeyHeaderName")
    ?? "X-Templar-Api-Key";

builder.Services.AddProblemDetails();
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
            ApiKeyAuthenticationDefaults.SchemeName,
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = authoringSecurityHeaderName,
                In = ParameterLocation.Header,
                Description = "API key required for authoring endpoints when AuthoringSecurity is enabled."
            });
        options.OperationFilter<AuthoringSecurityOperationFilter>();
    });
builder.Services.AddTemplarApiAuthoringSecurity(
    builder.Configuration);
builder.Services.AddTemplarCmsRuntime(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

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
app.MapContentLookupEndpoints();
app.MapFieldTypeEndpoints();
app.MapTemplateEndpoints();

app.Run();

public partial class Program
{
}
