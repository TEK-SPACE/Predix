using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using CommandLine;
using Predix.Domain.Model;
using Predix.Pipeline.DataService;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.UI
{
    static class Program
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        static void Main(string[] args)
        {
            Options options = new Options();
            var result = Parser.Default.ParseArguments<Options>(args);
            switch (result.Tag)
            {
                case ParserResultType.Parsed:
                    var parsed = (Parsed<Options>) result;
                    options = parsed.Value;
                    Commentary.Print(
                        $"\nRefresh Location = {options.RefreshLocations}\nIgnore Regulation Check = {options.IgnoreRegulationCheck}\nSave Events = {options.SaveEvents}\nSave Images= {options.SaveImages}\nMark All As Violations= {options.MarkAllAsViolations}");
                    break;
                case ParserResultType.NotParsed:
                    var notParsed = (NotParsed<Options>) result;
                    var errors = notParsed.Errors;
                    foreach (var error in errors)
                    {
                        Commentary.Print($"{error}");
                    }
                    return;
            }

            Init();
           // History();
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

                _eventService.GetByBoundaryAsync(boundary.Range, "PKIN", "PKOUT", _imageService, options,
                    new Customer(){Id = boundary.CustomerId, TimezoneId = "Eastern Standard Time" });
            }

            Commentary.Print($"Completed. Please enter a key to exit");
        }

        static void History()
        {
            IPredixWebSocketClient _predixWebSocketClient = new PredixWebSocketClient();
            Options options = new Options
            {
                IgnoreRegulationCheck = Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreRegulationCheck"]),
                MarkAllAsViolations = Convert.ToBoolean(ConfigurationManager.AppSettings["MarkAllAsViolations"]),
                RefreshLocations = Convert.ToBoolean(ConfigurationManager.AppSettings["RefreshLocations"]),
                SaveEvents = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveEvents"]),
                SaveImages = Convert.ToBoolean(ConfigurationManager.AppSettings["SaveImages"]),
                Debug = Convert.ToBoolean(ConfigurationManager.AppSettings["Debug"])
            };
            foreach (var location in _locationService.GetLocationsUids())
            {
                var inEvents = _eventService.Get(location, "PKIN", DateTime.UtcNow.AddHours(-1).ToEpoch().ToString(), DateTime.UtcNow.ToEpoch().ToString());
                var outEvents = _eventService.Get(location, "PKOUT", DateTime.UtcNow.AddHours(-1).ToEpoch().ToString(), DateTime.UtcNow.ToEpoch().ToString());
                inEvents.AddRange(outEvents);
                foreach(var evnt in inEvents)
                {
                    _predixWebSocketClient.ProcessEvent(_imageService, options, 
                        new Customer() { Id = 4120, TimezoneId = "Eastern Standard Time" },
                        evnt);
                }
            }
        }
        static void Init()
        {
            Commentary.WriteToFile = true;
            _locationService = new LocationService(GlobalVariables);
            _eventService = new EventService(GlobalVariables);
            _imageService = new ImageService(GlobalVariables);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());
        }
    }
}