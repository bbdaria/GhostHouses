using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebServer.Data;

#nullable disable

namespace WebServer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20251214120000_AddExcelColumns")]
public partial class AddExcelColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RegistryEntryDate",
            table: "Buildings",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InfoSource",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "InTreatment",
            table: "Buildings",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Revach",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StreetHouseCombined",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Quarter",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SubQuarter",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StatisticalArea",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MunicipalUseType",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DamagedAreaAtAddress",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ArnonaExemption",
            table: "Buildings",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DangerousBldgOrderIssuedAt",
            table: "Buildings",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AreaMaintenanceLevel",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CommercialActivityLevel",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SafetyFeeling",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PedestrianTrafficInArea",
            table: "Buildings",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RealEstatePricesInArea",
            table: "Buildings",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RegistryEntryDate", table: "Buildings");
        migrationBuilder.DropColumn(name: "InfoSource", table: "Buildings");
        migrationBuilder.DropColumn(name: "InTreatment", table: "Buildings");
        migrationBuilder.DropColumn(name: "Revach", table: "Buildings");
        migrationBuilder.DropColumn(name: "StreetHouseCombined", table: "Buildings");
        migrationBuilder.DropColumn(name: "Quarter", table: "Buildings");
        migrationBuilder.DropColumn(name: "SubQuarter", table: "Buildings");
        migrationBuilder.DropColumn(name: "StatisticalArea", table: "Buildings");
        migrationBuilder.DropColumn(name: "MunicipalUseType", table: "Buildings");
        migrationBuilder.DropColumn(name: "DamagedAreaAtAddress", table: "Buildings");
        migrationBuilder.DropColumn(name: "ArnonaExemption", table: "Buildings");
        migrationBuilder.DropColumn(name: "DangerousBldgOrderIssuedAt", table: "Buildings");
        migrationBuilder.DropColumn(name: "AreaMaintenanceLevel", table: "Buildings");
        migrationBuilder.DropColumn(name: "CommercialActivityLevel", table: "Buildings");
        migrationBuilder.DropColumn(name: "SafetyFeeling", table: "Buildings");
        migrationBuilder.DropColumn(name: "PedestrianTrafficInArea", table: "Buildings");
        migrationBuilder.DropColumn(name: "RealEstatePricesInArea", table: "Buildings");
    }
}

