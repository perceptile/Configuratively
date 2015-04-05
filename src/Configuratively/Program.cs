using Topshelf;

using Configuratively.Hosting;

namespace Configuratively
{
    class Program
    {
        static void Main(string[] args)
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
    }
}
