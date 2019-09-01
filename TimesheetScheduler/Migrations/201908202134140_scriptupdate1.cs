namespace TimesheetScheduler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class scriptupdate1 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.TimesheetWorkItem", "TimesheetDate", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.TimesheetWorkItem", "TimesheetDate", c => c.DateTime());
        }
    }
}
