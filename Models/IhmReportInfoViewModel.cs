using Newtonsoft.Json;

namespace Survery_Certification_App_Consume.Models

{

    public class IhmReportInfoViewModel

    {

        public int JobId { get; set; }

        public int? VesselId { get; set; }

        public string? VesselName { get; set; }
        public decimal? Deadweight { get; set; }
        public int? YearBuilt { get; set; }
        public string? OwnerName { get; set; }

        public string? ImoNumber { get; set; }

        public string? OfficialNumber { get; set; }

        public string? VesselType { get; set; }

        public string? Flag { get; set; }

        public string? PortOfRegistry { get; set; }

        public string? ClassSociety { get; set; }

        public string? CallSign { get; set; }

        public string? Builder { get; set; }

        public DateTime? DateOfBuild { get; set; }

        public decimal? LengthOverall { get; set; }

        public decimal? LengthBetweenPerpendiculars { get; set; }

        public decimal? Breadth { get; set; }

        public decimal? Depth { get; set; }

        public decimal? DraughtMoulded { get; set; }

        public decimal? GrossTonnage { get; set; }

        public decimal? NetTonnage { get; set; }


        public string? ShipManager { get; set; }

        public string? JobNumber { get; set; }

        public string? ProjectName { get; set; }

        public string? SiteOfCheck { get; set; }

        public DateTime? DateOfSurvey { get; set; }

        public int? CompanyId { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyDisplayName { get; set; }

        public string? CompanyAddress { get; set; }

        public string? CompanyTel { get; set; }

        public string? CompanyFax { get; set; }

        public string? CompanyWebsite { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyLogoPath { get; set; }

        public string? ClientName { get; set; }

        public string? ClientLogoPath { get; set; }

        public string? PreparedBy { get; set; }

        public string? CheckedBy { get; set; }

        public string? ApprovedBy { get; set; }

        public string? Uid { get; set; }

        public string? DocumentNo { get; set; }

        public DateTime? DocumentDate { get; set; }

        public string? RevisionNo { get; set; }
        public string? HazardousMaterialNoFindings { get; set; }
        public string? HazardousMaterialFindings { get; set; }

        public string? LabReportNo { get; set; }

        public DateTime? LabReportingDate { get; set; }

        public string? HazmatExpert { get; set; }

        public string? ShipManagerContact { get; set; }

        public string? SamplingMethodology { get; set; }

        public string? SamplingOfFragmentsOfPaints { get; set; }

        public string? Laboratory { get; set; }

        public string? ChemicalAnalysisMethod { get; set; }

        public string? LocationOfVisualSamplingCheck { get; set; }

        public string? OhsJobDetail { get; set; }

        public string? OhsClient { get; set; }

        public string? OhsJobLocation { get; set; }

        public string? OhsJobNo { get; set; }

        public string? OhsProjectInCharge { get; set; }

        public string? OhsDivisionBranch { get; set; }

        public string? OhsRaNo { get; set; }

        public string? OhsRevNo { get; set; }

        public DateTime? OhsRevisionDate { get; set; }

        public string? OhsPreparedBy { get; set; }

        public string? OhsPreparedBySignPath { get; set; }

        public DateTime? OhsPreparedDate { get; set; }

        public string? OhsApprovedBy { get; set; }

        public string? OhsApprovedBySignPath { get; set; }

        public DateTime? OhsApprovedDate { get; set; }

        public string? NecessaryInformationText { get; set; }

        public string? AppendixFiveInformationText { get; set; }

        public List<IhmReportRevisionViewModel> Revisions { get; set; } = new();

        public List<IhmPaintCoatingViewModel> PaintAndCoatingSystems { get; set; } = new();

        public List<IhmEquipmentMachineryViewModel> EquipmentAndMachinery { get; set; } = new();

        public List<IhmStructureHullViewModel> StructureAndHull { get; set; } = new();

        public List<IhmOhsPersonViewModel> OhsPeople { get; set; } = new();

        public List<IhmOhsRiskViewModel> OhsRiskAssessmentRows { get; set; } = new();

        public List<IhmAppendixPointViewModel> AppendixOneResultPoints { get; set; } = new();

        [JsonIgnore]

        public List<IhmAttachmentViewModel> AppendixTwoAttachments { get; set; } = new();

        [JsonIgnore]

        public List<IhmAttachmentViewModel> AppendixFiveAttachments { get; set; } = new();

        [JsonIgnore]

        public List<IhmAttachmentViewModel> AppendixSevenAttachments { get; set; } = new();

        [JsonIgnore]

        public List<IhmAttachmentViewModel> CustomAppendices { get; set; } = new();

        public object ToApiRequest()

        {

            return new

            {

                CompanyId,

                CompanyName,
                CompanyDisplayName,

                CompanyAddress,

                CompanyTel,

                CompanyFax,

                CompanyWebsite,

                CompanyEmail,

                CompanyLogoPath,

                ClientName,

                ClientLogoPath,

                PreparedBy,

                CheckedBy,

                ApprovedBy,

                Uid,

                DocumentNo,

                DocumentDate,

                RevisionNo,

                LabReportNo,

                LabReportingDate,
                HazardousMaterialNoFindings,
                HazardousMaterialFindings,

                ShipManagerContact,

                HazmatExpert,

                SamplingMethodology,

                SamplingOfFragmentsOfPaints,

                Laboratory,

                ChemicalAnalysisMethod,

                LocationOfVisualSamplingCheck,

                OhsJobDetail,

                OhsClient,

                OhsJobLocation,

                OhsJobNo,

                OhsProjectInCharge,

                OhsDivisionBranch,

                OhsRaNo,

                OhsRevNo,

                OhsRevisionDate,

                OhsPreparedBy,

                OhsPreparedBySignPath,

                OhsPreparedDate,

                OhsApprovedBy,

                OhsApprovedBySignPath,

                OhsApprovedDate,

                Revisions,

                PaintAndCoatingSystems,

                EquipmentAndMachinery,

                StructureAndHull,

                OhsPeople,

                OhsRiskAssessmentRows,

                AppendixOneResultPoints,

                NecessaryInformationPoints = Lines(NecessaryInformationText)

                    .Select((description, index) => new { description, sortOrder = index + 1 }),

                AppendixFiveInformationPoints = Lines(AppendixFiveInformationText)

                    .Select((description, index) => new { description, sortOrder = index + 1 })

            };

        }

        public object ToVesselRequest()

        {

            return new

            {

                VesselName = VesselName ?? string.Empty,

                ImoNumber,

                OfficialNumber,

                VesselType,

                Flag,

                PortOfRegistry,

                ClassSociety,

                CallSign,

                Builder,

                DateOfBuild,

                LengthOverall,

                LengthBetweenPerpendiculars,

                Breadth,

                Depth,

                DraughtMoulded,

                GrossTonnage,

                NetTonnage,
                Deadweight,

                YearBuilt,

                OwnerName,

                ManagerName = ShipManager,

                ClientName

            };

        }

        private static IEnumerable<string> Lines(string? value)

        {

            return (value ?? string.Empty)

                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)

                .Select(x => x.Trim())

                .Where(x => x.Length > 0);

        }

    }

