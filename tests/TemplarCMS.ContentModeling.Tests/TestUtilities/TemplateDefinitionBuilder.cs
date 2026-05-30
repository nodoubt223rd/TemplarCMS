using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Tests.TestUtilities
{
    internal sealed class TemplateDefinitionBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _name = "Test Template";
        private string _key = "test-template";

        private readonly List<TemplateDefinition> _baseTemplates = new List<TemplateDefinition>();
        private readonly List<TemplateSectionDefinition> _sections = new List<TemplateSectionDefinition>();

        public TemplateDefinitionBuilder WithId(Guid id)
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
            _key = key;
            return this;
        }

        public TemplateDefinitionBuilder WithNameAndKey(
        string name,
        string key)
            {
                _name = name;
                _key = key;
                return this;
            }

        public TemplateDefinitionBuilder WithBaseTemplate(TemplateDefinition template)
        {
            _baseTemplates.Add(template);
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
                _baseTemplates,
                _sections);
        }
    }
}
