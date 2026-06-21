using Microsoft.Extensions.Logging;
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
        var cancellationToken = TestContext.Current.CancellationToken;

        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync(cancellationToken);

        var actualTemplate = await context.Catalog.GetTemplateAsync(template.Id, cancellationToken);
        var actualEffectiveTemplate = await context.Catalog.GetEffectiveTemplateAsync(template.Id, cancellationToken);

        Assert.Same(template, actualTemplate);
        Assert.Same(effectiveTemplate, actualEffectiveTemplate);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsTemplate_WhenKeyMatchesDifferentCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync(cancellationToken);

        var actual = await context.Catalog.GetTemplateAsync("ARTICLE", cancellationToken);

        Assert.Same(template, actual);
    }

    [Fact]
    public async Task GetEffectiveTemplateAsync_ReturnsEffectiveTemplate_WhenKeyMatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync(cancellationToken);

        var actual = await context.Catalog.GetEffectiveTemplateAsync("article", cancellationToken);

        Assert.Same(effectiveTemplate, actual);
    }

    [Fact]
    public async Task InvalidateAsync_ClearsPublishedSnapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync(cancellationToken);
        await context.Catalog.InvalidateAsync(cancellationToken);

        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id, cancellationToken));
        Assert.Null(await context.Catalog.GetEffectiveTemplateAsync(template.Id, cancellationToken));
    }

    [Fact]
    public async Task RefreshAsync_ThrowsAndDoesNotBuildEffectiveTemplates_WhenAuthoringValidationFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var error = new ValidationError(
            "DuplicateFieldKeyInTemplate",
            "Duplicate field key detected.",
            "article");

        var context = CreateContext();

        context.TemplateRepository
            .GetTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<TemplateDefinition>>(
                [template]));

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { error })));

        var exception = await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync(cancellationToken));

        var actualError = Assert.Single(exception.Errors);

        Assert.Equal("DuplicateFieldKeyInTemplate", actualError.Code);

        await context.EffectiveTemplateBuilder
            .DidNotReceive()
            .BuildEffectiveTemplateAsync(Arg.Any<TemplateDefinition>(), Arg.Any<CancellationToken>());

        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id, cancellationToken));
    }

    [Fact]
    public async Task RefreshAsync_DoesNotReplaceExistingSnapshot_WhenAuthoringValidationFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var effectiveTemplate = CreateEffectiveTemplate(template);

        var context = CreateContext(template, effectiveTemplate);

        await context.Catalog.RefreshAsync(cancellationToken);

        var error = new ValidationError(
            "DuplicateFieldKeyInTemplate",
            "Duplicate field key detected.",
            "article");

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { error })));

        await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync(cancellationToken));

        var actual = await context.Catalog.GetTemplateAsync(template.Id, cancellationToken);

        Assert.Same(template, actual);
    }

    [Fact]
    public async Task RefreshAsync_ThrowsAndDoesNotPublish_WhenEffectiveTemplateBuildFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");
        var error = new ValidationError(
            "CircularTemplateInheritance",
            "Circular template inheritance detected.",
            "article");

        var context = CreateContext();

        context.TemplateRepository
            .GetTemplatesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<TemplateDefinition>>(
                [template]));

        context.TemplateValidator
            .ValidateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult()));

        context.EffectiveTemplateBuilder
            .BuildEffectiveTemplateAsync(template, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult<EffectiveTemplateDefinition>(
                null,
                [error])));

        var exception = await Assert.ThrowsAsync<ContentModelCatalogRefreshException>(
            () => context.Catalog.RefreshAsync(cancellationToken));

        var actualError = Assert.Single(exception.Errors);

        Assert.Equal("CircularTemplateInheritance", actualError.Code);
        Assert.Null(await context.Catalog.GetTemplateAsync(template.Id, cancellationToken));
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
                    [template]));

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
            effectiveTemplateValidator,
            Substitute.For<ILogger<ContentModelCatalog>>());

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
