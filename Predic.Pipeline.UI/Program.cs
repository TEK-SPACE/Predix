using System;
using System.Collections.Generic;
using Predic.Pipeline.Helper;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.UI
{
    static class Program
    {
        private static ILocation _locationService;

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
                DateTime startDate = new DateTime(2015, 10, 28, 18, 58, 57, DateTimeKind.Utc);
                DateTime endDate = new DateTime(2017, 10, 28, 19, 12, 17, DateTimeKind.Utc);
                var eventTypes = new[] {"PKIN", "PKOUT"};
                foreach (var eventType in eventTypes)
                {
                    Commentary.Print($"Location UID: {location.Uid}", true);
                    Commentary.Print($"Event Type : {eventType}", true);
                    Commentary.Print($"Start Date : {startDate:G}", true);
                    Commentary.Print($"End Date : {endDate:G}", true);

                    var parkingEvents = _locationService.Get(location.Uid, eventType, startDate, endDate);
                    parkingEventsForAllLocations.AddRange(parkingEvents);
                    Commentary.Print($"Total {eventType} events for Location UID: {location.Uid} are {parkingEvents.Count}");
                }
            }
            Commentary.Print($"Total Events for all locations: {parkingEventsForAllLocations.Count}");
            foreach (var parkingEvent in parkingEventsForAllLocations)
            {
                Commentary.Print($"Not Implemented: Pull Image Media for each parking event: {parkingEvent.AssetUid}");
            }
            Commentary.Print($"Completed. Please enter a key to exit");
        }

        static void Init()
        {
            Commentary.WriteToFile = true;
            _locationService = new LocationService();
        }
    }
}
