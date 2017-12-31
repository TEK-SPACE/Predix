﻿using System;
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
        private static readonly Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();

        static void Main(string[] args)
        {
            Init();
            var bbox = "33.974968:-84.736467,33.492143:-84.035631"; // "32.715675:-117.161230,32.708498:-117.151681";
            var locationType = "PARKING_ZONE";
            int pagesize = 50;
            Commentary.Print($"BBOX: {bbox}");
            Commentary.Print($"Location Type: {locationType}");
            var refreshNodeMaster = Convert.ToBoolean(args[0]);
            var ignoreRegulationCheck = Convert.ToBoolean(args[1]);
            if (refreshNodeMaster)
            {
                Commentary.Print($"Calling Get All Locations by BBOX & Location Type");
                //Commentary.Print($"Refreshing Node(Local) Data", true);
                var locations = _locationService.All(bbox, locationType, pagesize);
                Commentary.Print($"Total Locations: {locations.Count}");
                _locationService.Details(locations.Select(x => x.LocationUid).Distinct().ToList());
            }

            _eventService.GetByBoundary(bbox, "PKIN", "PKOUT", _imageService, ignoreRegulationCheck);
            //var parkingEvent = _eventService.GetByBoundary(bbox, "PKIN", "PKOUT");
            //_imageService.MediaOnDemand(parkingEvent.Result.Properties.ImageAssetUid, parkingEvent.Result.Timestamp);
            //var parkingEventsForAllLocations = new List<ParkingEvent>();

            //foreach (var location in locations)
            //{
            //    Commentary.Print($"Get Parking Events for");
            //    //DateTime startDate = new DateTime(2017, 11, 28, 18, 58, 57, DateTimeKind.Utc);
            //    //DateTime endDate = new DateTime(2017, 11, 29, 19, 12, 17, DateTimeKind.Utc);
            //    var eventTypes = new[] {"PKIN", "PKOUT"};
            //    //foreach (var eventType in eventTypes)
            //    //{
            //    Commentary.Print($"Location UID: {location.LocationUid}", true);
            //    Commentary.Print($"bbox : {bbox}", true);
            //    // Commentary.Print($"Event Type : {eventType}", true);
            //    //Commentary.Print($"Start Date : {startDate:G}", true);
            //    //Commentary.Print($"End Date : {endDate:G}", true);

            //    var parkingEvent = _eventService.GetByBoundary(bbox, "PKIN", "PKOUT");
            //    //parkingEventsForAllLocations.Add(parkingEvent);
            //    //Commentary.Print($"Event Type: {eventType}, Location UID: {location.LocationUid} Asset UID:{parkingEvent.AssetUid}");
            //    _imageService.MediaOnDemand(parkingEvent.Properties.ImageAssetUid, parkingEvent.Timestamp);
            //    //}
            //}
            //Commentary.Print($"Total Events for all locations: {parkingEventsForAllLocations.Count}");
            //foreach (var parkingEvent in parkingEventsForAllLocations)
            //{
            //    var imageAsset = _imageService.MediaOnDemand(parkingEvent.AssetUid, parkingEvent.Timestamp);
            //}
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