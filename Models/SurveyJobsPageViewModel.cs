namespace Survery_Certification_App_Consume.Models
{
    public class SurveyJobsPageViewModel
    {
        public string SurveyType { get; set; } = "IHM Survey";
        public string? IhmCompletionFilter { get; set; }
        public bool IsAdmin { get; set; }
        public List<SurveyJobDto> Jobs { get; set; } = new();
        public List<VesselDto> Vessels { get; set; } = new();
        public List<SurveyorOptionDto> Surveyors { get; set; } = new();
        public List<SurveyorOptionDto> Managers { get; set; } = new();
        public Dictionary<int, List<IhmAreaOptionDto>> AreasByJob { get; set; } = new();
        public Dictionary<int, Dictionary<string, IhmLayoutAttachmentDto>> LayoutAttachmentsByJob { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
    public class IhmImageMarkerDto
    {
        public int Id { get; set; }
        public int SurveyJobId { get; set; }
        public int? IhmAreaId { get; set; }
        public int? IhmCheckpointId { get; set; }
        public string ImagePath { get; set; } = "";
        public decimal XPercent { get; set; }
        public decimal YPercent { get; set; }
        public string? MarkerLabel { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public bool SuppressRender { get; set; }
        public bool IsManualForExistingImage { get; set; }
    }

    public class SaveIhmMarkerRequest
    {
        public int SurveyJobId { get; set; }
        public int? IhmAreaId { get; set; }
        public int? IhmCheckpointId { get; set; }
        public string ImagePath { get; set; } = "";
        public decimal XPercent { get; set; }
        public decimal YPercent { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public bool SuppressRender { get; set; }
        public bool IsManualForExistingImage { get; set; }
    }
    public class IhmLayoutAttachmentDto
    {
        public string Target { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public class IhmAreaOptionDto
    {
        public int Id { get; set; }
        public string AreaName { get; set; } = "";
        public string? LayoutFilePath { get; set; }
        public List<IhmCheckpointOptionDto> Checkpoints { get; set; } = new();
        public string LocationSummary => string.Join(", ", Checkpoints
            .Select(x => x.Location).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(4));
    }

    public class IhmCheckpointOptionDto
    {
        public int Id { get; set; }
        public int SurveyJobId { get; set; }
        public int IhmAreaId { get; set; }
        public string? SampleNo { get; set; }
        public string? Location { get; set; }
        public string? EquipmentName { get; set; }
        public string? Component { get; set; }
        public string? Description { get; set; }
        public string? TableType { get; set; }
        public string? SamplingCheck { get; set; }
        public string? VisualCheck { get; set; }
        public string? CheckResult { get; set; }
        public string? Result { get; set; }
        public string? AuditStatus { get; set; }
        public string? DocumentAnalysisResult { get; set; }
        public string? MaterialCategory { get; set; }
        public string? HazardousMaterial { get; set; }
        public string? QuantityUnitKg { get; set; }
        public string? QuantityNo { get; set; }
        public string? QuantityTotalKg { get; set; }
        public string? ManufacturerBrandName { get; set; }
        public bool IsPchm { get; set; }
        public string? Remarks { get; set; }
        public string? LocationRemarks { get; set; }
        public bool IsVisual => string.Equals(VisualCheck, "V", StringComparison.OrdinalIgnoreCase);
        public bool IsSample => string.Equals(SamplingCheck, "S", StringComparison.OrdinalIgnoreCase);

        public bool IsAudited => IsCompletedAuditValue(AuditStatus);

        private static bool IsCompletedAuditValue(string? value)
        {
            var normalized = (value ?? "")
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            return normalized is "done" or "3" or "audited" or "completed";
        }
    }

    public class IhmSamplingPhotoDto
    {
        public int Id { get; set; }
        public int SurveyJobId { get; set; }
        public int IhmCheckpointId { get; set; }
        public string OriginalFileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime? UploadedAt { get; set; }
        public bool ShowInAppendixThree { get; set; }
        public int AppendixThreeSortOrder { get; set; }
        public string? AppendixThreeEditedFilePath { get; set; }

    }

    public class SurveyJobDto
    {
        public int Id { get; set; }
        public string ModuleType { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string? ProjectName { get; set; }
        public int? VesselId { get; set; }
        public string? VesselName { get; set; }
        public string? ImoNumber { get; set; }
        public string? SiteName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? SurveyDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AuditPort { get; set; }
        public string? AuditAddress { get; set; }
        public string Status { get; set; } = "Draft";
        public string DisplayStatus { get; set; } = "Not Started";
        public int CompletedItems { get; set; }
        public int TotalItems { get; set; }
        public decimal CompletionPercentage { get; set; }
        public bool CanEdit { get; set; }
        public string? AssignedSurveyorName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int? PrimarySurveyorUserId { get; set; }
        public int? SecondarySurveyorUserId { get; set; }
        public int? AssignedManagerId { get; set; }
        public string? AssignedManagerName { get; set; }
        public string? VscpChecklistFilePath { get; set; }
        public string? VscpChecklistFileName { get; set; }
        public DateTime? VscpChecklistUploadedAt { get; set; }
        public List<SurveyAssignmentDto> Assignments { get; set; } = new();
    }

    public class SurveyAssignmentDto
    {
        public int SurveyorUserId { get; set; }
        public string SurveyorName { get; set; } = "";
        public int AssignedByUserId { get; set; }
        public string AssignedByName { get; set; } = "";
        public DateTime AssignedAt { get; set; }
        public string Status { get; set; } = "";
    }

    public class VesselDto
    {
        public int Id { get; set; }
        public string VesselName { get; set; } = "";
        public string? ImoNumber { get; set; }
        public string? OfficialNumber { get; set; }
        public string? VesselType { get; set; }
        public string? Flag { get; set; }
        public string? PortOfRegistry { get; set; }
        public string? ClassSociety { get; set; }
        public decimal? GrossTonnage { get; set; }
        public decimal? NetTonnage { get; set; }
        public decimal? Deadweight { get; set; }
        public decimal? LengthOverall { get; set; }
        public decimal? Breadth { get; set; }
        public decimal? Depth { get; set; }
        public int? YearBuilt { get; set; }
        public string? OwnerName { get; set; }
        public string? ManagerName { get; set; }
        public string? ClientName { get; set; }
        public string? CallSign { get; set; }
        public string? Builder { get; set; }
        public DateTime? DateOfBuild { get; set; }
        public decimal? LengthBetweenPerpendiculars { get; set; }
        public decimal? DraughtMoulded { get; set; }
        public string Status { get; set; } = "Active";
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
    }

    public class VesselMasterViewModel
    {
        public List<VesselDto> Vessels { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class SaveVesselRequest
    {
        public string VesselName { get; set; } = "";
        public string? ImoNumber { get; set; }
        public string? OfficialNumber { get; set; }
        public string? VesselType { get; set; }
        public string? Flag { get; set; }
        public string? PortOfRegistry { get; set; }
        public string? ClassSociety { get; set; }
        public decimal? GrossTonnage { get; set; }
        public decimal? NetTonnage { get; set; }
        public decimal? Deadweight { get; set; }
        public decimal? LengthOverall { get; set; }
        public decimal? Breadth { get; set; }
        public decimal? Depth { get; set; }
        public int? YearBuilt { get; set; }
        public string? OwnerName { get; set; }
        public string? ManagerName { get; set; }
        public string? ClientName { get; set; }
        public string? CallSign { get; set; }
        public string? Builder { get; set; }
        public DateTime? DateOfBuild { get; set; }
        public decimal? LengthBetweenPerpendiculars { get; set; }
        public decimal? DraughtMoulded { get; set; }
    }

    public class SurveyorOptionDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Email) ? FullName : $"{FullName} ({Email})";
    }
    public class ExSurveySummaryDto
    {
        public int SurveyJobId { get; set; }
        public string Status { get; set; } = "";
        public int EquipmentCount { get; set; }
        public int CompletedChecklistCount { get; set; }
        public int DefectCount { get; set; }
        public int OpenDefectCount { get; set; }
        public int PhotoCount { get; set; }
    }
}
