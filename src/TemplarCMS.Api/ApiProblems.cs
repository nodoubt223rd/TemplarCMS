using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Api;

internal static class ApiProblems
{
    public static ProblemHttpResult ContentItemNotFound(Guid id) =>
        TypedResults.Problem(
            title: "Content item was not found",
            detail: $"No content item exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult ContentItemNotFound(ContentPath path) =>
        TypedResults.Problem(
            title: "Content item was not found",
            detail: $"No content item exists at path '{path}'.",
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult ContentItemRequestRequired() =>
        TypedResults.Problem(
            title: "Content item request is required",
            detail: "Provide a content item payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentFieldValueRequestRequired() =>
        TypedResults.Problem(
            title: "Content field value request is required",
            detail: "Provide a field value payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentFieldValuesRequired() =>
        TypedResults.Problem(
            title: "Content field values are required",
            detail: "Provide one or more field values keyed by field key.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentPathRequired() =>
        TypedResults.Problem(
            title: "Content path is required",
            detail: "Provide a slash-delimited content path after '/api/v1/content/by-path/'.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidContentLookupRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid content lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidPathLookupRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid path lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidContentCreateRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid content create request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentItemCouldNotBeCreated(string detail, int statusCode) =>
        TypedResults.Problem(
            title: statusCode == StatusCodes.Status409Conflict
                ? "Content item could not be created"
                : "Invalid content create request",
            detail: detail,
            statusCode: statusCode);

    public static ProblemHttpResult UpdatedContentItemCouldNotBeLoaded(ContentItemId itemId) =>
        TypedResults.Problem(
            title: "Updated content item could not be loaded",
            detail: $"Content item '{itemId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult CreatedContentItemCouldNotBeLoaded(ContentItemId itemId) =>
        TypedResults.Problem(
            title: "Created content item could not be loaded",
            detail: $"Content item '{itemId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidContentUpdateRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid content update request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentItemWasNotFound(string detail) =>
        TypedResults.Problem(
            title: "Content item was not found",
            detail: detail,
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult ContentFieldValuesCouldNotBeSaved(string detail, int statusCode) =>
        TypedResults.Problem(
            title: statusCode == StatusCodes.Status404NotFound
                ? "Content item was not found"
                : "Content field values could not be saved",
            detail: detail,
            statusCode: statusCode);

    public static ProblemHttpResult InvalidContentFieldValueRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid content field value request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult ContentItemCouldNotBeDeleted(string detail) =>
        TypedResults.Problem(
            title: "Content item could not be deleted",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidContentDeleteRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid content delete request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult TemplateNotFound(Guid id) =>
        TypedResults.Problem(
            title: "Template was not found",
            detail: $"No template exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound);

    public static ProblemHttpResult TemplateRequestRequired() =>
        TypedResults.Problem(
            title: "Template request is required",
            detail: "Provide a template payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult InvalidTemplateLookupRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid template lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult CreatedTemplateCouldNotBeLoaded(TemplateId templateId) =>
        TypedResults.Problem(
            title: "Created template could not be loaded",
            detail: $"Template '{templateId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult TemplateCouldNotBeCreated(string detail, int statusCode) =>
        TypedResults.Problem(
            title: statusCode == StatusCodes.Status409Conflict
                ? "Template could not be created"
                : "Invalid template create request",
            detail: detail,
            statusCode: statusCode);

    public static ProblemHttpResult InvalidTemplateCreateRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid template create request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult UpdatedTemplateCouldNotBeLoaded(TemplateId templateId) =>
        TypedResults.Problem(
            title: "Updated template could not be loaded",
            detail: $"Template '{templateId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult TemplateCouldNotBeUpdated(string detail, int statusCode) =>
        TypedResults.Problem(
            title: statusCode switch
            {
                StatusCodes.Status404NotFound => "Template was not found",
                StatusCodes.Status409Conflict => "Template could not be updated",
                _ => "Invalid template update request"
            },
            detail: detail,
            statusCode: statusCode);

    public static ProblemHttpResult InvalidTemplateUpdateRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid template update request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);

    public static ProblemHttpResult TemplateCouldNotBeDeleted(string detail, int statusCode = StatusCodes.Status400BadRequest) =>
        TypedResults.Problem(
            title: statusCode == StatusCodes.Status404NotFound
                ? "Template was not found"
                : "Template could not be deleted",
            detail: detail,
            statusCode: statusCode);

    public static ProblemHttpResult InvalidTemplateDeleteRequest(string detail) =>
        TypedResults.Problem(
            title: "Invalid template delete request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);
}
