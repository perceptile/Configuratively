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

            // Define query endpoints
            var queries = (new MappingManager()).Queries;
            foreach (var q in queries)
            {
                if (!string.IsNullOrEmpty(q.UriTemplate))
                {
                    Get[q.UriTemplate] = _ =>
                    {
                        // retrieve the cached entity names associated with query
                        var queryEntities = _queryEndpoints[q.UriTemplate];

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
                        // return Response.AsJson((ExpandoObject)DynamicMerge.DoMerge(child.Value, parent.Value));
                    };
                }
            }
        }

        public static Dictionary<string, dynamic> GetConfigurationRoutes()
        {
            var mappingModel = new MappingManager();
            var hostUri = ConfigurationManager.AppSettings["hostUri"];

            _queryEndpoints = new Dictionary<string, IEnumerable<string>>();

            // Generate the root endpoint
            var resources = mappingModel.Entities.Select(e => string.Format("{0}/{1}", hostUri, e.Name)).ToArray();

            Dictionary<string, dynamic> routes = new Dictionary<string, dynamic>();

            routes["/"] = new {resources};

            // Generate an endpoint for each entity
            foreach (var e in mappingModel.Entities)
            {
                if (!string.IsNullOrEmpty(e.Regex))
                {
                    var url = string.Format("/{0}", e.Name);
                    IDictionary<string, object> ret = new ExpandoObject();
                    var items = InMemoryRepository.Get(e.Name) as IEnumerable<dynamic>;
                    ret.Add(e.Name, items);

                    // Generate an endpoint for the entity collection
                	routes[url] = ret;

                    foreach (var item in items)
                    {
                        // Generate an endpoint for each entity within the collection
                        routes[item._route] = item;
                    }
                }
            }

            // Process the queries
            foreach (var q in mappingModel.Queries)
            {
                if (!string.IsNullOrEmpty(q.UriTemplate))
                {
                    // Generate the query endpoints (these reference mapped entities)
                    
                    // extract the uri tokens from the query URI and translate them into entity names
                    var uriTokens = Regex.Matches(q.UriTemplate, @"\{.*?\}");
                    var queryFields = new List<string>();
                    foreach (var s in uriTokens)
                    {
                        queryFields.Add(s.ToString().Replace("{","").Replace("}",""));
                    }
                    // cache the ordered entity names for the current query
                    _queryEndpoints.Add(q.UriTemplate, queryFields);
                }
            }
            return routes;
        }
    }
}