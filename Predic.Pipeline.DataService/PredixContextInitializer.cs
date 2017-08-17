using System;
using System.Data.Entity.Migrations;
using Predix.Domain.Model;

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
            context.Activities.Add(new Activity {ProcessDateTime = DateTime.Now, Error = "App Started"});
            base.Seed(context);
        }
    }
}
