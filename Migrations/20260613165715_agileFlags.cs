using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBattControl.Migrations
{
    /// <inheritdoc />
    public partial class agileFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgileChargeFlagEntityID",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AgileChargeThreshold",
                table: "AppDbSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "AgileDischargeFlagEntityID",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AgileDischargeThreshold",
                table: "AppDbSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgileChargeFlagEntityID",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "AgileChargeThreshold",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "AgileDischargeFlagEntityID",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "AgileDischargeThreshold",
                table: "AppDbSettings");
        }
    }
}
