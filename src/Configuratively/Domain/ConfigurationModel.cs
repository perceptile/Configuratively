using System.Collections.Generic;

namespace Configuratively.Domain
{
    public class ConfigurationModel
    {
        public List<ConfigurationEntity> Entities { get; set; }

        public List<ConfigurationQuery> Queries { get; set; }
    }
}
