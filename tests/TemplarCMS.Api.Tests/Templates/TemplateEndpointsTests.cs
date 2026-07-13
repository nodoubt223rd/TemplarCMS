using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly Dictionary<TemplateId, EffectiveTemplateDefinition> _templates;

        public FakeContentModelCatalog(
            params EffectiveTemplateDefinition?[] templates)
        {
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
            throw new NotSupportedException();
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
        public Func<TemplateDefinition, Task>? OnCreateTemplateAsync { get; set; }

        public TemplateDefinition? LastCreatedTemplate { get; private set; }

        public TemplateKey? LastDeletedTemplateKey { get; private set; }

        public Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CreateTemplateAsync(
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            LastCreatedTemplate = template;

            return OnCreateTemplateAsync == null
                ? Task.CompletedTask
                : OnCreateTemplateAsync(template);
        }

        public Task DeleteTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            LastDeletedTemplateKey = key;
            return Task.CompletedTask;
        }
    }
}
