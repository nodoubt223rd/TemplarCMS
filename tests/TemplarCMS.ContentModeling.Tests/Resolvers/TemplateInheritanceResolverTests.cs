using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplarCMS.ContentModeling.Definitions;
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
                new[] { baseTemplate });

            var sut = new TemplateInheritanceResolver();

            var result = await sut.ResolveAsync(articleTemplate, cancellationToken);

            Assert.True(result.Succeeded);

            Assert.Collection(
                result.Value!,
                item => Assert.Equal("base-seo", item.Key),
                item => Assert.Equal("article", item.Key));
        }

        [Fact]
        public async Task ResolveAsync_ReturnsResolvedChain_ForMultipleBaseTemplates()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var seoTemplate = new TemplateDefinitionBuilder()
                .WithNameAndKey("Base SEO", "base-seo")
                .Build();

            var auditingTemplate = new TemplateDefinitionBuilder()
                .WithNameAndKey("Base Auditing", "base-auditing")
                .Build();


            var articleTemplate = TestTemplateFactory.Create(
                "Article",
                "article",
                new[]
                {
            seoTemplate,
            auditingTemplate
                });

            var sut = new TemplateInheritanceResolver();

            var result = await sut.ResolveAsync(articleTemplate, cancellationToken);

            Assert.True(result.Succeeded);

            Assert.Collection(
                result.Value!,
                item => Assert.Equal("base-seo", item.Key),
                item => Assert.Equal("base-auditing", item.Key),
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
                new[] { baseContent });

            var article = TestTemplateFactory.Create(
                "Article",
                "article",
                new[] { baseSeo });

            var sut = new TemplateInheritanceResolver();

            var result = await sut.ResolveAsync(article, cancellationToken);

            Assert.True(result.Succeeded);

            Assert.Collection(
                result.Value!,
                item => Assert.Equal("base-content", item.Key),
                item => Assert.Equal("base-seo", item.Key),
                item => Assert.Equal("article", item.Key));
        }
    }
}
