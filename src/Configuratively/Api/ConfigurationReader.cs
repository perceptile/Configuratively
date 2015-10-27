using System;
using System.Collections.Generic;
using System.Dynamic;
using Configuratively.Hosting;
using Nancy.Testing;
using Newtonsoft.Json;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace Configuratively.Api
{
    public class ConfigurationReader
    {
        private readonly Browser _browser;

        public ConfigurationReader(ConfigSettings configSettings)
        {
            _browser = new Browser(new AnonymousBootstrapper(true, configSettings));
        }

        public String Get(String route)
        {
            var result = _browser.Get(route);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException(string.Format("Unable to find configuration for the route: {0}", route));
            }

            var response = result.Body.AsString();

            dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(response);

            if (Helpers.HasProperty(jsonObject, "_errors"))
            {
                throw new JsonException(string.Format("Errors found in configuration repository.{0}{1}", 
                    Environment.NewLine, 
                    string.Join(Environment.NewLine, jsonObject._errors)));
            }

            return response;
        }
    }
}