using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Tests.TestUtilities
{
    internal static class TestTemplateFactory
    {
        public static TemplateDefinition Create(
            string name,
            string key,
            TemplateDefinition baseTemplate)
        {
            return new TemplateDefinition(
                Guid.NewGuid(),
                name,
                key,
                baseTemplate);
        }
    }
}
