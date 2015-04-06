using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using Configuratively.Domain;
using Configuratively.Repositories;
using Nancy;
using Nancy.Json;

namespace Configuratively.Api
{
    public class ConfigurationModule : NancyModule
    {
        public ConfigurationModule()
        {
            JsonSettings.MaxJsonLength = 5000000;
            JsonSettings.RetainCasing = true;

            var routes = GetConfigurationRoutes();

            foreach (var route in routes)
            {
                Get[route.Key] = _ => Response.AsJson((ExpandoObject)route.Value);
            }
        }

        public static Dictionary<string, dynamic> GetConfigurationRoutes()
        {
            var entities = (new MappingManager()).Entities;
            var hostUri = ConfigurationManager.AppSettings["hostUri"];

            // Generate the root endpoint
            var resources = entities.Keys.Select(k => string.Format("{0}/{1}", hostUri, k)).ToArray();

            Dictionary<string, dynamic> routes = new Dictionary<string, dynamic>();

            routes["/"] = new {resources};

            // Generate an endpoint for each entity
            foreach (var e in entities.Keys)
            {
                var url = string.Format("/{0}", e);
                IDictionary<string, object> ret = new ExpandoObject();
                var items = InMemoryRepository.Get(e) as IEnumerable<dynamic>;
                ret.Add(e, items);

                routes[url] = ret;

                foreach (var item in items)
                {
                    routes[item._route] = item;
                }
            }
            return routes;
        }
    }
}