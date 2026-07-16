using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Templates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddTemplarCmsRuntime(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
app.MapContentLookupEndpoints();
app.MapFieldTypeEndpoints();
app.MapTemplateEndpoints();

app.Run();

public partial class Program
{
}
