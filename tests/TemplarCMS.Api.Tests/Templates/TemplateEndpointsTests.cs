using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Templates;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Templates;

public sealed class TemplateEndpointsTests
{
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
                new FakeContentModelCatalog(null),
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
                new FakeContentModelCatalog(null),
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
                new FakeContentModelCatalog(null),
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
                new FakeContentModelCatalog(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    private static EffectiveTemplateDefinition CreateTemplate()
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
            "Article Page",
            new TemplateKey("article-page"),
            new[] { section });
    }

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly EffectiveTemplateDefinition? _template;

        public FakeContentModelCatalog(
            EffectiveTemplateDefinition? template)
        {
            _template = template;
        }

        public TemplateId? LastRequestedTemplateId { get; private set; }

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
            return Task.FromResult(_template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task InvalidateAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RefreshAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
