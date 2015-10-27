using log4net;

namespace Configuratively.Infrastructure
{
    public static class LogManager
    {
        public static ILog Logger
        {
            get { return log4net.LogManager.GetLogger("default"); }
        }
    }
}
