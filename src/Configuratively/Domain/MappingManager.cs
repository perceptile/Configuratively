using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Configuratively.Domain
{
    public class MappingManager
    {
        private ConfigurationModel _model { get; set; }

        public List<ConfigurationEntity> Entities
        {
            get { return _model.Entities; }
        }

        public List<ConfigurationQuery> Queries
        {
            get { return _model.Queries; }
        }

        public MappingManager()
        {
            var repoPath = Path.GetFullPath(ConfigurationManager.AppSettings["repoPath"]);
            var mappingFile = Path.Combine(repoPath, ConfigurationManager.AppSettings["mappingFile"]);

            _model = JsonConvert.DeserializeObject<ConfigurationModel>(Encoding.ASCII.GetString(File.ReadAllBytes(mappingFile)));
        }
    }
}
