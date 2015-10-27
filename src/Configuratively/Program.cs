using System;
using System.Dynamic;
using System.IO;
using Configuratively.Hosting;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
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
            [ArgDescription("The route to generate to the specified file in the form route=outputpath.")]string[] routes)
        {
            var settings = new ConfigSettings(repositoryPath);

            var browser = new Browser(new AnonymousBootstrapper(true, settings));

            foreach (var routeFilePair in routes)
            {
                var split = routeFilePair.Split('=');

                if (split.Length != 2)
                {
                    Console.WriteLine("Routes parameter is not correct. Please use the format route1=outputPath1,route2=outputPath2.");
                    Environment.Exit(2);
                }

                var route = split[0];
                var path = split[1];

                var result = browser.Get(route);

                if (result.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The route: {0} does not exist.{1}", route, Environment.NewLine);
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(result.Body.AsString());

                if (Helpers.HasProperty(jsonObject, "_errors"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The configuration repository has errors for this route." + Environment.NewLine);
                    Console.ResetColor();
                    Console.WriteLine(string.Join(Environment.NewLine, jsonObject._errors) + Environment.NewLine);
                    Environment.Exit(1);
                }

                using (var stream = new StreamWriter(path))
                {
                    var writer = JsonConvert.SerializeObject(jsonObject);
                    writer.Write(jsonObject, stream);
                }
            }
        }
    }
}