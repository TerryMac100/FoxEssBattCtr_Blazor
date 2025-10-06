using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBattControl.Migrations
{
    /// <inheritdoc />
    public partial class IntialDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SchedualId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeSlot = table.Column<int>(type: "INTEGER", nullable: false),
                    BattMode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MinSoc = table.Column<int>(type: "INTEGER", nullable: false),
                    MinDischargeSoc = table.Column<int>(type: "INTEGER", nullable: false),
                    DischargePower = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mode");

            migrationBuilder.DropTable(
                name: "Schedule");
        }
    }
}
