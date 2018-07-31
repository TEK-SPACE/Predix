using System.ServiceProcess;

namespace Predix.Pipeline.WinService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new PredixRealTimeService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
