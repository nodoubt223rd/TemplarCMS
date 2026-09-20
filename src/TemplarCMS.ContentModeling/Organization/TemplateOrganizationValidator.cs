using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Organization;

public static class TemplateOrganizationValidator
{
    public static ValidationResult Validate(TemplateOrganizationSnapshot snapshot)
    {
        var errors = new List<ValidationError>();
        void Error(string message) => errors.Add(new ValidationError("InvalidTemplateOrganization", message));
        if (snapshot.Folders is null || snapshot.Placements is null)
        {
            Error("Folders and placements are required.");
            return new(errors);
        }
        if (snapshot.Folders.Any(f => f is null) || snapshot.Placements.Any(p => p is null))
        {
            Error("Null entries are not allowed.");
            return new(errors);
        }
        if (snapshot.Folders.GroupBy(f => f.Id).Any(g => g.Count() > 1)) Error("Duplicate folder IDs.");
        if (snapshot.Placements.GroupBy(p => p.TemplateId).Any(g => g.Count() > 1)) Error("Duplicate template placements.");
        foreach (var siblings in snapshot.Folders.GroupBy(f => f.ParentId))
            if (siblings.GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) Error("Folder keys must be unique among siblings.");
        var folders = snapshot.Folders.DistinctBy(f => f.Id).ToDictionary(f => f.Id);
        foreach (var folder in snapshot.Folders)
        {
            var seen = new HashSet<TemplarCMS.Domain.Content.TemplateFolderId> { folder.Id };
            var parent = folder.ParentId;
            while (parent is { } id)
            {
                if (!seen.Add(id)) { Error("Folder placement creates a cycle."); break; }
                if (!folders.TryGetValue(id, out var ancestor)) { Error("Parent folder does not exist."); break; }
                parent = ancestor.ParentId;
            }
        }
        foreach (var placement in snapshot.Placements)
            if (placement.TemplateId.Value == Guid.Empty || !folders.ContainsKey(placement.ParentId)) Error("Invalid template placement.");
        return new(errors);
    }
}
