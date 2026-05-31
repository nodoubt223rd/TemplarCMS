using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Validation;

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

        private ContentModelSnapshot _snapshot = ContentModelSnapshot.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentModelCatalog"/> class.
        /// </summary>
        public ContentModelCatalog(
            ITemplateRepository templateRepository,
            ITemplateValidator templateValidator,
            IEffectiveTemplateBuilder effectiveTemplateBuilder,
            IEffectiveTemplateValidator effectiveTemplateValidator)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _templateValidator = templateValidator ?? throw new ArgumentNullException(nameof(templateValidator));
            _effectiveTemplateBuilder = effectiveTemplateBuilder ?? throw new ArgumentNullException(nameof(effectiveTemplateBuilder));
            _effectiveTemplateValidator = effectiveTemplateValidator ?? throw new ArgumentNullException(nameof(effectiveTemplateValidator));
        }

        /// <inheritdoc />
        public Task<TemplateDefinition?> GetTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _snapshot.Templates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task<TemplateDefinition?> GetTemplateAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
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
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _snapshot.EffectiveTemplates.TryGetValue(id, out var template);

            return Task.FromResult(template);
        }

        /// <inheritdoc />
        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
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

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task RefreshAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var templates = await _templateRepository.GetTemplatesAsync(cancellationToken);
            var errors = new List<ValidationError>();

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
                throw new ContentModelCatalogRefreshException(errors);
            }

            var templatesById = templates.ToDictionary(
                template => template.Id);

            var templateKeys = templates.ToDictionary(
                template => template.Key,
                template => template.Id,
                StringComparer.OrdinalIgnoreCase);

            var effectiveTemplatesById = new Dictionary<Guid, EffectiveTemplateDefinition>();

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
                throw new ContentModelCatalogRefreshException(errors);
            }

            _snapshot = new ContentModelSnapshot(
                templatesById,
                effectiveTemplatesById,
                templateKeys);
        }
    }
}
