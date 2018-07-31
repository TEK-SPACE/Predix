using Predix.Domain.Model;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;
using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Predix.Pipeline.HistoryService
{
    public partial class PredixHistoryService : ServiceBase
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        public PredixHistoryService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Commentary.Print("Hitory Events Service is Started");

            Commentary.WriteToFile = true;
            _locationService = new LocationService(GlobalVariables);
            _eventService = new EventService(GlobalVariables);
            _imageService = new ImageService(GlobalVariables);
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());

            Task.Run(() => GetHistory());
        }

        protected override void OnStop()
        {
            Commentary.Print("RTA Service is Stoped");
        }
        private void GetHistory()
        {
            IPredixWebSocketClient _predixWebSocketClient = new PredixWebSocketClient();
            foreach (var location in _locationService.GetLocationsUids())
            {
                var inEvents = _eventService.Get(location, "PKIN", DateTime.UtcNow.AddHours(-1).ToEpoch().ToString(), DateTime.UtcNow.ToEpoch().ToString());
                var outEvents = _eventService.Get(location, "PKOUT", DateTime.UtcNow.AddHours(-1).ToEpoch().ToString(), DateTime.UtcNow.ToEpoch().ToString());
                inEvents.AddRange(outEvents);
                foreach (var evnt in inEvents)
                {
                    _predixWebSocketClient.ProcessEvent(_imageService,
                        new Customer() { Id = 4120, TimezoneId = "Eastern Standard Time" },
                        evnt);
                }
            }

            Commentary.Print($"Completed. Please enter a key to exit");
        }
    }
}