    public class IhmReportApiResponse

    {

        public IhmReportInfoData? ReportInfo { get; set; }

        public IhmVesselViewModel? Vessel { get; set; }

        public IhmJobViewModel? Job { get; set; }

        public List<IhmReportRevisionViewModel> Revisions { get; set; } = new();

        public List<IhmPaintCoatingViewModel> PaintAndCoatingSystems { get; set; } = new();

        public List<IhmEquipmentMachineryViewModel> EquipmentAndMachinery { get; set; } = new();

        public List<IhmStructureHullViewModel> StructureAndHull { get; set; } = new();

        public List<IhmDescriptionPointViewModel> NecessaryInformationPoints { get; set; } = new();

        public List<IhmOhsPersonViewModel> OhsPeople { get; set; } = new();

        public List<IhmOhsRiskViewModel> OhsRiskAssessmentRows { get; set; } = new();

        public List<IhmAppendixPointViewModel> AppendixOneResultPoints { get; set; } = new();

        public List<IhmDescriptionPointViewModel> AppendixFiveInformationPoints { get; set; } = new();

        public List<IhmAttachmentViewModel> AppendixTwoAttachments { get; set; } = new();

        public List<IhmAttachmentViewModel> AppendixFiveAttachments { get; set; } = new();

        public List<IhmAttachmentViewModel> AppendixSevenAttachments { get; set; } = new();

        public List<IhmAttachmentViewModel> CustomAppendices { get; set; } = new();

    }

    public class IhmReportInfoData

    {

