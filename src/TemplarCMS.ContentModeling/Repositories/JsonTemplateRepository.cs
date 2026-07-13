using Microsoft.Extensions.Options;
using System.Text.Json;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Serialization;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Repositories;

/// <summary>
/// Loads template definitions from JSON files stored on disk.
/// </summary>
/// <remarks>
/// Each template is stored in an individual JSON file.
///
/// This repository is responsible only for persistence and mapping.
/// It does not perform validation, inheritance resolution, caching,
/// or catalog construction.
/// </remarks>
public sealed class JsonTemplateRepository : ITemplateRepository
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

    private readonly JsonTemplateRepositoryOptions _options;
    private readonly IJsonTemplateMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="JsonTemplateRepository"/> class.
    /// </summary>
    /// <param name="options">
    /// Repository configuration options.
    /// </param>
    /// <param name="mapper">
    /// DTO mapper used to convert JSON models into
    /// domain definitions.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when required dependencies are null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the templates path is missing.
    /// </exception>
    public JsonTemplateRepository(
        IOptions<JsonTemplateRepositoryOptions> options,
        IJsonTemplateMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(mapper);

        if (string.IsNullOrWhiteSpace(
            options.Value.TemplatesPath))
        {
            throw new ArgumentException(
                "Templates path is required.",
                nameof(options));
        }

        _options = options.Value;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TemplateDefinition>>
        GetTemplatesAsync(
            CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.TemplatesPath))
        {
            throw new DirectoryNotFoundException(
                $"Template directory '{_options.TemplatesPath}' was not found.");
        }

        var templateFiles =
            Directory.GetFiles(
                    _options.TemplatesPath,
                    "*.json",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(
                    path => path,
                    StringComparer.Ordinal)
                .ToArray();

        var mappedTemplates =
            new List<(JsonTemplateDefinition Dto, TemplateDefinition Template)>(
                templateFiles.Length);

        foreach (var templateFile in templateFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json =
                await File.ReadAllTextAsync(
                    templateFile,
                    cancellationToken);

            var dto =
                JsonSerializer.Deserialize<JsonTemplateDefinition>(
                    json,
                    SerializerOptions);

            if (dto == null)
            {
                throw new InvalidOperationException(
                    $"Unable to deserialize template file '{templateFile}'.");
            }

            mappedTemplates.Add(
                (
                    dto,
                    _mapper.Map(dto)));
        }

        var templatesByKey =
            mappedTemplates.ToDictionary(
                pair => pair.Template.Key,
                pair => pair.Template);

        return mappedTemplates
            .Select(
                pair =>
                    ResolveBaseTemplate(
                        pair.Dto,
                        pair.Template,
                        templatesByKey))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task CreateTemplateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.TemplatesPath);

        var existingTemplates =
            Directory.GetFiles(
                _options.TemplatesPath,
                "*.json",
                SearchOption.TopDirectoryOnly).Length == 0
                ? Array.Empty<TemplateDefinition>()
                : await GetTemplatesAsync(cancellationToken);

        if (existingTemplates.Any(existing => existing.Id == template.Id))
        {
            throw new InvalidOperationException(
                $"Template id '{template.Id}' already exists.");
        }

        if (existingTemplates.Any(existing => existing.Key == template.Key))
        {
            throw new InvalidOperationException(
                $"Template key '{template.Key}' already exists.");
        }

        var templatePath =
            GetTemplatePath(
                template.Key);

        if (File.Exists(templatePath))
        {
            throw new InvalidOperationException(
                $"Template key '{template.Key}' already exists.");
        }

        var json =
            JsonSerializer.Serialize(
                MapJsonTemplateDefinition(template),
                SerializerOptions);

        await File.WriteAllTextAsync(
            templatePath,
            json,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteTemplateAsync(
        TemplateKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templatePath =
            GetTemplatePath(
                key);

        if (File.Exists(templatePath))
        {
            File.Delete(templatePath);
        }

        return Task.CompletedTask;
    }

    private JsonTemplateDefinition MapJsonTemplateDefinition(
        TemplateDefinition template)
    {
        return new JsonTemplateDefinition
        {
            Id = template.Id.Value,
            Name = template.Name,
            Key = template.Key.ToString(),
            BaseTemplates = template.BaseTemplate == null
                ? []
                : [template.BaseTemplate.Key.ToString()],
            Sections = template.Sections
                .Select(
                    section => new JsonTemplateSectionDefinition
                    {
                        Id = section.Id,
                        Name = section.Name,
                        Key = section.Key,
                        SortOrder = section.SortOrder,
                        Fields = section.Fields
                            .Select(
                                field => new JsonFieldDefinition
                                {
                                    Id = field.Id.Value,
                                    Name = field.Name,
                                    Key = field.Key,
                                    FieldType = field.FieldType.ToString(),
                                    IsShared = field.IsShared,
                                    IsUnversioned = field.IsUnversioned,
                                    Metadata = new Dictionary<string, string>(
                                        field.Metadata,
                                        StringComparer.Ordinal)
                                })
                            .ToList()
                    })
                .ToList()
        };
    }

    private string GetTemplatePath(
        TemplateKey key)
    {
        return Path.Combine(
            _options.TemplatesPath,
            $"{key}.json");
    }

    private static TemplateDefinition ResolveBaseTemplate(
        JsonTemplateDefinition dto,
        TemplateDefinition template,
        IReadOnlyDictionary<TemplateKey, TemplateDefinition> templatesByKey)
    {
        if (dto.BaseTemplates == null || dto.BaseTemplates.Count == 0)
        {
            return template;
        }

        if (dto.BaseTemplates.Count > 1)
        {
            throw new InvalidOperationException(
                $"Template '{template.Key}' declares multiple base templates, but only single inheritance is supported.");
        }

        var baseTemplateKey =
            new TemplateKey(dto.BaseTemplates[0]);

        if (!templatesByKey.TryGetValue(baseTemplateKey, out var baseTemplate))
        {
            throw new InvalidOperationException(
                $"Template '{template.Key}' references missing base template '{baseTemplateKey}'.");
        }

        return new TemplateDefinition(
            template.Id,
            template.Name,
            template.Key,
            baseTemplate,
            template.Sections);
    }
}
