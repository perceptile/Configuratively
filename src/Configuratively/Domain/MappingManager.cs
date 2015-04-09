using System.Collections.Generic;
using System.IO;
using System.Text;
using Configuratively.Hosting;
using Newtonsoft.Json;

namespace Configuratively.Domain
{
    public class MappingManager
    {
        private ConfigurationModel Model { get; set; }

        public List<ConfigurationEntity> Entities
        {
            get { return Model.Entities; }
        }

        public List<ConfigurationQuery> Queries
        {
            get { return Model.Queries; }
        }

        public MappingManager(ConfigSettings settings)
        {
            var repoPath = Path.GetFullPath(settings.RepositoryPath);
            var mappingFile = Path.Combine(repoPath, settings.MappingFile);

            Model = JsonConvert.DeserializeObject<ConfigurationModel>(Encoding.ASCII.GetString(File.ReadAllBytes(mappingFile)));
        }
    }
}
