//using Asos.Alm.ConfigurationRepository.Background;

using System.Configuration;
using System.IO;
using Configuratively.Workers;
using log4net;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using LogManager = Configuratively.Infrastructure.LogManager;

namespace Configuratively.Hosting
{
    public class AnonymousBootstrapper : DefaultNancyBootstrapper
    {
        private readonly bool _synchronise;
        private readonly ConfigSettings _settings;
        public AnonymousBootstrapper(bool synchronise, ConfigSettings settings)
        {
            _synchronise = synchronise;
            _settings = settings;
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            pipelines.OnError.AddItemToEndOfPipeline((nancyContext, exception) =>
            {
                container.Resolve<ILog>().Error("Unhandled and logged at the end of the pipeline", exception);
                return HttpStatusCode.InternalServerError;
            });
            base.RequestStartup(container, pipelines, context);
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            container.Register(_settings);
            container.Register(LogManager.Logger);
            container.Register<ConfigRepoSync>().AsSingleton();
            
            if (_synchronise)
            {
                container.Resolve<ConfigRepoSync>().Synchronise();
            }
            else
            {
                container.Resolve<ConfigRepoSync>().Start();
            }

            pipelines.OnError.AddItemToEndOfPipeline((nancyContext, exception) =>
            {              
                container.Resolve<ILog>().Error(string.Format("Unhandled and logged at the end of the pipeline"), exception);
                return HttpStatusCode.InternalServerError;
            });

            pipelines.AfterRequest += ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");

            base.ApplicationStartup(container, pipelines);
        }
    }

    public class ConfigSettings 
    {
        public ConfigSettings() : this(ConfigurationManager.AppSettings["repoPath"])
        {
        }

        public ConfigSettings(string repositoryPath)
        {
            RepositoryPath = Path.GetFullPath(repositoryPath);
            HostUri = ConfigurationManager.AppSettings["hostUri"];
            MappingFile = ConfigurationManager.AppSettings["mappingFile"];
            ServiceUri = ConfigurationManager.AppSettings["ClientSettingsProvider.ServiceUri"];
        }

        public string HostUri { get; set; }
        public string RepositoryPath { get; set; }
        public string MappingFile { get; set; }
        public string ServiceUri { get; set; }
    }

}