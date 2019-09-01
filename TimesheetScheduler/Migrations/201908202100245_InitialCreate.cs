namespace TimesheetScheduler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TimesheetWorkItem",
                c => new
                    {
                        TimesheetWorkItemId = c.Int(nullable: false, identity: true),
                        IsWeekend = c.Boolean(nullable: false),
                        TimesheetDate = c.DateTime(nullable: false),
                        WorkItemNumber = c.Int(nullable: false),
                        Description = c.String(),
                        ChargeableHours = c.Single(nullable: false),
                        NonChargeableHours = c.Single(nullable: false),
                        Comments = c.String(),
                    })
                .PrimaryKey(t => t.TimesheetWorkItemId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TimesheetWorkItem");
        }
    }
}
