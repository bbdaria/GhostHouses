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
                name: "Streets",
                columns: table => new
                {
                    StreetId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streets", x => x.StreetId);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FIdId = table.Column<int>(type: "integer", nullable: true),
                    StreetName = table.Column<string>(type: "text", nullable: false),
                    StreetId = table.Column<int>(type: "integer", nullable: true),
                    BldNum = table.Column<string>(type: "text", nullable: false),
                    BldName = table.Column<string>(type: "text", nullable: false),
                    Neighborhood = table.Column<string>(type: "text", nullable: false),
                    BldSivug = table.Column<int>(type: "integer", nullable: true),
                    ShikumStatus = table.Column<int>(type: "integer", nullable: false),
                    StatusSummary = table.Column<string>(type: "text", nullable: false),
                    StatusSummary_Update_Dt = table.Column<DateTime>(type: "date", nullable: true),
                    complaints = table.Column<string>(type: "text", nullable: false),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    GushM = table.Column<int>(type: "integer", nullable: true),
                    ParcelM = table.Column<int>(type: "integer", nullable: true),
                    GushS = table.Column<int>(type: "integer", nullable: true),
                    ParcelS = table.Column<int>(type: "integer", nullable: true),
                    ParcelTat = table.Column<string>(type: "text", nullable: true),
                    StreetCode = table.Column<int>(type: "integer", nullable: true),
                    TikBinyanNum = table.Column<string>(type: "text", nullable: true),
                    MivneNum = table.Column<string>(type: "text", nullable: true),
                    FiziNum = table.Column<int>(type: "integer", nullable: true),
                    SugMivne = table.Column<int>(type: "integer", nullable: true),
                    IsUnitInEmptyBldg = table.Column<int>(type: "integer", nullable: true),
                    DamagePercentage = table.Column<int>(type: "integer", nullable: true),
                    FloorNum = table.Column<int>(type: "integer", nullable: true),
                    OwnerDetails = table.Column<string>(type: "text", nullable: true),
                    HolderDetails = table.Column<string>(type: "text", nullable: true),
                    PropNum = table.Column<int>(type: "integer", nullable: true),
                    IsPlannedEmpty = table.Column<int>(type: "integer", nullable: true),
                    WaterConsumption = table.Column<int>(type: "integer", nullable: true),
                    TimeFromLastWaterConsumption = table.Column<string>(type: "text", nullable: true),
                    ElectricityConsumption = table.Column<int>(type: "integer", nullable: true),
                    TimeFromLastElectricityConsumption = table.Column<string>(type: "text", nullable: true),
                    ReasonForNonUse = table.Column<string>(type: "text", nullable: true),
                    Yeud = table.Column<int>(type: "integer", nullable: true),
                    ActualUse = table.Column<string>(type: "text", nullable: true),
                    ArnonaUseType = table.Column<string>(type: "text", nullable: true),
                    ArnonaCodeShimush = table.Column<int>(type: "integer", nullable: true),
                    HeterBniya = table.Column<int>(type: "integer", nullable: true),
                    Tofes4 = table.Column<int>(type: "integer", nullable: true),
                    HarigatBniya = table.Column<int>(type: "integer", nullable: true),
                    IsurShimushHoreg = table.Column<int>(type: "integer", nullable: true),
                    ParcelSize = table.Column<string>(type: "text", nullable: true),
                    BuildRights = table.Column<string>(type: "text", nullable: true),
                    ShtachBanuySum = table.Column<string>(type: "text", nullable: true),
                    FloorSum = table.Column<int>(type: "integer", nullable: true),
                    KidumTichnunStatus = table.Column<int>(type: "integer", nullable: true),
                    SugBaalut = table.Column<int>(type: "integer", nullable: true),
                    WantToSell = table.Column<int>(type: "integer", nullable: true),
                    WantToRent = table.Column<int>(type: "integer", nullable: true),
                    HezkaMove = table.Column<int>(type: "integer", nullable: true),
                    WhatsInside = table.Column<int>(type: "integer", nullable: true),
                    CanMuniFix = table.Column<int>(type: "integer", nullable: true),
                    OwnerPosition = table.Column<string>(type: "text", nullable: true),
                    MiuniPosition = table.Column<string>(type: "text", nullable: true),
                    StandardMark = table.Column<int>(type: "integer", nullable: true),
                    InPilot = table.Column<int>(type: "integer", nullable: true),
                    Shiabud = table.Column<int>(type: "integer", nullable: true),
                    OwnerUnderExec = table.Column<int>(type: "integer", nullable: true),
                    LegalDespute = table.Column<int>(type: "integer", nullable: true),
                    PtorStage = table.Column<int>(type: "integer", nullable: true),
                    ArnonaDept = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    maintenance = table.Column<int>(type: "integer", nullable: true),
                    DangerousBldg = table.Column<int>(type: "integer", nullable: true),
                    SuspectedDangerousBldg = table.Column<int>(type: "integer", nullable: true),
                    Itum = table.Column<int>(type: "integer", nullable: true),
                    LandQuality = table.Column<int>(type: "integer", nullable: true),
                    BldgRightsNotUsed = table.Column<int>(type: "integer", nullable: true),
                    ForShimur = table.Column<int>(type: "integer", nullable: true),
                    FutureYeud = table.Column<string>(type: "text", nullable: true),
                    FutureUse = table.Column<string>(type: "text", nullable: true),
                    PikuachKlali = table.Column<string>(type: "text", nullable: true),
                    PikuachAlBniya = table.Column<string>(type: "text", nullable: true),
                    TzavDangerBldg = table.Column<string>(type: "text", nullable: true),
                    HasDangerousBldgOrder = table.Column<int>(type: "integer", nullable: true),
                    TzavShiputzFronts = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_Streets_StreetId",
                        column: x => x.StreetId,
                        principalTable: "Streets",
                        principalColumn: "StreetId",
                        onDelete: ReferentialAction.SetNull);
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
                name: "IX_Buildings_StreetId",
                table: "Buildings",
                column: "StreetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSystemSnapshots_BuildingId",
                table: "ExternalSystemSnapshots",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Streets_Name",
                table: "Streets",
                column: "Name");
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

            migrationBuilder.DropTable(
                name: "Streets");
        }
    }
}
