using System.Text.Json.Serialization;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Application.Security;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Api.Security;

public static class UserDirectoryEndpoints
{
    public const string Policy = "ManageUserDirectory";
    private const string Root = "/api/v1/security";

    public static void MapUserDirectoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Root).WithTags("User directory")
            .RequireAuthorization(Policy);
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            return config.GetValue<bool>("UserDirectory:Enabled")
                ? await next(context)
                : Problem(503, "directory-unavailable", "The user directory is not enabled on this instance.");
        });
        group.MapGet("/roles", () => TypedResults.Ok(new
        {
            items = Enum.GetValues<DirectoryRole>().Select(role => new { id = role.ToString(), label = Label(role) }),
            _links = new { self = new { href = Root + "/roles" } }
        })).WithName("GetDirectoryRoles").Produces(401).Produces(403).ProducesProblem(503);
        group.MapGet("/users", ListAsync).WithName("GetDirectoryUsers")
            .Produces<DirectoryListResponse>().ProducesProblem(400).Produces(401).Produces(403).ProducesProblem(503);
        group.MapGet("/users/{id:guid}", GetAsync).WithName("GetDirectoryUser")
            .Produces<DirectoryUserResponse>().ProducesProblem(404).Produces(401).Produces(403).ProducesProblem(503);
        group.MapPost("/users", CreateAsync).WithName("CreateDirectoryUser")
            .Produces<DirectoryUserResponse>(201).ProducesValidationProblem().ProducesProblem(409).Produces(401).Produces(403).ProducesProblem(503);
        group.MapPut("/users/{id:guid}", UpdateAsync).WithName("UpdateDirectoryUser")
            .Produces<DirectoryUserResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).Produces(401).Produces(403).ProducesProblem(503);
    }

    private static string Label(DirectoryRole role) => role switch
    {
        DirectoryRole.PlatformAdministrator => "Platform Administrator",
        DirectoryRole.SecurityAdministrator => "Security Administrator",
        DirectoryRole.TemplateDesigner => "Template Designer",
        DirectoryRole.ContentAuthor => "Content Author",
        DirectoryRole.MediaManager => "Media Manager",
        _ => role.ToString()
    };

    public static async Task<IResult> ListAsync(IUserDirectoryRepository repository, CancellationToken cancellationToken,
        string? search = null, string? role = null, string? status = null, int offset = 0, int limit = 50)
    {
        if (offset < 0 || limit is < 1 or > 100 ||
            (role is not null && !Enum.GetNames<DirectoryRole>().Contains(role)) ||
            (status is not null && !Enum.GetNames<DirectoryUserStatus>().Any(s => s.ToLowerInvariant() == status)))
            return Problem(400, "invalid-directory-filter", "Use catalog roles, known statuses, a nonnegative offset, and a limit between 1 and 100.");
        var users = (await repository.ListAsync(cancellationToken)).Where(u =>
            (role is null || u.Roles.Any(r => r.ToString() == role)) &&
            (status is null || u.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(search) || $"{u.FirstName} {u.LastName} {u.Email}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return TypedResults.Ok(new DirectoryListResponse(users.Skip(offset).Take(limit).Select(Response).ToArray(),
            users.Length, offset, limit, new Dictionary<string, DirectoryLink>
            {
                ["self"] = new(Root + "/users"), ["roles"] = new(Root + "/roles")
            }));
    }

    public static async Task<IResult> GetAsync(Guid id, IUserDirectoryRepository repository, CancellationToken cancellationToken) =>
        await repository.GetAsync(id, cancellationToken) is { } user
            ? TypedResults.Ok(Response(user)) : Problem(404, "directory-user-not-found", "The directory user was not found.");

    public static Task<IResult> CreateAsync(DirectoryProfileRequest request, IUserDirectoryRepository repository,
        CancellationToken cancellationToken) => WriteAsync(null, request, repository, cancellationToken);

    public static Task<IResult> UpdateAsync(Guid id, DirectoryProfileRequest request, IUserDirectoryRepository repository,
        CancellationToken cancellationToken) => WriteAsync(id, request, repository, cancellationToken);

    private static async Task<IResult> WriteAsync(Guid? id, DirectoryProfileRequest request,
        IUserDirectoryRepository repository, CancellationToken cancellationToken)
    {
        if (request.Roles is null || request.Roles.Any(role => !Enum.GetNames<DirectoryRole>().Contains(role)))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["roles"] = ["Select roles from the role catalog."] });
        var profile = new DirectoryUserProfile(request.FirstName ?? "", request.LastName ?? "", request.Email ?? "",
            request.Language ?? "en", request.Roles.Select(Enum.Parse<DirectoryRole>).ToArray());
        var errors = DirectoryProfileValidation.Validate(profile);
        if (id.HasValue && (!request.Revision.HasValue || request.Revision == Guid.Empty))
            errors["revision"] = ["The loaded profile revision is required."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        profile = DirectoryProfileValidation.Normalize(profile);
        var result = id.HasValue
            ? await repository.UpdateAsync(id.Value, request.Revision!.Value, profile, cancellationToken)
            : await repository.CreateAsync(profile, cancellationToken);
        return result.Status switch
        {
            DirectoryWriteStatus.Saved when !id.HasValue => TypedResults.Created($"{Root}/users/{result.User!.Id}", Response(result.User)),
            DirectoryWriteStatus.Saved => TypedResults.Ok(Response(result.User!)),
            DirectoryWriteStatus.NotFound => Problem(404, "directory-user-not-found", "The directory user was not found."),
            DirectoryWriteStatus.DuplicateEmail => Problem(409, "directory-email-conflict", "A directory record already uses this email address."),
            _ => Problem(409, "directory-revision-conflict", "This profile changed. Reload it before saving again.")
        };
    }

    private static DirectoryUserResponse Response(DirectoryUser user) => new(
        user.Id, user.FirstName, user.LastName, user.Email, user.Language,
        user.Status.ToString().ToLowerInvariant(), user.Roles.Select(r => r.ToString()).ToArray(),
        user.CreatedAt, user.LastLogin, user.Revision, new Dictionary<string, DirectoryLink>
        {
            ["self"] = new($"{Root}/users/{user.Id}"), ["collection"] = new(Root + "/users")
        });

    private static IResult Problem(int status, string code, string detail) => TypedResults.Problem(
        statusCode: status, title: "Unable to complete the directory request", detail: detail,
        type: $"/api/problems/{code}", extensions: new Dictionary<string, object?> { ["code"] = code });
}

// Reject status, credentials, IDs, and other properties outside the profile contract.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DirectoryProfileRequest(string? FirstName, string? LastName, string? Email,
    string[]? Roles, string? Language = "en", Guid? Revision = null);

public sealed record DirectoryLink(string Href);
public sealed record DirectoryUserResponse(Guid Id, string FirstName, string LastName, string Email,
    string Language, string Status, string[] Roles, DateTimeOffset CreatedAt, DateTimeOffset? LastLogin,
    Guid Revision, [property: JsonPropertyName("_links")] Dictionary<string, DirectoryLink> Links);
public sealed record DirectoryListResponse(DirectoryUserResponse[] Items, int Total, int Offset, int Limit,
    [property: JsonPropertyName("_links")] Dictionary<string, DirectoryLink> Links);