        public int? CompanyId { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyAddress { get; set; }

        public string? CompanyTel { get; set; }

        public string? CompanyFax { get; set; }

        public string? CompanyWebsite { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyLogoPath { get; set; }

        public string? ClientName { get; set; }

        public string? ClientLogoPath { get; set; }

        public string? PreparedBy { get; set; }

        public string? CheckedBy { get; set; }

        public string? ApprovedBy { get; set; }

        public string? Uid { get; set; }

        public string? DocumentNo { get; set; }

        public DateTime? DocumentDate { get; set; }

        public string? RevisionNo { get; set; }

        public string? LabReportNo { get; set; }

        public DateTime? LabReportingDate { get; set; }
        public string? HazardousMaterialNoFindings { get; set; }
        public string? HazardousMaterialFindings { get; set; }

        public string? ShipManagerContact { get; set; }

        public string? HazmatExpert { get; set; }

        public string? SamplingMethodology { get; set; }

        public string? SamplingOfFragmentsOfPaints { get; set; }

        public string? Laboratory { get; set; }

        public string? ChemicalAnalysisMethod { get; set; }

        public string? LocationOfVisualSamplingCheck { get; set; }

        public string? OhsJobDetail { get; set; }

        public string? OhsClient { get; set; }

        public string? OhsJobLocation { get; set; }

        public string? OhsJobNo { get; set; }

        public string? OhsProjectInCharge { get; set; }

        public string? OhsDivisionBranch { get; set; }

        public string? OhsRaNo { get; set; }

        public string? OhsRevNo { get; set; }

        public DateTime? OhsRevisionDate { get; set; }

        public string? OhsPreparedBy { get; set; }

        public string? OhsPreparedBySignPath { get; set; }

        public DateTime? OhsPreparedDate { get; set; }

        public string? OhsApprovedBy { get; set; }

        public string? OhsApprovedBySignPath { get; set; }

        public DateTime? OhsApprovedDate { get; set; }

    }

    public class IhmVesselViewModel

    {

        public int Id { get; set; }

        public string? VesselName { get; set; }

        public string? ImoNumber { get; set; }

        public string? OfficialNumber { get; set; }

        public string? VesselType { get; set; }

        public string? Flag { get; set; }

        public string? PortOfRegistry { get; set; }

        public string? ClassSociety { get; set; }

        public string? CallSign { get; set; }

        public string? Builder { get; set; }

        public DateTime? DateOfBuild { get; set; }

        public decimal? LengthOverall { get; set; }

        public decimal? LengthBetweenPerpendiculars { get; set; }

        public decimal? Breadth { get; set; }

        public decimal? Depth { get; set; }

        public decimal? DraughtMoulded { get; set; }

        public decimal? GrossTonnage { get; set; }

        public decimal? NetTonnage { get; set; }
        public decimal? Deadweight { get; set; }

        public int? YearBuilt { get; set; }

        public string? OwnerName { get; set; }

        public string? ManagerName { get; set; }

    }

    public class IhmJobViewModel

    {

        public string? JobNumber { get; set; }

        public string? VesselName { get; set; }

        public string? ImoNumber { get; set; }

        public string? ClientName { get; set; }

        public string? AuditPort { get; set; }

        public string? SiteName { get; set; }

        public DateTime? StartDate { get; set; }

        public string? ProjectName { get; set; }

    }

    public class IhmReportRevisionViewModel

    {

        public string? Uid { get; set; }

        public string? RevisionNo { get; set; }

        public DateTime? RevisionDate { get; set; }

        public string? Description { get; set; }

        public string? PreparedBy { get; set; }

        public string? CheckedBy { get; set; }

        public string? ApprovedBy { get; set; }

    }

    public class IhmPaintCoatingViewModel

    {

        public string? ApplicationOfPaint { get; set; }

        public string? NameOfPaint { get; set; }

        public string? Location { get; set; }

        public string? MaterialClassification { get; set; }

        public string? Mass { get; set; }

        public string? Unit { get; set; }

        public string? Remarks { get; set; }

    }

    public class IhmEquipmentMachineryViewModel

    {

        public int? IhmCheckpointId { get; set; }

        public string? EquipmentName { get; set; }

        public string? Location { get; set; }

        public string? MaterialClassification { get; set; }

        public string? PartsWereUsed { get; set; }

        public string? UnitKg { get; set; }

        public string? QuantityNo { get; set; }

        public string? TotalKg { get; set; }

        public string? Remarks { get; set; }

        public bool IsHidden { get; set; }

        public int SortOrder { get; set; }

    }

    public class IhmStructureHullViewModel

    {

        public string? EquipmentName { get; set; }

        public string? Location { get; set; }

        public string? MaterialClassification { get; set; }

        public string? PartsWereUsed { get; set; }

        public string? ApproximateQuantity { get; set; }

        public string? Remarks { get; set; }

    }

    public class IhmOhsPersonViewModel

    {

        public string RoleType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

    }

    public class IhmOhsRiskViewModel

    {

        public string? Activity { get; set; }

        public string? HazardDescription { get; set; }

        public string? Risks { get; set; }

        public string? InitialRiskP { get; set; }

        public string? InitialRiskS { get; set; }

        public string? InitialRiskR { get; set; }

        public string? InitialAlarp { get; set; }

        public string? ControlMeasures { get; set; }

        public string? ResidualRiskP { get; set; }

        public string? ResidualRiskS { get; set; }

        public string? ResidualRiskR { get; set; }

        public string? ResidualAlarp { get; set; }

        public string? ResponsiblePerson { get; set; }

        public int SortOrder { get; set; }

    }

    public class IhmAppendixPointViewModel

    {

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int SortOrder { get; set; }

    }

    public class IhmDescriptionPointViewModel

    {

        public string? Description { get; set; }

        public int SortOrder { get; set; }

    }

    public class IhmAttachmentViewModel

    {

        public int Id { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public string AppendixType { get; set; } = string.Empty;

        public int? AppendixNo { get; set; }

        public string? AppendixTitle { get; set; }

        public int SortOrder { get; set; }

    }

}

