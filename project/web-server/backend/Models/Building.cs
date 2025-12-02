namespace WebServer.Models;

public class Building
{
    public int Id { get; set; }
    public string FldId { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string BldSivug { get; set; } = string.Empty;
    public BuildingStatus ShikumStatus { get; set; } = BuildingStatus.Unknown;
    public string StatusSummary { get; set; } = string.Empty;
    public DateTime? StatusSummaryUpdatedAt { get; set; }
    public string Complaints { get; set; } = string.Empty;
    public string PhotoUrls { get; set; } = string.Empty;

    // Location / zoning
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? GushM { get; set; }
    public string? ParcelM { get; set; }
    public string? GushS { get; set; }
    public string? ParcelS { get; set; }
    public string? ParcelTat { get; set; }
    public string? StreetCode { get; set; }
    public string? TkBinyanNum { get; set; }
    public string? MivneNum { get; set; }
    public string? FiziNum { get; set; }

    // Usage / inspection
    public bool? IsUnitInEmptyBuilding { get; set; }
    public decimal? DamagePercentage { get; set; }
    public int? FloorNum { get; set; }
    public string? OwnerDetails { get; set; }
    public string? HolderDetails { get; set; }
    public string? PropertyNumber { get; set; }

    // Consumption
    public decimal? WaterConsumption { get; set; }
    public int? DaysSinceWaterConsumption { get; set; }
    public decimal? ElectricityConsumption { get; set; }
    public int? DaysSinceElectricityConsumption { get; set; }

    // Planning & rights
    public string? IntendedUse { get; set; }
    public string? ActualUse { get; set; }
    public string? ArnonaCodeShimush { get; set; }
    public string? HeterBniya { get; set; }
    public string? Tofes4 { get; set; }
    public string? HarigatBniya { get; set; }
    public string? IsurShimushHoreg { get; set; }
    public double? ParcelSize { get; set; }
    public double? BuildRights { get; set; }
    public double? ShtachBantuySum { get; set; }
    public double? FloorSum { get; set; }
    public string? KidumTichnunStatus { get; set; }
    public string? OwnershipType { get; set; }
    public bool? WantToSell { get; set; }
    public bool? WantToRent { get; set; }
    public string? HezkaMove { get; set; }
    public string? WhatsInside { get; set; }
    public bool? CanMunicipalityFix { get; set; }
    public string? OwnerPosition { get; set; }
    public string? MiuuniPosition { get; set; }
    public string? StandardMark { get; set; }
    public bool? InPilot { get; set; }

    // Legal / maintenance / risk
    public bool? Shiabud { get; set; }
    public bool? OwnerUnderExecution { get; set; }
    public bool? LegalDispute { get; set; }
    public bool? ArnonaDebt { get; set; }
    public string? MaintenanceStatus { get; set; }
    public bool? DangerousBuilding { get; set; }
    public string? LandQuality { get; set; }
    public bool? BuildingRightsNotUsed { get; set; }
    public bool? ForPreservation { get; set; }
    public string? FutureYeud { get; set; }
    public string? FutureUse { get; set; }
    public string? PikuachKlali { get; set; }
    public string? PikuachAlBniya { get; set; }
    public string? TzavDangerBuilding { get; set; }
    public string? TzavShiputzFronts { get; set; }

    public ICollection<BuildingLog> Logs { get; set; } = new List<BuildingLog>();
    public ICollection<ExternalSystemSnapshot> ExternalSnapshots { get; set; } = new List<ExternalSystemSnapshot>();
}
