namespace WebServer.Models;

public enum BuildingStatus
{
    Unknown = 0,
    MappingBarriersAndSolution = 1,
    OwnershipTransfer = 2,
    DevelopmentBarriers = 3,
    OwnerConsideringAction = 4,
    PreparingRehabPlan = 5,
    PlanApprovedPreparingExecution = 6,
    InExecution = 7,
    OccupancyProcess = 8
}
