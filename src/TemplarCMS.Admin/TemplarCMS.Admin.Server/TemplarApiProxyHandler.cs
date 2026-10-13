using Microsoft.Extensions.Hosting;

namespace TemplarCMS.Admin.Server;

/// <summary>Forward only the requesting browser's cookies; never retain them in a pooled handler.</summary>
public sealed class TemplarApiProxyHandler : HttpClientHandler
{
    public TemplarApiProxyHandler(IHostEnvironment environment)
    {
        UseCookies = false;
        AllowAutoRedirect = false;
        if (environment.IsDevelopment())
            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
    }
}
