using System;
using System.Data.Entity.Migrations;
using Predix.Domain.Model;
using Predix.Domain.Model.Enum;

namespace Predic.Pipeline.DataService
{
    public class PredixContextInitializer : DbMigrationsConfiguration<PredixContext>
    {
        public PredixContextInitializer()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(PredixContext context)
        {
            context.Activities.AddOrUpdate(x => x.Id, new Activity { Id = 1, ProcessDateTime = DateTime.Now, Error = "App Started"});
            context.NodeMasters.AddOrUpdate(x=>x.Id, new NodeMaster[]
            {
                new NodeMaster{ Id =1 , LocationUid = "LOCATION-225" },
                new NodeMaster{ Id =2 , LocationUid = "LOCATION-213" }
            } );
            context.NodeMasterRegulations.AddOrUpdate(x => x.Id, new NodeMasterRegulation []
            {
                new NodeMasterRegulation { Id = 1, NodeMasterId = 1, RegulationId = 1},
                new NodeMasterRegulation { Id = 2, NodeMasterId = 2, RegulationId = 2}
            });
           context.ParkingRegulations.AddOrUpdate(x=>x.RegualationId, new ParkingRegulation
            {
                RegualationId=1,
                Address = "310 Market street",
                Coodrinate1 = "32.713930891456904:-117.15828",
                Coodrinate2 = "32.713930891456904:-117.158251",
                Coodrinate3 = "32.713732:-117.158251",
                Coodrinate4 = "32.713731:-117.15828",
                DayOfWeek = "Mon|Tue|Wed|Thu|Fri|Sat|Sun",
                Description = "Fire Hydrant",
                ParkingAllowed = false,
                Duration = 0,
                Metered = false,
                HourlyRate = 0,
                EndTime = new TimeSpan(23, 59, 59),
                StartTime = new TimeSpan(00, 00, 00),
                IsActive = true,
                ViolationType = ViolationType.NoParking
            });
            context.ParkingRegulations.AddOrUpdate(x => x.RegualationId, new ParkingRegulation
            {
                RegualationId = 2,
                Address = "310 Market street",
                Coodrinate1 = "32.713930891456904:-117.15828",
                Coodrinate2 = "32.713930891456904:-117.158251",
                Coodrinate3 = "32.713732:-117.158251",
                Coodrinate4 = "32.713731:-117.15828",
                DayOfWeek = "Mon|Tue|Wed|Thu|Fri",
                Description = "Time Limit",
                ParkingAllowed = true,
                Duration = 2,
                Metered = true,
                HourlyRate = 1,
                EndTime = new TimeSpan(21, 00, 00),
                StartTime = new TimeSpan(09, 00, 00),
                IsActive = true,
                ViolationType = ViolationType.ExceedParkingLimit
            });
            context.ParkingRegulations.AddOrUpdate(x => x.RegualationId, new ParkingRegulation
            {
                RegualationId = 3,
                Address = "310 Market street",
                Coodrinate1 = "32.713930891456904:-117.15828",
                Coodrinate2 = "32.713930891456904:-117.158251",
                Coodrinate3 = "32.713732:-117.158251",
                Coodrinate4 = "32.713731:-117.15828",
                DayOfWeek = "Mon",
                Description = "Street Sweeping",
                ParkingAllowed = false,
                Duration = 0,
                Metered = false,
                HourlyRate = 0,
                EndTime = new TimeSpan(08, 00, 00),
                StartTime = new TimeSpan(07, 00, 00),
                IsActive = true,
                ViolationType = ViolationType.StreetSweeping
            });
            base.Seed(context);
        }
    }
}
