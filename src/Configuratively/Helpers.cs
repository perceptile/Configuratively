using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using JsonFx.Json;
using JsonFx.Serialization;

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
                .Select(t => GetDynamicWithRouteFromJson(new FileInfo(t), jsonBasePath));

            return jsonFiles;
        }



        private static dynamic GetDynamicWithRouteFromJson(FileInfo fileInfo, string basePath)
        {
            var jr = new JsonReader();
            dynamic obj;

            try
            {
                obj = jr.Read(Encoding.ASCII.GetString(File.ReadAllBytes(fileInfo.FullName)));
            }
            catch (DeserializationException e)
            {
                obj = new ExpandoObject();
                obj._error = string.Format("{0} {{{1},{2}}} at {3}", e.Message, e.Line, e.Column, fileInfo.FullName);
            }

            // generate an identifier
            obj._id = GetIdFromFileInfo(fileInfo, basePath);
            obj._isLinksResolved = false;

            obj._route = obj._id.Replace(fileInfo.Extension, string.Empty);

            return obj;
        }

        private static string GetIdFromFileInfo(FileInfo fileInfo, string basePath)
        {
            return fileInfo.FullName.Replace(basePath, string.Empty).Replace(@"\", "/");
        }

        public static bool HasProperty(dynamic obj, string name)
        {
            var entry = (ExpandoObject)obj;
            return entry.Any(x => x.Key == name);
        }
    }
}
