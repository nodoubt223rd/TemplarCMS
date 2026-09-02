using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Resolvers;
using TemplarCMS.ContentModeling.Tests.TestUtilities;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Resolvers
{
    public class TemplateInheritanceResolverTests
    {
        [Fact]
        public async Task ResolveAsync_ReturnsResolvedChain_ForSimpleInheritance()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var baseTemplate = new TemplateDefinitionBuilder()
                .WithNameAndKey("Base SEO", "base-seo")
                .Build();

            var articleTemplate = TestTemplateFactory.Create(
                "Article",
                "article",
                baseTemplate);

            var sut = new TemplateInheritanceResolver();

            var result = await sut.ResolveAsync(articleTemplate, cancellationToken);

            Assert.True(result.Succeeded);

            Assert.Collection(
                result.Value!.InheritanceChain,
                item => Assert.Equal(new TemplateKey("base-seo"), item.Key),
                item => Assert.Equal(new TemplateKey("article"), item.Key));
        }

        [Fact]
        public async Task ResolveAsync_ReturnsResolvedChain_ForDeepInheritance()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var baseContent = new TemplateDefinitionBuilder()
            .WithNameAndKey("Base Content", "base-content")
            .Build();

            var baseSeo = TestTemplateFactory.Create(
                "Base SEO",
                "base-seo",
                baseContent);

            var article = TestTemplateFactory.Create(
                "Article",
                "article",
                baseSeo);

            var sut = new TemplateInheritanceResolver();

            var result = await sut.ResolveAsync(article, cancellationToken);

            Assert.True(result.Succeeded);

            Assert.Collection(
                result.Value!.InheritanceChain,
                item => Assert.Equal(new TemplateKey("base-content"), item.Key),
                item => Assert.Equal(new TemplateKey("base-seo"), item.Key),
                item => Assert.Equal(new TemplateKey("article"), item.Key));
        }

        [Fact]
        public async Task ResolveAsync_ReturnsOrderedChain_ForMultipleBaseTemplates()
        {
            var firstBase = new TemplateDefinitionBuilder()
                .WithNameAndKey("Base Content", "base-content")
                .Build();
            var secondBase = new TemplateDefinitionBuilder()
                .WithNameAndKey("Base Metadata", "base-metadata")
                .Build();
            var article = new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article",
                new TemplateKey("article"),
                baseTemplates: [firstBase, secondBase]);

            var result = await new TemplateInheritanceResolver()
                .ResolveAsync(article, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Collection(
                result.Value!.InheritanceChain,
                item => Assert.Equal(new TemplateKey("base-content"), item.Key),
                item => Assert.Equal(new TemplateKey("base-metadata"), item.Key),
                item => Assert.Equal(new TemplateKey("article"), item.Key));
        }

        [Fact]
        public async Task ResolveAsync_ResolvesDiamondInheritance_WithoutDuplicatingTheSharedAncestor()
        {
            var sharedBase = new TemplateDefinitionBuilder()
                .WithNameAndKey("Shared", "shared")
                .Build();
            var contentBase = TestTemplateFactory.Create("Content", "content", sharedBase);
            var metadataBase = TestTemplateFactory.Create("Metadata", "metadata", sharedBase);
            var article = new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article",
                new TemplateKey("article"),
                baseTemplates: [contentBase, metadataBase]);

            var result = await new TemplateInheritanceResolver()
                .ResolveAsync(article, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Collection(
                result.Value!.InheritanceChain,
                item => Assert.Equal(new TemplateKey("shared"), item.Key),
                item => Assert.Equal(new TemplateKey("content"), item.Key),
                item => Assert.Equal(new TemplateKey("metadata"), item.Key),
                item => Assert.Equal(new TemplateKey("article"), item.Key));
        }
    }
}
