namespace Predix.Pipeline.DataService
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RunSqlScript : DbMigration
    {
        public override void Up()
        {
            Sql("ALTER TABLE dbo.GeViolations ADD ViolationTimeElapsed AS dbo.ViolationTimeElapsed(RegulationId, EventInDateTime)");
        }
        
        public override void Down()
        {
            Sql("ALTER TABLE dbo.GeViolations DROP COLUMN ViolationTimeElapsed");
        }
    }
}
