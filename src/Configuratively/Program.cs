using System;
using System.IO;
using Configuratively.Hosting;
using Configuratively.Workers;
using JsonFx.Json;
using JsonFx.Serialization;
using Nancy.Testing;
using PowerArgs;
using Topshelf;

namespace Configuratively
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HostFactory.Run(x =>
                {
                    x.Service<ConfigurativelyWindowsService>(s =>
                    {
                        s.ConstructUsing(name => new ConfigurativelyWindowsService());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription("Exposes the configuration repository as a web service over the artefact repository");
                    x.SetDisplayName("Configuratively");
                    x.SetServiceName("Configuratively");
                });
            }
            else
            {
                Args.InvokeAction<FromCommandArgs>(args);
            }
        }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    internal class FromCommandArgs
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Exports the configuration resource to a specified file.")]
        public void Export(
            [ArgRequired]
            [ArgExistingDirectory]
            [ArgDescription("The path to the configuration repository.")]string repositoryPath, 
            [ArgRequired]
            [ArgDescription("The route to the configuration resource.")]string route, 
            [ArgRequired]
            [ArgDescription("The relative filepath to save the resource to.")]string path)
        {
            var settings = new ConfigSettings(repositoryPath);

            var browser = new Browser(new AnonymousBootstrapper(true, settings));
            var result = browser.Get(route);

            var jsonReader = new JsonReader();
            dynamic jsonObject = jsonReader.Read(result.Body.AsString());

            if (Helpers.HasProperty(jsonObject, "_errors"))
            {
                Console.WriteLine("The configuration repository has errors for this route." + Environment.NewLine);
                Console.WriteLine(string.Join(Environment.NewLine, jsonObject._errors) + Environment.NewLine);

                Environment.Exit(1);
            }

            using (var stream = new StreamWriter(path))
            {
                var writer = new JsonWriter(new DataWriterSettings{PrettyPrint = true});
                writer.Write(jsonObject, stream);
            }
        }
    }
}