using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Tests.TestUtilities
{
    internal sealed class TemplateDefinitionBuilder
    {
        private TemplateId _id = new(Guid.NewGuid());
        private string _name = "Test Template";
        private TemplateKey _key = new("test-template");

        private TemplateDefinition? _baseTemplate;
        private readonly List<TemplateSectionDefinition> _sections = new List<TemplateSectionDefinition>();

        public TemplateDefinitionBuilder WithId(TemplateId id)
        {
            _id = id;
            return this;
        }

        public TemplateDefinitionBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public TemplateDefinitionBuilder WithKey(string key)
        {
            _key = new TemplateKey(key);
            return this;
        }

        public TemplateDefinitionBuilder WithNameAndKey(
        string name,
        string key)
        {
            _name = name;
            _key = new TemplateKey(key);
            return this;
        }

        public TemplateDefinitionBuilder WithBaseTemplate(TemplateDefinition template)
        {
            _baseTemplate = template;
            return this;
        }

        public TemplateDefinitionBuilder WithSection(TemplateSectionDefinition section)
        {
            _sections.Add(section);
            return this;
        }

        public TemplateDefinition Build()
        {
            return new TemplateDefinition(
                _id,
                _name,
                _key,
                _baseTemplate,
                _sections);
        }
    }
}
