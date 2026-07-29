using NSubstitute;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Repositories;

public sealed class BuiltInTemplateRepositoryTests
{
    [Fact]
    public async Task GetTemplatesAsync_ShouldMergeBuiltInAndMutableTemplates()
    {
        var mutableTemplate =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article Page",
                new TemplateKey("article-page"));
        var innerRepository =
            Substitute.For<ITemplateRepository>();
        innerRepository
            .GetTemplatesAsync(TestContext.Current.CancellationToken)
            .Returns([mutableTemplate]);

        var repository =
            new BuiltInTemplateRepository(
                innerRepository,
                new BuiltInTemplateProvider());

        var templates =
            await repository.GetTemplatesAsync(TestContext.Current.CancellationToken);

        Assert.Contains(templates, template => template.Key == BuiltInTemplateKeys.Standard);
        Assert.Contains(templates, template => template.Key == BuiltInTemplateKeys.Folder);
        Assert.Contains(templates, template => template.Key == BuiltInTemplateKeys.Item);
        Assert.Contains(templates, template => template.Key == mutableTemplate.Key);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldExposeWaveOneStandardSections()
    {
        var repository =
            new BuiltInTemplateRepository(
                Substitute.For<ITemplateRepository>(),
                new BuiltInTemplateProvider());

        var templates =
            await repository.GetTemplatesAsync(TestContext.Current.CancellationToken);

        var standardTemplate =
            Assert.Single(
                templates,
                template => template.Key == BuiltInTemplateKeys.Standard);

        Assert.Equal(
            [
                "content",
                "appearance",
                "help",
                "lifetime",
                "publishing",
                "statistics",
                "version"
            ],
            standardTemplate.Sections
                .OrderBy(section => section.SortOrder)
                .Select(section => section.Key)
                .ToArray());

        var appearanceSection =
            Assert.Single(
                standardTemplate.Sections,
                section => section.Key == "appearance");
        Assert.Contains(
            appearanceSection.Fields,
            field => field.Key == "__displayName" && field.IsUnversioned);
        Assert.Contains(
            appearanceSection.Fields,
            field => field.Key == "__hidden" && field.IsShared);

        var helpSection =
            Assert.Single(
                standardTemplate.Sections,
                section => section.Key == "help");
        Assert.Contains(
            helpSection.Fields,
            field => field.Key == "__helpLink" && field.IsUnversioned);

        var publishingSection =
            Assert.Single(
                standardTemplate.Sections,
                section => section.Key == "publishing");
        Assert.Contains(
            publishingSection.Fields,
            field => field.Key == "__publish" && field.IsVersioned);

        var statisticsSection =
            Assert.Single(
                standardTemplate.Sections,
                section => section.Key == "statistics");
        Assert.Contains(
            statisticsSection.Fields,
            field => field.Key == "__created" && field.IsShared);

        Assert.All(
            standardTemplate.Sections,
            section =>
                Assert.Equal(
                    SectionVisibilityMetadata.SystemValue,
                    section.Metadata[SectionVisibilityMetadata.VisibilityKey]));
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldRejectBuiltInTemplateKey()
    {
        var repository =
            new BuiltInTemplateRepository(
                Substitute.For<ITemplateRepository>(),
                new BuiltInTemplateProvider());
        var template =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Standard Clone",
                BuiltInTemplateKeys.Standard);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.CreateTemplateAsync(
                    template,
                    TestContext.Current.CancellationToken));

        Assert.Contains("reserved by a built-in system template", exception.Message);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldRejectBuiltInTemplate()
    {
        var repository =
            new BuiltInTemplateRepository(
                Substitute.For<ITemplateRepository>(),
                new BuiltInTemplateProvider());
        var template =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Standard",
                BuiltInTemplateKeys.Standard);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.UpdateTemplateAsync(
                    BuiltInTemplateKeys.Standard,
                    template,
                    TestContext.Current.CancellationToken));

        Assert.Contains("source-controlled", exception.Message);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldRejectBuiltInTemplate()
    {
        var repository =
            new BuiltInTemplateRepository(
                Substitute.For<ITemplateRepository>(),
                new BuiltInTemplateProvider());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.DeleteTemplateAsync(
                    BuiltInTemplateKeys.Standard,
                    TestContext.Current.CancellationToken));

        Assert.Contains("source-controlled", exception.Message);
    }
}
