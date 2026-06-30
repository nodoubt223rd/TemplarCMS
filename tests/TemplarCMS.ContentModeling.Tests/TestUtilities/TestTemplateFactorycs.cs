using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

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
                new TemplateId(Guid.NewGuid()),
                name,
                new TemplateKey(key),
                baseTemplate);
        }
    }
}
