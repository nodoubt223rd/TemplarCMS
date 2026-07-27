using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl =
    builder.Configuration["TemplarApi:BaseUrl"]
    ?? "https://templarcms.api";

builder.Services.AddHttpClient(
    "TemplarApiProxy",
    client => client.BaseAddress = new Uri(apiBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });

var app = builder.Build();

app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    apiApp =>
    {
        apiApp.Run(
            async context =>
            {
                var clientFactory =
                    context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client =
                    clientFactory.CreateClient("TemplarApiProxy");
                using var requestMessage =
                    new HttpRequestMessage(
                        new HttpMethod(context.Request.Method),
                        context.Request.Path + context.Request.QueryString);

                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                {
                    requestMessage.Content = new StreamContent(context.Request.Body);

                    if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                    {
                        requestMessage.Content.Headers.ContentType =
                            MediaTypeHeaderValue.Parse(context.Request.ContentType);
                    }
                }

                foreach (var header in context.Request.Headers)
                {
                    if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    {
                        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                using var responseMessage =
                    await client.SendAsync(
                        requestMessage,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.RequestAborted);

                context.Response.StatusCode = (int)responseMessage.StatusCode;

                foreach (var header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                context.Response.Headers.Remove("transfer-encoding");

                await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
    });

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.MapFallbackToFile("/index.html");

app.Run();
