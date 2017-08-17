using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predix.Domain.Model.Location;
using Predic.Pipeline.DataService;

namespace Predic.Pipeline.UI
{
    static class Program
    {
        private static ILocation _locationService;
        private static IEvent _eventService;
        private static IImage _imageService;
        internal static Dictionary<string, object> _globalVariables = new Dictionary<string, object>();
        static void Main(string[] args)
        {
            Init();
            var bbox = "32.715675:-117.161230,32.708498:-117.151681";
            var locationType = "PARKING_ZONE";
            int pagesize = 5;
            Commentary.Print($"Calling Get All Locations by BBOX & Location Type");
            Commentary.Print($"BBOX: {bbox}", true);
            Commentary.Print($"Location Type: {locationType}", true);
            var locations = _locationService.All(bbox, locationType, pagesize);
            Commentary.Print($"Total Locations: {locations.Count}");

            var parkingEventsForAllLocations = new List<ParkingEvent>();
           
            foreach (var location in locations)
            {
                Commentary.Print($"Get Parking Events for");
                DateTime startDate = new DateTime(2017, 04, 28, 18, 58, 57, DateTimeKind.Utc);
                DateTime endDate = new DateTime(2017, 04, 28, 19, 12, 17, DateTimeKind.Utc);
                var eventTypes = new[] {"PKIN", "PKOUT"};
                foreach (var eventType in eventTypes)
                {
                    Commentary.Print($"Location UID: {location.Uid}", true);
                    Commentary.Print($"Event Type : {eventType}", true);
                    Commentary.Print($"Start Date : {startDate:G}", true);
                    Commentary.Print($"End Date : {endDate:G}", true);

                    var parkingEvent = _eventService.Get(location.Uid, eventType);
                    parkingEventsForAllLocations.Add(parkingEvent);
                    Commentary.Print($"Event Type: {eventType}, Location UID: {location.Uid} Asset UID:{parkingEvent.AssetUid}");
                    var imageBase64 = _imageService.MediaOnDemand(parkingEvent.AssetUid, parkingEvent.Timestamp);
                }
            }
            Commentary.Print($"Total Events for all locations: {parkingEventsForAllLocations.Count}");
            //foreach (var parkingEvent in parkingEventsForAllLocations)
            //{
            //    var imageAsset = _imageService.MediaOnDemand(parkingEvent.AssetUid, parkingEvent.Timestamp);
            //}
            Commentary.Print($"Completed. Please enter a key to exit");
        }

        static void Init()
        {

            Commentary.WriteToFile = true;
            _locationService = new LocationService(_globalVariables);
            _eventService = new EventService(_globalVariables);
            _imageService = new ImageService(_globalVariables);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());
        }
    }
}
