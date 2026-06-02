using Microsoft.Extensions.Options;
using TemplarCMS.ContentModeling.Serialization;

namespace TemplarCMS.ContentModeling.Repositories
{
    public class JsonTemplateRepository
    {
        public JsonTemplateRepository(IOptions<JsonTemplateRepositoryOptions> options, IJsonTemplateMapper mapper)
        {

        }
    }
}
