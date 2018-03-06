using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.ServiceProcess;
using Predix.Domain.Model;
using Predix.Pipeline.DataService;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.WinService
{
    public partial class PredixPipelineService : ServiceBase
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        private readonly System.Timers.Timer _timeDelay;

        public PredixPipelineService()
        {
            InitializeComponent();
            _timeDelay = new System.Timers.Timer();
            _timeDelay.Elapsed += WorkProcess;

            Commentary.WriteToFile = true;
            _locationService = new LocationService(GlobalVariables);
            _eventService = new EventService(GlobalVariables);
            _imageService = new ImageService(GlobalVariables);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());
        }

        protected override void OnStart(string[] args)
        {
            Commentary.Print("Service is Started");
            _timeDelay.Enabled = true;
            OpenSocket();
        }

        protected override void OnStop()
        {
            Commentary.Print("Service is Stoped");
            _timeDelay.Enabled = false;
        }

        public void WorkProcess(object sender, System.Timers.ElapsedEventArgs e)
        {
            //OpenSocket();
        }

        private void OpenSocket()
        {
            Options options = new Options
            {
                IgnoreRegulationCheck = Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreRegulationCheck"]),
                MarkAllAsViolations = Convert.ToBoolean(ConfigurationManager.AppSettings["MarkAllAsViolations"]),
                RefreshLocations = Convert.ToBoolean(ConfigurationManager.AppSettings["RefreshLocations"]),
                SaveEvents = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveEvents"]),
                SaveImages = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveImages"])
            };
            var locationType = "PARKING_ZONE";
            int pagesize = 50;
            List<Boundary> boundaries = _locationService.GetBoundaries();
            foreach (var boundary in boundaries)
            {
                Commentary.Print($"BBOX: {boundary.Range}");
                Commentary.Print($"Location Type: {locationType}");
                if (options.RefreshLocations)
                {
                    Commentary.Print($"Calling Get All Locations by BBOX & Location Type");
                    var locations = _locationService.All(boundary.Range, locationType, pagesize);
                    Commentary.Print($"Total Locations: {locations.Count}");

                    _locationService.Details(locations.Select(x => x.LocationUid).Distinct().ToList());
                }

                _eventService.GetByBoundary(boundary.Range, "PKIN", "PKOUT", _imageService, options);
            }

            Commentary.Print($"Completed. Please enter a key to exit");
        }
    }
}