using System.ServiceProcess;

namespace Predix.Pipeline.HistoryService
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
                new PredixHistoryService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
