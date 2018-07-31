using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predix.Pipeline.DataService
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
        //public DbSet<NodeMaster> NodeMasters { get; set; }
        public DbSet<NodeMasterRegulation> NodeMasterRegulations { get; set; }
        public DbSet<ParkingRegulation> ParkingRegulations { get; set; }


        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationDetails> LocationDetails { get; set; }
        public DbSet<LocationDetailsExtended> LocationDetailsExtendeds { get; set; }


        public DbSet<ParkingEvent> ParkingEvents { get; set; }
        public DbSet<ParkingEventProperties> ParkingEventProperties { get; set; }

        public DbSet<Media> Medias { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Content> ImageContents { get; set; }
        public DbSet<Measures> Measures { get; set; }
        public DbSet<Activity> Activities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<GeViolation>()
            //    .Property(p => p.ViolationTimeElapsed)
            //    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
            //    .HasComputedColumnSql("[LastName] + ', ' + [FirstName]");
        }
        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException dbEntityValidationException)
            {
                Exception raise =
                    (from validationErrors in dbEntityValidationException.EntityValidationErrors
                        from validationError in validationErrors.ValidationErrors
                        select $"{validationErrors.Entry.Entity}:{validationError.ErrorMessage}")
                    .Aggregate<string, Exception>(dbEntityValidationException,
                        (current, message) => new InvalidOperationException(message, current));
                throw raise;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }
}
