using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using Configuratively.Domain;
using Configuratively.Repositories;
using Nancy;
using Nancy.Json;

namespace Configuratively.Api
{
    public class ConfigurationModule : NancyModule
    {
        private static Dictionary<string, IEnumerable<string>> _queryEndpoints;

        public ConfigurationModule()
        {
            JsonSettings.MaxJsonLength = 5000000;
            JsonSettings.RetainCasing = true;

            var routes = GetConfigurationRoutes();

            foreach (var route in routes)
            {
                Get[route.Key] = _ => Response.AsJson((ExpandoObject)route.Value);
            }

            Get["{parent}/{child}"] = _ =>
            {
                var parent = routes.FirstOrDefault(pair => pair.Key.EndsWith(_["parent"]));
                var child = routes.FirstOrDefault(pair => pair.Key.EndsWith(_["child"]));

                return Response.AsJson((ExpandoObject)DynamicMerge.DoMerge(child.Value, parent.Value));
            };

            // Define query endpoints
            var entities = (new MappingManager()).Entities;
            foreach (var e in entities.Keys)
            {
                if (string.IsNullOrEmpty(entities[e]))
                {
                    Get[e] = _ =>
                    {
                        // retrieve the cached entity names associated with query
                        var queryEntities = _queryEndpoints[e];

                        // build up the set of query parameters (and their respective values)
                        var queryParameters = new Dictionary<string, string>();
                        foreach (var entityName in queryEntities)
                        {
                            queryParameters.Add(entityName, _[entityName]);
                        }

                        // Perform each query against the relevant entity from the InMemory cache
                        // and construct the response object
                        var response = new List<dynamic>();
                        foreach (var queryParameter in queryParameters.Keys)
                        {
                            var queryResults = new List<dynamic>();

                            var queryValues = queryParameters[queryParameter].Split(',');
                            foreach (var queryValue in queryValues)
                            {
                                var queryResult = InMemoryRepository.Get(queryParameter) as IEnumerable<dynamic>;
                                var filteredResult = queryResult.Where(i => i.name == queryValue);
                                queryResults.Add(filteredResult);
                            }

                            // nest the results under a key that relates to the entity being queried
                            var wrapper = new ExpandoObject();
                            Dynamitey.Dynamic.InvokeSet(wrapper, queryParameter, queryResults);
                            response.Add(wrapper);
                        }
                        return response;
                    };
                }
            }
        }

        public static Dictionary<string, dynamic> GetConfigurationRoutes()
        {
            var entities = (new MappingManager()).Entities;
            var hostUri = ConfigurationManager.AppSettings["hostUri"];

            _queryEndpoints = new Dictionary<string, IEnumerable<string>>();

            // Generate the root endpoint
            var resources = entities.Keys.Select(k => string.Format("{0}/{1}", hostUri, k)).ToArray();

            Dictionary<string, dynamic> routes = new Dictionary<string, dynamic>();

            routes["/"] = new {resources};

            // Generate an endpoint for each entity
            foreach (var e in entities.Keys)
            {
                // Only process simple mappings
                if (!string.IsNullOrEmpty(entities[e]))
                {
                    var url = string.Format("/{0}", e);
                    IDictionary<string, object> ret = new ExpandoObject();
                    var items = InMemoryRepository.Get(e) as IEnumerable<dynamic>;
                    ret.Add(e, items);

                    // Generate an endpoint for the entity collection
                	routes[url] = ret;

                    foreach (var item in items)
                    {
                        // Generate an endpoint for each entity within the collection
                        routes[item._route] = item;
                    }
                }
                else
                {
                    // Generate the query endpoints (these reference mapped entities)
                    
                    // extract the uri tokens from the query URI and translate them into entity names
                    var uriTokens = Regex.Matches(e, @"\{.*?\}");
                    var queryFields = new List<string>();
                    foreach (var s in uriTokens)
                    {
                        queryFields.Add(s.ToString().Replace("{","").Replace("}",""));
                    }
                    // cache the ordered entity names for the current query
                    _queryEndpoints.Add(e, queryFields);
                }
            }
            return routes;
        }
    }
}