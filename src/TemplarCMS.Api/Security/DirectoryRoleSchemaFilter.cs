using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Api.Security;

internal sealed class DirectoryRoleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(DirectoryProfileRequest) || context.Type == typeof(DirectoryUserResponse))
        {
            if (!schema.Properties.TryGetValue("roles", out var roles)) return;
            roles.UniqueItems = true;
            roles.Items.Enum = RoleKeys();
            roles.Description = "Case-sensitive catalog keys; empty or multiple direct memberships are valid. " +
                "MarketingAutomationEditors cannot be newly assigned on POST or PUT; existing membership may be preserved or removed. " +
                "PUT requires the current revision; stale writes return 409. Memberships do not grant permissions.";
        }
        else if (context.Type == typeof(DirectoryRoleResponse))
        {
            schema.Properties["id"].Enum = RoleKeys();
            schema.Properties["availability"].Enum = [new OpenApiString("available"), new OpenApiString("planned")];
        }
    }

    private static List<IOpenApiAny> RoleKeys() => DirectoryRoleCatalog.Entries
        .Select(entry => (IOpenApiAny)new OpenApiString(entry.Role.ToString())).ToList();
}
