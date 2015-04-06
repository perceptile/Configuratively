using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using JsonFx.Json;

namespace Configuratively.Domain
{
    public class MappingManager
    {
        public IDictionary<string, string> Entities
        {
            get;
            private set;
        }

        public MappingManager()
        {
            var repoPath = Path.GetFullPath(ConfigurationManager.AppSettings["repoPath"]);
            var mappingFile = Path.Combine(repoPath, ConfigurationManager.AppSettings["mappingFile"]);

            var jr = new JsonReader();
            dynamic mappings = jr.Read(Encoding.ASCII.GetString(File.ReadAllBytes(mappingFile)));
            Entities = new Dictionary<string, string>();
            foreach (var m in mappings)
            {
                Entities.Add(m.Key, m.Value);
            }
        }
    }
}
