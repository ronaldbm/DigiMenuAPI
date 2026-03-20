using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace DigiMenuAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchLocationAndMapToggle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowMapInMenu",
                table: "CompanyThemes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Branches",
                type: "geography",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "Location",
                value: null);

            migrationBuilder.UpdateData(
                table: "CompanyThemes",
                keyColumn: "Id",
                keyValue: 1,
                column: "ShowMapInMenu",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowMapInMenu",
                table: "CompanyThemes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Branches");
        }
    }
}
