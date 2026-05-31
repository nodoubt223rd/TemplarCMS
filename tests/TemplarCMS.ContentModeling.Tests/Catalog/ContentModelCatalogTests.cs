using NSubstitute;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Validation;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Catalog;

public sealed class ContentModelCatalogTests
{
    [Fact]
    public async Task RefreshAsync_PublishesTemplates_WhenRefreshSucceeds()
    {
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync();

        var actualTemplate = await context.Catalog.GetTemplateAsync(template.Id);
        var actualEffectiveTemplate = await context.Catalog.GetEffectiveTemplateAsync(template.Id);

        Assert.Same(template, actualTemplate);
        Assert.Same(effectiveTemplate, actualEffectiveTemplate);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsTemplate_WhenKeyMatchesDifferentCasing()
    {
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync();

        var actual = await context.Catalog.GetTemplateAsync("ARTICLE");

        Assert.Same(template, actual);
    }

    [Fact]
    public async Task GetEffectiveTemplateAsync_ReturnsEffectiveTemplate_WhenKeyMatches()
    {
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync();

        var actual = await context.Catalog.GetEffectiveTemplateAsync("article");

        Assert.Same(effectiveTemplate, actual);
    }

    [Fact]
    public async Task InvalidateAsync_ClearsPublishedSnapshot()
    {
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync();
        await context.Catalog.InvalidateAsync();

        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id));
        Assert.Null(await context.Catalog.GetEffectiveTemplateAsync(template.Id));
    }

    [Fact]
    public async Task RefreshAsync_ThrowsAndDoesNotBuildEffectiveTemplates_WhenAuthoringValidationFails()
    {
        var template = CreateTemplate("Article", "article");
        var error = new ValidationError(
            "DuplicateFieldKeyInTemplate",
            "Duplicate field key detected.",
            "article");

        var context = CreateContext();

        context.TemplateRepository
            .GetTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<TemplateDefinition>>(
                new[] { template }));

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { error })));

        var exception = await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync());

        var actualError = Assert.Single(exception.Errors);

        Assert.Equal("DuplicateFieldKeyInTemplate", actualError.Code);

        await context.EffectiveTemplateBuilder
            .DidNotReceive()
            .BuildEffectiveTemplateAsync(Arg.Any<TemplateDefinition>(), Arg.Any<CancellationToken>());

        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id));
    }

    [Fact]
    public async Task RefreshAsync_DoesNotReplaceExistingSnapshot_WhenAuthoringValidationFails()
    {
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync();

        var error = new ValidationError(
            "DuplicateFieldKeyInTemplate",
            "Duplicate field key detected.",
            "article");

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { error })));

        await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync());

        var actual = await context.Catalog.GetTemplateAsync(template.Id);

        Assert.Same(template, actual);
    }

    [Fact]
    public async Task RefreshAsync_ThrowsAndDoesNotPublish_WhenEffectiveTemplateBuildFails()
    {
        var template = CreateTemplate("Article", "article");
        var error = new ValidationError(
            "CircularTemplateInheritance",
            "Circular template inheritance detected.",
            "article");

        var context = CreateContext();

        context.TemplateRepository
            .GetTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<TemplateDefinition>>(
                new[] { template }));

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult()));

        context.EffectiveTemplateBuilder
            .BuildEffectiveTemplateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult<EffectiveTemplateDefinition>(
                null,
                new[] { error })));

        var exception = await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync());

        var actualError = Assert.Single(exception.Errors);

        Assert.Equal("CircularTemplateInheritance", actualError.Code);
        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id));
    }

    private static CatalogTestContext CreateContext(
        TemplateDefinition? template = null,
        EffectiveTemplateDefinition? effectiveTemplate = null)
    {
        var templateRepository = Substitute.For<ITemplateRepository>();
        var templateValidator = Substitute.For<ITemplateValidator>();
        var effectiveTemplateBuilder = Substitute.For<IEffectiveTemplateBuilder>();
        var effectiveTemplateValidator = Substitute.For<IEffectiveTemplateValidator>();

        if (template is not null)
        {
            templateRepository
                .GetTemplatesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyCollection<TemplateDefinition>>(
                    new[] { template }));

            templateValidator
                .ValidateAsync(template, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ValidationResult()));
        }

        if (template is not null && effectiveTemplate is not null)
        {
            effectiveTemplateBuilder
                .BuildEffectiveTemplateAsync(template, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ValidationResult<EffectiveTemplateDefinition>(
                    effectiveTemplate)));

            effectiveTemplateValidator
                .ValidateAsync(effectiveTemplate, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ValidationResult()));
        }

        var catalog = new ContentModelCatalog(
            templateRepository,
            templateValidator,
            effectiveTemplateBuilder,
            effectiveTemplateValidator);

        return new CatalogTestContext(
            catalog,
            templateRepository,
            templateValidator,
            effectiveTemplateBuilder,
            effectiveTemplateValidator);
    }

    private static TemplateDefinition CreateTemplate(
        string name,
        string key)
    {
        return new TemplateDefinition(
            Guid.NewGuid(),
            name,
            key);
    }

    private static EffectiveTemplateDefinition CreateEffectiveTemplate(
        TemplateDefinition template)
    {
        return new EffectiveTemplateDefinition(
            template.Id,
            template.Name,
            template.Key,
            Array.Empty<TemplateSectionDefinition>());
    }

    private sealed record CatalogTestContext(
        ContentModelCatalog Catalog,
        ITemplateRepository TemplateRepository,
        ITemplateValidator TemplateValidator,
        IEffectiveTemplateBuilder EffectiveTemplateBuilder,
        IEffectiveTemplateValidator EffectiveTemplateValidator);
}
