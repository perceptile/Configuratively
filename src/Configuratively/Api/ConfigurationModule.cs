using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Dynamic;

using Nancy;
using Nancy.Json;

using Configuratively.Repositories;

namespace Configuratively.Api
{
    public class ConfigurationModule : NancyModule
    {
        public ConfigurationModule()
        {
            JsonSettings.MaxJsonLength = 5000000;
            JsonSettings.RetainCasing = true;

            var entities = (new Domain.MappingManager()).Entities;
            var hostUri = ConfigurationManager.AppSettings["hostUri"];

            // Generate the root endpoint
            var resources = entities.Keys.Select(k => string.Format("{0}/{1}", hostUri, k)).ToArray();
            Get["/"] = _ =>
                Response.AsJson(new { resources = resources });

            // Generate an endpoint for each entity
            foreach (var e in entities.Keys)
            {
                var url = string.Format("/{0}", e);
                IDictionary<string, object> ret = new ExpandoObject();
                var items = InMemoryRepository.Get(e) as IEnumerable<dynamic>;
                ret.Add(e, items);

                Get[url] = _ =>
                    Response.AsJson(ret);
            }
        }
    }
}