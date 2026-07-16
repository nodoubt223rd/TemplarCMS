using Microsoft.OpenApi.Models;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Templates;

var builder = WebApplication.CreateBuilder(args);
var openApiEnabled =
    builder.Configuration.GetValue<bool?>("OpenApi:Enabled")
    ?? builder.Environment.IsDevelopment();

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
    });
builder.Services.AddTemplarCmsRuntime(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();

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

app.MapContentLookupEndpoints();
app.MapFieldTypeEndpoints();
app.MapTemplateEndpoints();

app.Run();

public partial class Program
{
}
