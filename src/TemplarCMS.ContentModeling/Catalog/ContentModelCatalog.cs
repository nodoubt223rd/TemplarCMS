using Microsoft.Extensions.Logging;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Catalog
{
    /// <summary>
    /// Provides runtime access to authored and effective content model definitions.
    /// </summary>
    /// <remarks>
    /// The catalog is the primary runtime entry point for content model consumers.
    /// It loads authored templates from an <see cref="ITemplateRepository"/>,
    /// validates them, builds effective templates, validates those effective
    /// templates, and publishes the completed result as an immutable snapshot.
    ///
    /// Runtime reads are served from the current snapshot and do not perform
    /// repository access, inheritance resolution, or validation.
    /// </remarks>
    public sealed class ContentModelCatalog : IContentModelCatalog
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ITemplateValidator _templateValidator;
        private readonly IEffectiveTemplateBuilder _effectiveTemplateBuilder;
        private readonly IEffectiveTemplateValidator _effectiveTemplateValidator;
        private readonly ILogger<ContentModelCatalog> _logger;

        private ContentModelSnapshot _snapshot = ContentModelSnapshot.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentModelCatalog"/> class.
        /// </summary>
        public ContentModelCatalog(
            ITemplateRepository templateRepository,
            ITemplateValidator templateValidator,
            IEffectiveTemplateBuilder effectiveTemplateBuilder,
            IEffectiveTemplateValidator effectiveTemplateValidator,
            ILogger<ContentModelCatalog> logger)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _templateValidator = templateValidator ?? throw new ArgumentNullException(nameof(templateValidator));
            _effectiveTemplateBuilder = effectiveTemplateBuilder ?? throw new ArgumentNullException(nameof(effectiveTemplateBuilder));
            _effectiveTemplateValidator = effectiveTemplateValidator ?? throw new ArgumentNullException(nameof(effectiveTemplateValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _snapshot.Templates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_snapshot.TemplateKeys.TryGetValue(key, out var id))
            {
                return Task.FromResult<TemplateDefinition?>(null);
            }

            _snapshot.Templates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _snapshot.EffectiveTemplates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_snapshot.TemplateKeys.TryGetValue(key, out var id))
            {
                return Task.FromResult<EffectiveTemplateDefinition?>(null);
            }

            _snapshot.EffectiveTemplates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task InvalidateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _snapshot = ContentModelSnapshot.Empty;

            _logger.LogInformation("Content model catalog snapshot was invalidated.");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RefreshAsync(
    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Refreshing content model catalog.");

            var templates = await _templateRepository.GetTemplatesAsync(cancellationToken);
            var errors = new List<ValidationError>();

            _logger.LogInformation(
                "Loaded {TemplateCount} authored content model templates.",
                templates.Count);

            foreach (var template in templates)
            {
                var validationResult = await _templateValidator.ValidateAsync(template, cancellationToken);

                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors);
                }
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning(
                    "Content model catalog refresh failed during authoring validation with {ErrorCount} validation errors.",
                    errors.Count);

                throw new ContentModelCatalogRefreshException(errors);
            }

            var templatesById = templates.ToDictionary(
                template => template.Id);

            var templateKeys = templates.ToDictionary(
                template => template.Key,
                template => template.Id);

            var effectiveTemplatesById = new Dictionary<TemplateId, EffectiveTemplateDefinition>();

            foreach (var template in templates)
            {
                var buildResult = await _effectiveTemplateBuilder.BuildEffectiveTemplateAsync(
                    template,
                    cancellationToken);

                if (!buildResult.Succeeded || buildResult.Value == null)
                {
                    errors.AddRange(buildResult.Errors);
                    continue;
                }

                var effectiveValidationResult = await _effectiveTemplateValidator.ValidateAsync(
                    buildResult.Value,
                    cancellationToken);

                if (!effectiveValidationResult.IsValid)
                {
                    errors.AddRange(effectiveValidationResult.Errors);
                    continue;
                }

                effectiveTemplatesById[buildResult.Value.Id] = buildResult.Value;
            }

            if (errors.Count > 0)
            {
                _logger.LogWarning(
                    "Content model catalog refresh failed during effective template generation with {ErrorCount} validation errors.",
                    errors.Count);

                throw new ContentModelCatalogRefreshException(errors);
            }

            _snapshot = new ContentModelSnapshot(
                templatesById,
                effectiveTemplatesById,
                templateKeys);

            _logger.LogInformation(
                "Content model catalog refresh completed with {TemplateCount} authored templates and {EffectiveTemplateCount} effective templates.",
                templatesById.Count,
                effectiveTemplatesById.Count);
        }
    }
}
