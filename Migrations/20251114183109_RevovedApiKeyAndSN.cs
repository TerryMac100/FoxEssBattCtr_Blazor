using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBattControl.Migrations
{
    /// <inheritdoc />
    public partial class RevovedApiKeyAndSN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceSN",
                table: "AppDbSettings");

            migrationBuilder.DropColumn(
                name: "FoxApiKey",
                table: "AppDbSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
