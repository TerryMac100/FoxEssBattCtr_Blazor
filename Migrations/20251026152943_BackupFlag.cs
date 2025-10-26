using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorBattControl.Migrations
{
    /// <inheritdoc />
    public partial class BackupFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupFlagEntityID",
                table: "AppDbSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupFlagEntityID",
                table: "AppDbSettings");
        }
    }
}
