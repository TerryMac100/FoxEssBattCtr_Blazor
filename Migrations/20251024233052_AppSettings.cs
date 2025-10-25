using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBattControl.Migrations
{
    /// <inheritdoc />
    public partial class AppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceSN",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FoxApiKey",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OffPeakFlagEntityID",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UseOffPeakFlag",
                table: "AppDbSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceSN",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "FoxApiKey",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "OffPeakFlagEntityID",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "UseOffPeakFlag",
                table: "AppDbSettings");
        }
    }
}
