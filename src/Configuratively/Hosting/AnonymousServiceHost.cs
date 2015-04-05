using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Web;
using Nancy.Hosting.Wcf;

namespace Configuratively.Hosting
{
    static internal class AnonymousServiceHost
    {
        public static void Create()
        {
            var anonHost = new WebServiceHost(new NancyWcfGenericService(new AnonymousBootstrapper()), new Uri(ConfigurationManager.AppSettings["hostUri"]));
            anonHost.AddServiceEndpoint(typeof (NancyWcfGenericService), new WebHttpBinding(), "");
            anonHost.Open();
        }
    }


}