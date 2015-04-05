using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configuratively
{
    public static class Helpers
    {

        public static IEnumerable<dynamic> ReadAllJsonFiles(IEnumerable<string> files, string basePath, string repoRoot = @"configuration\jsonRepo\")
        {
            var jsonBasePath = Path.Combine(basePath, repoRoot);
            if (!jsonBasePath.EndsWith(@"\"))
            {
                jsonBasePath = string.Format(@"{0}\", jsonBasePath);
            }

            // Create a dynamic object based on each JSON file, for later parsing
            var jsonFiles = files.Where(
                    f => f.StartsWith(jsonBasePath, StringComparison.InvariantCultureIgnoreCase)
                        && Path.GetExtension(f).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                .Select(t => GetDynamicFromJson(new FileInfo(t), jsonBasePath));

            return jsonFiles;
        }



        private static dynamic GetDynamicFromJson(FileInfo fileInfo, string basePath)
        {
            try
            {
                var jr = new JsonFx.Json.JsonReader();
                dynamic obj = jr.Read(Encoding.ASCII.GetString(File.ReadAllBytes(fileInfo.FullName)));

                // generate an identifier
                obj._id = GetIdFromFileInfo(fileInfo, basePath);
                obj._isLinksResolved = false;

                return obj;
            }
            catch (Exception e)
            {
                return new
                {
                    error = e.ToString()
                };
            }
        }

        private static string GetIdFromFileInfo(FileInfo fileInfo, string basePath)
        {
            return fileInfo.FullName.Replace(basePath, "").Replace(@"\", "/");
        }


    }
}
