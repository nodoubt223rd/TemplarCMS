using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Api.Templates;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Templates;

public sealed class TemplateEndpointsTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnTemplatesInStableOrder()
    {
        var article =
            CreateTemplate(
                "Article Page",
                "article-page");
        var landing =
            CreateTemplate(
                "Landing Page",
                "landing-page");
        var catalog =
            new FakeContentModelCatalog(
                article,
                landing);

        var result =
            await TemplateEndpoints.GetAllAsync(
                catalog,
                TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);

        var templates = result.Value.Embedded.Templates.ToArray();
        Assert.Equal(2, templates.Length);
        Assert.Equal("article-page", templates[0].Key);
        Assert.Equal("landing-page", templates[1].Key);
        Assert.Equal("/api/v1/templates", result.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{article.Id.Value}", templates[0].Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{article.Id.Value}/fields", templates[0].Links.Fields.Href);
        Assert.Equal("/api/v1/content", templates[0].Links.CreateItem.Href);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var template =
            CreateTemplate();
        var catalog =
            new FakeContentModelCatalog(
                template);

        var result =
            await TemplateEndpoints.GetByIdAsync(
                template.Id.Value,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Equal(template.Id.Value.ToString(), ok.Value.Id);
        Assert.Equal(template.Name, ok.Value.Name);
        Assert.Equal(template.Key.ToString(), ok.Value.Key);
        Assert.Single(ok.Value.Sections);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}", ok.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/fields", ok.Value.Links.Fields.Href);
        Assert.Equal("/api/v1/content", ok.Value.Links.CreateItem.Href);
        Assert.Equal(new TemplateId(template.Id.Value), catalog.LastRequestedTemplateId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.GetByIdAsync(
                Guid.NewGuid(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await TemplateEndpoints.GetByIdAsync(
                Guid.Empty,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        var catalog =
            new FakeContentModelCatalog();
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Content",
                            Key = "content",
                            SortOrder = 100,
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Title",
                                    Key = "title",
                                    Type = "singleLineText",
                                    IsUnversioned = true
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);
        Assert.Equal("Article Page", repository.LastCreatedTemplate.Name);
        Assert.Equal("article-page", repository.LastCreatedTemplate.Key.ToString());
        Assert.Equal($"/api/v1/templates/{repository.LastCreatedTemplate.Id.Value}", created.Location);
        Assert.Equal("article-page", created.Value.Key);
        Assert.Single(created.Value.Sections);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignBaseTemplate_WhenBaseTemplateKeyIsProvided()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [baseTemplate]);
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys = ["base-page"],
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);
        Assert.NotNull(repository.LastCreatedTemplate.BaseTemplate);
        Assert.Equal(new TemplateKey("base-page"), repository.LastCreatedTemplate.BaseTemplate.Key);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenMultipleBaseTemplateKeysAreProvided()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys =
                    [
                        "base-page",
                        "metadata"
                    ],
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenTemplateKeyAlreadyExists()
    {
        var repository =
            new FakeTemplateRepository
            {
                OnCreateTemplateAsync = _ => throw new InvalidOperationException(
                    "Template key 'article-page' already exists.")
            };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections = []
                },
                repository,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenBaseTemplateIsMissing()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys = ["missing-base"],
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var catalog =
            new FakeContentModelCatalog
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "DuplicateFieldKeyInTemplate",
                            "Template 'article-page' contains multiple fields with key 'title'.",
                            "title")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Content",
                            Key = "content",
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Title",
                                    Key = "title",
                                    Type = "singleLineText"
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(new TemplateKey("article-page"), repository.LastDeletedTemplateKey);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsMissing()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                null,
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsInvalid()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = " ",
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenTemplateExists()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates: [template]),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(new TemplateKey("article-page"), repository.LastDeletedTemplateKey);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.DeleteAsync(
                Guid.NewGuid(),
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [existingTemplate]);
        var repository =
            new FakeTemplateRepository();
        repository.OnUpdateTemplateAsync = (_, template) =>
        {
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(repository.LastUpdatedTemplate);
        Assert.Equal(existingTemplate.Key, repository.LastUpdatedExistingKey);
        Assert.Equal(existingTemplate.Id, repository.LastUpdatedTemplate.Id);
        Assert.Equal("landing-page", repository.LastUpdatedTemplate.Key.ToString());
        Assert.Equal("landing-page", ok.Value.Key);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.UpdateAsync(
                Guid.NewGuid(),
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnConflict_WhenTemplateKeyAlreadyExists()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var repository =
            new FakeTemplateRepository
            {
                OnUpdateTemplateAsync = (_, _) => throw new InvalidOperationException(
                    "Template key 'landing-page' already exists.")
            };

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates: [existingTemplate]),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [existingTemplate])
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "DuplicateFieldKeyInTemplate",
                            "Template 'landing-page' contains multiple fields with key 'title'.",
                            "title")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.NotNull(repository.LastRollbackTemplate);
        Assert.Equal(existingTemplate.Key, repository.LastRollbackTemplate.Key);
        Assert.Equal(new TemplateKey("landing-page"), repository.LastRollbackExistingKey);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenTemplateIsBaseForAnotherTemplate()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var childTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page",
                baseTemplate);
        var repository =
            new FakeTemplateRepository(
                authoredTemplates:
                [
                    baseTemplate,
                    childTemplate
                ]);

        var result =
            await TemplateEndpoints.DeleteAsync(
                baseTemplate.Id.Value,
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates:
                    [
                        baseTemplate,
                        childTemplate
                    ]),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Null(repository.LastDeletedTemplateKey);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenContentItemsStillUseTemplate()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var contentRepository =
            new FakeContentRepository(
                [
                    CreateContentItem(template.Id)
                ]);

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                new FakeTemplateRepository(
                    authoredTemplates: [template]),
                new FakeContentModelCatalog(
                    authoredTemplates: [template]),
                contentRepository,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [template])
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "TemplateStillReferenced",
                            "Template 'article-page' is still referenced.",
                            "article-page")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                repository,
                catalog,
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.NotNull(repository.LastRecreatedTemplate);
        Assert.Equal(template.Key, repository.LastRecreatedTemplate.Key);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var template =
            CreateTemplate();
        var catalog =
            new FakeContentModelCatalog(
                template);

        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                template.Id.Value,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateFieldCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        var field = Assert.Single(ok.Value.Embedded.Fields);
        Assert.Equal("title", field.Key);
        Assert.Equal("SingleLineText", field.Type);
        Assert.Equal("content", field.SectionKey);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/fields", ok.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}", ok.Value.Links.Template.Href);
        Assert.Equal("/api/v1/content", ok.Value.Links.CreateItem.Href);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                Guid.NewGuid(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                Guid.Empty,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    private static EffectiveTemplateDefinition CreateTemplate()
    {
        return CreateTemplate(
            "Article Page",
            "article-page");
    }

    private static EffectiveTemplateDefinition CreateTemplate(
        string name,
        string key)
    {
        var field =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Title",
                "title",
                FieldType.SingleLineText,
                isUnversioned: true);
        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Content",
                "content",
                100,
                new[] { field });

        return new EffectiveTemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            name,
            new TemplateKey(key),
            new[] { section });
    }

    private static TemplateDefinition CreateAuthoredTemplate(
        string name,
        string key,
        TemplateDefinition? baseTemplate = null)
    {
        return new TemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            name,
            new TemplateKey(key),
            baseTemplate,
            []);
    }

    private static ContentItemDefinition CreateContentItem(
        TemplateId templateId)
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            templateId);
    }

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly Dictionary<TemplateId, TemplateDefinition> _authoredTemplates;
        private readonly Dictionary<TemplateId, EffectiveTemplateDefinition> _templates;

        public FakeContentModelCatalog(
            params EffectiveTemplateDefinition?[] templates)
            : this(
                Array.Empty<TemplateDefinition>(),
                templates)
        {
        }

        public FakeContentModelCatalog(
            IReadOnlyCollection<TemplateDefinition> authoredTemplates,
            params EffectiveTemplateDefinition?[] templates)
        {
            _authoredTemplates = (authoredTemplates ?? Array.Empty<TemplateDefinition>())
                .ToDictionary(template => template.Id);
            _templates = (templates ?? Array.Empty<EffectiveTemplateDefinition?>())
                .Where(template => template != null)
                .Cast<EffectiveTemplateDefinition>()
                .ToDictionary(template => template.Id);
        }

        public TemplateId? LastRequestedTemplateId { get; private set; }

        public int RefreshCallCount { get; private set; }

        public ContentModelCatalogRefreshException? RefreshException { get; init; }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            _authoredTemplates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            var template =
                _authoredTemplates.Values.FirstOrDefault(
                    value => value.Key == key);

            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            LastRequestedTemplateId = id;
            _templates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            var template =
                _templates.Values.FirstOrDefault(
                    value => value.Key == key);

            return Task.FromResult(template);
        }

        public Task<IReadOnlyCollection<EffectiveTemplateDefinition>> GetEffectiveTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            var templates =
                _templates.Values
                    .OrderBy(template => template.Key.ToString(), StringComparer.Ordinal)
                    .ToArray();

            return Task.FromResult<IReadOnlyCollection<EffectiveTemplateDefinition>>(templates);
        }

        public void AddTemplate(
            EffectiveTemplateDefinition template)
        {
            _templates[template.Id] = template;
        }

        public void AddAuthoredTemplate(
            TemplateDefinition template)
        {
            _authoredTemplates[template.Id] = template;
        }

        public Task InvalidateAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RefreshAsync(
            CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;

            if (RefreshException != null)
            {
                throw RefreshException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
    {
        private readonly IReadOnlyCollection<TemplateDefinition> _authoredTemplates;

        public FakeTemplateRepository(
            IReadOnlyCollection<TemplateDefinition>? authoredTemplates = null)
        {
            _authoredTemplates = authoredTemplates ?? [];
        }

        public Func<TemplateDefinition, Task>? OnCreateTemplateAsync { get; set; }

        public Func<TemplateKey, TemplateDefinition, Task>? OnUpdateTemplateAsync { get; set; }

        public TemplateDefinition? LastCreatedTemplate { get; private set; }

        public TemplateDefinition? LastRecreatedTemplate { get; private set; }

        public TemplateKey? LastUpdatedExistingKey { get; private set; }

        public TemplateDefinition? LastUpdatedTemplate { get; private set; }

        public TemplateKey? LastRollbackExistingKey { get; private set; }

        public TemplateDefinition? LastRollbackTemplate { get; private set; }

        public TemplateKey? LastDeletedTemplateKey { get; private set; }

        public Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_authoredTemplates);
        }

        public Task CreateTemplateAsync(
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            if (LastDeletedTemplateKey == null)
            {
                LastCreatedTemplate = template;
            }
            else
            {
                LastRecreatedTemplate = template;
            }

            return OnCreateTemplateAsync == null
                ? Task.CompletedTask
                : OnCreateTemplateAsync(template);
        }

        public Task UpdateTemplateAsync(
            TemplateKey existingKey,
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            if (LastUpdatedExistingKey == null)
            {
                LastUpdatedExistingKey = existingKey;
                LastUpdatedTemplate = template;
            }
            else
            {
                LastRollbackExistingKey = existingKey;
                LastRollbackTemplate = template;
            }

            return OnUpdateTemplateAsync == null
                ? Task.CompletedTask
                : OnUpdateTemplateAsync(existingKey, template);
        }

        public Task DeleteTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            LastDeletedTemplateKey = key;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContentRepository : IContentRepository
    {
        private readonly IReadOnlyCollection<ContentItemDefinition> _items;

        public FakeContentRepository(
            IReadOnlyCollection<ContentItemDefinition>? items = null)
        {
            _items = items ?? [];
        }

        public Task<ContentItemDefinition?> GetItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ContentItemDefinition?> GetItemAsync(
            ContentPath path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
            ContentItemId? parentId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ContentItemDefinition>> GetItemsByTemplateAsync(
            TemplateId templateId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ContentItemDefinition>>(
                _items.Where(item => item.TemplateId == templateId).ToArray());
        }

        public Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
