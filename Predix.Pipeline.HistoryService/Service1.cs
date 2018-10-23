using Predix.Domain.Model;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.ServiceProcess;
using System.Threading;
using Predix.Pipeline.DataService;

namespace Predix.Pipeline.HistoryService
{
    public partial class PredixHistoryService : ServiceBase
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        private Timer _schedular;
        public PredixHistoryService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Commentary.WriteToFile = true;
            Commentary.Print("Hitory Events Service is Started");
            _locationService = new LocationService(GlobalVariables);
            _eventService = new EventService(GlobalVariables);
            _imageService = new ImageService(GlobalVariables);
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["SeedData"]))
            {
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());
            }
            _schedular = new Timer(SchedularCallback);
            //Get the Interval in Minutes from AppSettings.
            int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);
            var scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
            if (DateTime.Now > scheduledTime)
            {
                //If Scheduled Time is passed set Schedule for the next Interval.
                scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
            }
            TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
            string schedule =
                $"{timeSpan.Days} day(s) {timeSpan.Hours} hour(s) {timeSpan.Minutes} minute(s) {timeSpan.Seconds} seconds(s)";

            Commentary.Print("Simple Service scheduled to run after: " + schedule + " {0}");
            //Get the difference in Minutes between the Scheduled and Current Time.
            int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

            //Change the Timer's Due Time.
            _schedular.Change(dueTime, Timeout.Infinite);
            //Task.Run(() => GetHistory());
        }
        private void SchedularCallback(object e)
        {
            Commentary.Print("Simple Service Log: {0}");
            this.GetHistory();
        }
        protected override void OnStop()
        {
            Commentary.Print("RTA Service is Stoped");
        }
        private void GetHistory()
        {
            try
            {
                IPredixWebSocketClient predixWebSocketClient = new PredixWebSocketClient();
                var locations = _locationService.GetLocationsUids();
                Commentary.Print($"Total Locations: {locations.Count}");
                Options options = new Options
                {
                    IgnoreRegulationCheck = Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreRegulationCheck"]),
                    MarkAllAsViolations = Convert.ToBoolean(ConfigurationManager.AppSettings["MarkAllAsViolations"]),
                    RefreshLocations = Convert.ToBoolean(ConfigurationManager.AppSettings["RefreshLocations"]),
                    SaveEvents = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveEvents"]),
                    SaveImages = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveImages"]),
                    Debug = Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"])
                };

                foreach (var location in locations)
                {
                    var inEvents = _eventService.Get(location, "PKIN", DateTime.UtcNow.AddMinutes(-2).ToEpoch().ToString(), DateTime.UtcNow.AddSeconds(30).ToEpoch().ToString());
                    var outEvents = _eventService.Get(location, "PKOUT", DateTime.UtcNow.AddMinutes(-2).ToEpoch().ToString(), DateTime.UtcNow.AddSeconds(30).ToEpoch().ToString());
                    Commentary.Print($"location: {location}, In Events: {inEvents.Count}, Out Events: {outEvents.Count}");
                    inEvents.AddRange(outEvents);
                    foreach (var evnt in inEvents)
                    {
                        predixWebSocketClient.ProcessEvent(_imageService, options,
                            new Customer() { Id = 4120, TimezoneId = "Eastern Standard Time" },
                            evnt);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Commentary.Print(e.ToString());
            }
        }
    }
}
