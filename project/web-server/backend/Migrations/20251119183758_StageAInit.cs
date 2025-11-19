using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebServer.Migrations
{
    /// <inheritdoc />
    public partial class StageAInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Changes = table.Column<string>(type: "text", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FldId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StreetName = table.Column<string>(type: "text", nullable: false),
                    HouseNumber = table.Column<string>(type: "text", nullable: false),
                    BuildingName = table.Column<string>(type: "text", nullable: false),
                    Neighborhood = table.Column<string>(type: "text", nullable: false),
                    BldSivug = table.Column<string>(type: "text", nullable: false),
                    ShikumStatus = table.Column<string>(type: "text", nullable: false),
                    StatusSummary = table.Column<string>(type: "text", nullable: false),
                    StatusSummaryUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Complaints = table.Column<string>(type: "text", nullable: false),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    GushM = table.Column<string>(type: "text", nullable: true),
                    ParcelM = table.Column<string>(type: "text", nullable: true),
                    GushS = table.Column<string>(type: "text", nullable: true),
                    ParcelS = table.Column<string>(type: "text", nullable: true),
                    ParcelTat = table.Column<string>(type: "text", nullable: true),
                    StreetCode = table.Column<string>(type: "text", nullable: true),
                    TkBinyanNum = table.Column<string>(type: "text", nullable: true),
                    MivneNum = table.Column<string>(type: "text", nullable: true),
                    FiziNum = table.Column<string>(type: "text", nullable: true),
                    IsUnitInEmptyBuilding = table.Column<bool>(type: "boolean", nullable: true),
                    DamagePercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    FloorNum = table.Column<int>(type: "integer", nullable: true),
                    OwnerDetails = table.Column<string>(type: "text", nullable: true),
                    HolderDetails = table.Column<string>(type: "text", nullable: true),
                    PropertyNumber = table.Column<string>(type: "text", nullable: true),
                    WaterConsumption = table.Column<decimal>(type: "numeric", nullable: true),
                    DaysSinceWaterConsumption = table.Column<int>(type: "integer", nullable: true),
                    ElectricityConsumption = table.Column<decimal>(type: "numeric", nullable: true),
                    DaysSinceElectricityConsumption = table.Column<int>(type: "integer", nullable: true),
                    IntendedUse = table.Column<string>(type: "text", nullable: true),
                    ActualUse = table.Column<string>(type: "text", nullable: true),
                    ArnonaCodeShimush = table.Column<string>(type: "text", nullable: true),
                    HeterBniya = table.Column<string>(type: "text", nullable: true),
                    Tofes4 = table.Column<string>(type: "text", nullable: true),
                    HarigatBniya = table.Column<string>(type: "text", nullable: true),
                    IsurShimushHoreg = table.Column<string>(type: "text", nullable: true),
                    ParcelSize = table.Column<double>(type: "double precision", nullable: true),
                    BuildRights = table.Column<double>(type: "double precision", nullable: true),
                    ShtachBantuySum = table.Column<double>(type: "double precision", nullable: true),
                    FloorSum = table.Column<double>(type: "double precision", nullable: true),
                    KidumTichnunStatus = table.Column<string>(type: "text", nullable: true),
                    OwnershipType = table.Column<string>(type: "text", nullable: true),
                    WantToSell = table.Column<bool>(type: "boolean", nullable: true),
                    WantToRent = table.Column<bool>(type: "boolean", nullable: true),
                    HezkaMove = table.Column<string>(type: "text", nullable: true),
                    WhatsInside = table.Column<string>(type: "text", nullable: true),
                    CanMunicipalityFix = table.Column<bool>(type: "boolean", nullable: true),
                    OwnerPosition = table.Column<string>(type: "text", nullable: true),
                    MiuuniPosition = table.Column<string>(type: "text", nullable: true),
                    StandardMark = table.Column<string>(type: "text", nullable: true),
                    InPilot = table.Column<bool>(type: "boolean", nullable: true),
                    Shiabud = table.Column<bool>(type: "boolean", nullable: true),
                    OwnerUnderExecution = table.Column<bool>(type: "boolean", nullable: true),
                    LegalDispute = table.Column<bool>(type: "boolean", nullable: true),
                    ArnonaDebt = table.Column<bool>(type: "boolean", nullable: true),
                    MaintenanceStatus = table.Column<string>(type: "text", nullable: true),
                    DangerousBuilding = table.Column<bool>(type: "boolean", nullable: true),
                    LandQuality = table.Column<string>(type: "text", nullable: true),
                    BuildingRightsNotUsed = table.Column<bool>(type: "boolean", nullable: true),
                    ForPreservation = table.Column<bool>(type: "boolean", nullable: true),
                    FutureYeud = table.Column<string>(type: "text", nullable: true),
                    FutureUse = table.Column<string>(type: "text", nullable: true),
                    PikuachKlali = table.Column<string>(type: "text", nullable: true),
                    PikuachAlBniya = table.Column<string>(type: "text", nullable: true),
                    TzavDangerBuilding = table.Column<string>(type: "text", nullable: true),
                    TzavShiputzFronts = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "text", nullable: false),
                    PendingTwoFactorCode = table.Column<string>(type: "text", nullable: true),
                    PendingTwoFactorToken = table.Column<string>(type: "text", nullable: true),
                    PendingTwoFactorExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalSystemSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<int>(type: "integer", nullable: false),
                    SystemName = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSystemSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalSystemSnapshots_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingLogs_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuildingLogs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingLogs_BuildingId",
                table: "BuildingLogs",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingLogs_CreatedByUserId",
                table: "BuildingLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSystemSnapshots_BuildingId",
                table: "ExternalSystemSnapshots",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BuildingLogs");

            migrationBuilder.DropTable(
                name: "ExternalSystemSnapshots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
