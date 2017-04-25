using System.Data.Entity;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.DataService
{
    public class PredixContext : DbContext
    {
        public DbSet<Location> Identifiers { get; set; }
        public DbSet<ParkingEvent> Details { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Measures> Measures { get; set; }
        public DbSet<Properties> Properties { get; set; }
        public DbSet<Activity> Activities { get; set; }
    }
}
