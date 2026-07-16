using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Api;

internal static class ApiProblems
{
    public static ProblemHttpResult ContentItemNotFound(Guid id) =>
        Create(
            title: "Content item was not found",
            detail: $"No content item exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound,
            code: "content-item-not-found");

    public static ProblemHttpResult ContentItemNotFound(ContentPath path) =>
        Create(
            title: "Content item was not found",
            detail: $"No content item exists at path '{path}'.",
            statusCode: StatusCodes.Status404NotFound,
            code: "content-item-not-found");

    public static ProblemHttpResult ContentItemRequestRequired() =>
        Create(
            title: "Content item request is required",
            detail: "Provide a content item payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "content-item-request-required");

    public static ProblemHttpResult ContentFieldValueRequestRequired() =>
        Create(
            title: "Content field value request is required",
            detail: "Provide a field value payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "content-field-value-request-required");

    public static ProblemHttpResult ContentFieldValuesRequired() =>
        Create(
            title: "Content field values are required",
            detail: "Provide one or more field values keyed by field key.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "content-field-values-required");

    public static ProblemHttpResult ContentPathRequired() =>
        Create(
            title: "Content path is required",
            detail: "Provide a slash-delimited content path after '/api/v1/content/by-path/'.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "content-path-required");

    public static ProblemHttpResult InvalidContentLookupRequest(string detail) =>
        Create(
            title: "Invalid content lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-lookup-request");

    public static ProblemHttpResult InvalidPathLookupRequest(string detail) =>
        Create(
            title: "Invalid path lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-path-lookup-request");

    public static ProblemHttpResult InvalidContentCreateRequest(string detail) =>
        Create(
            title: "Invalid content create request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-create-request");

    public static ProblemHttpResult ContentItemCouldNotBeCreated(string detail, int statusCode) =>
        Create(
            title: statusCode == StatusCodes.Status409Conflict
                ? "Content item could not be created"
                : "Invalid content create request",
            detail: detail,
            statusCode: statusCode,
            code: statusCode == StatusCodes.Status409Conflict
                ? "content-item-could-not-be-created"
                : "invalid-content-create-request");

    public static ProblemHttpResult UpdatedContentItemCouldNotBeLoaded(ContentItemId itemId) =>
        Create(
            title: "Updated content item could not be loaded",
            detail: $"Content item '{itemId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "updated-content-item-could-not-be-loaded");

    public static ProblemHttpResult CreatedContentItemCouldNotBeLoaded(ContentItemId itemId) =>
        Create(
            title: "Created content item could not be loaded",
            detail: $"Content item '{itemId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "created-content-item-could-not-be-loaded");

    public static ProblemHttpResult InvalidContentUpdateRequest(string detail) =>
        Create(
            title: "Invalid content update request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-update-request");

    public static ProblemHttpResult ContentItemCouldNotBeRenamed(string detail, int statusCode) =>
        Create(
            title: statusCode switch
            {
                StatusCodes.Status404NotFound => "Content item was not found",
                StatusCodes.Status409Conflict => "Content item could not be renamed",
                _ => "Invalid content rename request"
            },
            detail: detail,
            statusCode: statusCode,
            code: statusCode switch
            {
                StatusCodes.Status404NotFound => "content-item-not-found",
                StatusCodes.Status409Conflict => "content-item-could-not-be-renamed",
                _ => "invalid-content-rename-request"
            });

    public static ProblemHttpResult InvalidContentRenameRequest(string detail) =>
        Create(
            title: "Invalid content rename request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-rename-request");

    public static ProblemHttpResult ContentItemCouldNotBeMoved(string detail, int statusCode) =>
        Create(
            title: statusCode switch
            {
                StatusCodes.Status404NotFound => "Content item was not found",
                StatusCodes.Status409Conflict => "Content item could not be moved",
                _ => "Invalid content move request"
            },
            detail: detail,
            statusCode: statusCode,
            code: statusCode switch
            {
                StatusCodes.Status404NotFound => "content-item-not-found",
                StatusCodes.Status409Conflict => "content-item-could-not-be-moved",
                _ => "invalid-content-move-request"
            });

    public static ProblemHttpResult InvalidContentMoveRequest(string detail) =>
        Create(
            title: "Invalid content move request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-move-request");

    public static ProblemHttpResult ContentItemWasNotFound(string detail) =>
        Create(
            title: "Content item was not found",
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            code: "content-item-not-found");

    public static ProblemHttpResult ContentFieldValuesCouldNotBeSaved(string detail, int statusCode) =>
        Create(
            title: statusCode == StatusCodes.Status404NotFound
                ? "Content item was not found"
                : "Content field values could not be saved",
            detail: detail,
            statusCode: statusCode,
            code: statusCode == StatusCodes.Status404NotFound
                ? "content-item-not-found"
                : "content-field-values-could-not-be-saved");

    public static ProblemHttpResult InvalidContentFieldValueRequest(string detail) =>
        Create(
            title: "Invalid content field value request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-field-value-request");

    public static ProblemHttpResult ContentItemCouldNotBeDeleted(string detail) =>
        Create(
            title: "Content item could not be deleted",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "content-item-could-not-be-deleted");

    public static ProblemHttpResult InvalidContentDeleteRequest(string detail) =>
        Create(
            title: "Invalid content delete request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-content-delete-request");

    public static ProblemHttpResult TemplateNotFound(Guid id) =>
        Create(
            title: "Template was not found",
            detail: $"No template exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound,
            code: "template-not-found");

    public static ProblemHttpResult TemplateRequestRequired() =>
        Create(
            title: "Template request is required",
            detail: "Provide a template payload in the request body.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "template-request-required");

    public static ProblemHttpResult InvalidTemplateLookupRequest(string detail) =>
        Create(
            title: "Invalid template lookup request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-template-lookup-request");

    public static ProblemHttpResult CreatedTemplateCouldNotBeLoaded(TemplateId templateId) =>
        Create(
            title: "Created template could not be loaded",
            detail: $"Template '{templateId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "created-template-could-not-be-loaded");

    public static ProblemHttpResult TemplateCouldNotBeCreated(string detail, int statusCode) =>
        Create(
            title: statusCode == StatusCodes.Status409Conflict
                ? "Template could not be created"
                : "Invalid template create request",
            detail: detail,
            statusCode: statusCode,
            code: statusCode == StatusCodes.Status409Conflict
                ? "template-could-not-be-created"
                : "invalid-template-create-request");

    public static ProblemHttpResult InvalidTemplateCreateRequest(string detail) =>
        Create(
            title: "Invalid template create request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-template-create-request");

    public static ProblemHttpResult UpdatedTemplateCouldNotBeLoaded(TemplateId templateId) =>
        Create(
            title: "Updated template could not be loaded",
            detail: $"Template '{templateId}' was saved but could not be reloaded.",
            statusCode: StatusCodes.Status400BadRequest,
            code: "updated-template-could-not-be-loaded");

    public static ProblemHttpResult TemplateCouldNotBeUpdated(string detail, int statusCode) =>
        Create(
            title: statusCode switch
            {
                StatusCodes.Status404NotFound => "Template was not found",
                StatusCodes.Status409Conflict => "Template could not be updated",
                _ => "Invalid template update request"
            },
            detail: detail,
            statusCode: statusCode,
            code: statusCode switch
            {
                StatusCodes.Status404NotFound => "template-not-found",
                StatusCodes.Status409Conflict => "template-could-not-be-updated",
                _ => "invalid-template-update-request"
            });

    public static ProblemHttpResult InvalidTemplateUpdateRequest(string detail) =>
        Create(
            title: "Invalid template update request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-template-update-request");

    public static ProblemHttpResult TemplateCouldNotBeDeleted(string detail, int statusCode = StatusCodes.Status400BadRequest) =>
        Create(
            title: statusCode == StatusCodes.Status404NotFound
                ? "Template was not found"
                : "Template could not be deleted",
            detail: detail,
            statusCode: statusCode,
            code: statusCode == StatusCodes.Status404NotFound
                ? "template-not-found"
                : "template-could-not-be-deleted");

    public static ProblemHttpResult InvalidTemplateDeleteRequest(string detail) =>
        Create(
            title: "Invalid template delete request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            code: "invalid-template-delete-request");

    private static ProblemHttpResult Create(
        string title,
        string detail,
        int statusCode,
        string code)
    {
        var problem =
            new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = statusCode,
                Type = $"/api/problems/{code}"
            };

        problem.Extensions["code"] = code;

        return TypedResults.Problem(problem);
    }
}
