namespace TimesheetScheduler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class scriptupdate : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.TimesheetWorkItem", "TimesheetDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.TimesheetWorkItem", "TimesheetDate", c => c.DateTime(nullable: false));
        }
    }
}
