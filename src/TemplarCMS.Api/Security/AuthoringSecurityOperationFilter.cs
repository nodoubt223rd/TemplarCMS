using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TemplarCMS.Api.Security;

internal sealed class AuthoringSecurityOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var authorizeData =
            context.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .ToArray();

        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "AdminSession"
                        }
                    }
                ] = []
            });
        if (authorizeData.Any(data => data.Policy == ApiAuthorizationPolicies.ReadContent))
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new() { Type = ReferenceType.SecurityScheme, Id = DeliveryApiKeyDefaults.SchemeName } }] = []
            });
        foreach (var (status, description) in new[]
        {
            ("401", "Authentication is required. Supply an eligible credential for this operation."),
            ("403", "The credential does not grant access. Restricted account sessions return password-change-required.")
        })
        {
            if (operation.Responses.TryGetValue(status, out var response)) response.Description = description;
            else operation.Responses[status] = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/problem+json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository) }
                }
            };
        }
    }
}
