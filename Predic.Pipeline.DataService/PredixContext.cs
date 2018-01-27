using System.Data.Entity;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.DataService
{
    public class PredixContext : DbContext
    {
        public PredixContext() : base("DefaultConnection")
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = false;
        }
        public DbSet<Boundary> Boundaries { get; set; }
        public DbSet<GeViolation> GeViolations { get; set; }
        public DbSet<NodeMaster> NodeMasters { get; set; }
        public DbSet<NodeMasterRegulation> NodeMasterRegulations { get; set; }
        public DbSet<ParkingRegulation> ParkingRegulations { get; set; }


        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationDetails> LocationDetails { get; set; }

        public DbSet<ParkingEvent> ParkingEvents { get; set; }
        public DbSet<ParkingEventProperties> ParkingEventProperties { get; set; }

        public DbSet<Media> Medias { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Content> ImageContents { get; set; }
        public DbSet<Measures> Measures { get; set; }
        public DbSet<Activity> Activities { get; set; }
     
    }
}
