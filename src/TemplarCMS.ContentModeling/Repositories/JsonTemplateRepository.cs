using Microsoft.Extensions.Options;
using System.Text.Json;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Serialization;

namespace TemplarCMS.ContentModeling.Repositories
{
    /// <summary>
    /// Loads template definitions from JSON files stored on disk.
    /// </summary>
    /// <remarks>
    /// Each template is stored in its own JSON file.
    ///
    /// The repository is responsible only for persistence and mapping.
    /// It does not perform validation, inheritance resolution, caching,
    /// or catalog construction.
    /// </remarks>
    public sealed class JsonTemplateRepository : ITemplateRepository
    {
        private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly JsonTemplateRepositoryOptions _options;
        private readonly IJsonTemplateMapper _mapper;

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
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();

            var templates =
                new List<TemplateDefinition>(
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

                templates.Add(
                    _mapper.Map(dto));
            }

            return templates;
        }
    }
}
