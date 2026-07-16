using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.Api.Templates;

public static class FieldTypeEndpoints
{
    public static IEndpointRouteBuilder MapFieldTypeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/v1/field-types",
                GetAllAsync)
            .WithName("GetFieldTypes")
            .WithTags("Templates")
            .Produces<FieldTypeCollectionResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    public static Task<Ok<FieldTypeCollectionResponse>> GetAllAsync()
    {
        var fieldTypes =
            FieldTypeCatalog.GetAll()
                .Select(
                    descriptor => new FieldTypeResponse
                    {
                        Value = descriptor.FieldType.ToString(),
                        Label = descriptor.Label,
                        EditorKind = descriptor.EditorKind,
                        InputType = descriptor.InputType,
                        Placeholder = descriptor.Placeholder,
                        Rows = descriptor.Rows,
                        Step = descriptor.Step,
                        HelpText = descriptor.HelpText
                    })
                .ToArray();

        return Task.FromResult(
            TypedResults.Ok(
                new FieldTypeCollectionResponse
                {
                    Embedded = new FieldTypeCollectionEmbeddedResponse
                    {
                        FieldTypes = fieldTypes
                    },
                    Links = new FieldTypeCollectionLinksResponse
                    {
                        Self = new LinkResponse
                        {
                            Href = "/api/v1/field-types"
                        }
                    }
                }));
    }
}
