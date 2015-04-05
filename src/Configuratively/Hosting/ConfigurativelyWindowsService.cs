namespace Configuratively.Hosting
{
    internal class ConfigurativelyWindowsService
    {
        public void Start()
        {
            AnonymousServiceHost.Create();
        }

        public void Stop()
        {
        }
    }
}
