using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Tests
{
    internal static class TestTemplateFactory
    {
        public static TemplateDefinition Create(
            string name,
            string key,
            params TemplateDefinition[] baseTemplates)
        {
            return new TemplateDefinition(
                Guid.NewGuid(),
                name,
                key,
                baseTemplates);
        }
    }
}
