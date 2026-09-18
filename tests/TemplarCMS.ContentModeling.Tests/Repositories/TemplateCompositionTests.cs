using TemplarCMS.ContentModeling.Builders;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Resolvers;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Repositories;

public sealed class TemplateCompositionTests
{
    [Fact]
    public async Task Standard_ShouldComposeSystemSections_AndTemplateShouldInheritStandard()
    {
        var templates = new BuiltInTemplateProvider().GetTemplates();
        Assert.DoesNotContain(templates, t => t.Key.ToString() == "item");
        var standard = Assert.Single(templates, t => t.Key.ToString() == "standard");
        Assert.Equal(new[] { "advanced", "appearance", "help", "lifetime", "publishing", "statistics", "tasks", "version" },
            standard.BaseTemplates.Select(t => t.Key.ToString()));
        Assert.All(standard.BaseTemplates, t => Assert.Single(t.Sections));
        var template = Assert.Single(templates, t => t.Key.ToString() == "template");
        Assert.All(templates, t => Assert.Equal(template.Id, t.TemplateId));
        Assert.IsAssignableFrom<TemplarCMS.Domain.Content.Item>(template);
        Assert.Same(standard, Assert.Single(template.BaseTemplates));
        var result = await new EffectiveTemplateBuilder(new TemplateInheritanceResolver())
            .BuildEffectiveTemplateAsync(template, TestContext.Current.CancellationToken);
        Assert.True(result.IsValid);
        Assert.Contains(result.Value!.Sections.SelectMany(s => s.Fields), f => f.Key == "__created");
        Assert.Contains(result.Value.Sections.SelectMany(s => s.Fields), f => f.Key == "__publish");
    }

    [Fact]
    public async Task Page_ShouldExposeAdminSectionsTogetherWithLocalFields()
    {
        var standard = new BuiltInTemplateProvider().GetTemplates().Single(t => t.Key.ToString() == "standard");
        var page = BuiltInTemplateProvider.CreateStarterPage(standard);
        var result = await new EffectiveTemplateBuilder(new TemplateInheritanceResolver())
            .BuildEffectiveTemplateAsync(page, TestContext.Current.CancellationToken);
        Assert.True(result.IsValid);
        Assert.Same(standard, Assert.Single(page.BaseTemplates));
        Assert.Equal(9, result.Value!.Sections.Count);
        Assert.Contains(result.Value.Sections.SelectMany(s => s.Fields), f => f.Key == "body");
        Assert.Contains(result.Value.Sections.SelectMany(s => s.Fields), f => f.Key == "__created");
    }
}
