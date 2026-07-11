using TemplarCMS.Api.Content;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.MapContentLookupEndpoints();

app.Run();

public partial class Program
{
}
