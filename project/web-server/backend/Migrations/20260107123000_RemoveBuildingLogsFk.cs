using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebServer.Data;

#nullable disable

namespace WebServer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260107123000_RemoveBuildingLogsFk")]
public partial class RemoveBuildingLogsFk : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BuildingLogs_Buildings_BuildingId",
            table: "BuildingLogs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_BuildingLogs_Buildings_BuildingId",
            table: "BuildingLogs",
            column: "BuildingId",
            principalTable: "Buildings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
