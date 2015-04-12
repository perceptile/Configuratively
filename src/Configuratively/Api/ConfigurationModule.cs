using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using Configuratively.Domain;
using Configuratively.Hosting;
using Configuratively.Repositories;
using Nancy;
using Nancy.Json;

namespace Configuratively.Api
{
    public class ConfigurationModule : NancyModule
    {
        private readonly ConfigSettings _settings;
        private static Dictionary<string, IEnumerable<string>> _queryEndpoints;

        public ConfigurationModule(ConfigSettings settings)
        {
            _settings = settings;
            JsonSettings.MaxJsonLength = 5000000;
            JsonSettings.RetainCasing = true;

            var routes = GetConfigurationRoutes();

            foreach (var route in routes)
            {
                Get[route.Key] = _ => Response.AsJson((ExpandoObject)route.Value);
            }

            // Define query endpoints
            var queries = (new MappingManager(_settings)).Queries;
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
                        var responseObjects = new List<dynamic>();
                        foreach (var queryParameter in queryParameters.Keys)
                        {
                            var queryValue = queryParameters[queryParameter];
                            var queryResult = InMemoryRepository.Get(queryParameter) as IEnumerable<dynamic>;

                            var errors = queryResult.Where(o => Helpers.HasProperty(o, "_error")).Select(o => o._error).ToList();
                            if (errors.Any())
                                return Response.AsJson(new {_errors = errors});

                            var result = queryResult.FirstOrDefault(i => i.name == queryValue);

                            if (result == null)
                            {
                                return HttpStatusCode.NotFound;
                            }
                            responseObjects.Add(result);
                        }

                        // Merge the entities that have been queried into a single object.
                        // Child URI segments have greater merge precedence than their parent
                        if (responseObjects.Count >= 2)
                        {
                            // a query must have at least 2 referenced entities
                            dynamic response = DynamicMerge.DoMerge(responseObjects[1], responseObjects[0]);

                            // merge any remaining entity query results
                            for (int i=2; i < responseObjects.Count; i++)
                            {
                                response = DynamicMerge.DoMerge(responseObjects[i], response);
                            }

                            return Response.AsJson((ExpandoObject)response);
                        }

                        return HttpStatusCode.BadRequest;
                    };
                }
            }
        }

        public Dictionary<string, dynamic> GetConfigurationRoutes()
        {
            var mappingModel = new MappingManager(_settings);
            var hostUri = _settings.HostUri;

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