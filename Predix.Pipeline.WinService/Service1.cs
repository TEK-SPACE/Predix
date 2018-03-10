﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
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

        public PredixPipelineService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Commentary.Print("Service is Started");

            Commentary.WriteToFile = true;
            _locationService = new LocationService(GlobalVariables);
            _eventService = new EventService(GlobalVariables);
            _imageService = new ImageService(GlobalVariables);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<PredixContext, PredixContextInitializer>());

            Task.Run(() => OpenSocket());
        }

        protected override void OnStop()
        {
            Commentary.Print("Service is Stoped");
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
            Commentary.Print(
                $"\nRefresh Location = {options.RefreshLocations}\nIgnore Regulation Check = {options.IgnoreRegulationCheck}\nSave Events = {options.SaveEvents}\nSave Images= {options.SaveImages}\nMark All As Violations= {options.MarkAllAsViolations}");
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

                _eventService.GetByBoundaryAsync(boundary.Range, "PKIN", "PKOUT", _imageService, options);
            }

            Commentary.Print($"Completed. Please enter a key to exit");
        }
    }
}