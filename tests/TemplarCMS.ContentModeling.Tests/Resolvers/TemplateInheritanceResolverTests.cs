using TemplarCMS.ContentModeling.Resolvers;
using TemplarCMS.ContentModeling.Tests.TestUtilities;
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
                item => Assert.Equal("base-seo", item.Key),
                item => Assert.Equal("article", item.Key));
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
                item => Assert.Equal("base-content", item.Key),
                item => Assert.Equal("base-seo", item.Key),
                item => Assert.Equal("article", item.Key));
        }
    }
}
