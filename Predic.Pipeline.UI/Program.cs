using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predic.Pipeline.DataService;
using Predix.Domain.Model;

namespace Predic.Pipeline.UI
{
    static class Program
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();

        static void Main(string[] args)
        {
            Init();
            var locationType = "PARKING_ZONE";
            int pagesize = 50;
            var refreshNodeMaster = Convert.ToBoolean(args[0]);
            var ignoreRegulationCheck = Convert.ToBoolean(args[1]);
            List<Boundary> boundaries = _locationService.GetBoundaries();
            foreach (var boundary in boundaries)
            {
                Commentary.Print($"BBOX: {boundary.Range}");
                Commentary.Print($"Location Type: {locationType}");
                if (refreshNodeMaster)
                {
                    Commentary.Print($"Calling Get All Locations by BBOX & Location Type");
                    var locations = _locationService.All(boundary.Range, locationType, pagesize);
                    Commentary.Print($"Total Locations: {locations.Count}");
                    _locationService.Details(locations.Select(x => x.LocationUid).Distinct().ToList());
                }

                _eventService.GetByBoundary(boundary.Range, "PKIN", "PKOUT", _imageService, ignoreRegulationCheck);
            }

            Commentary.Print($"Completed. Please enter a key to exit");
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