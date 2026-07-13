using Survey_Certification_App.Models;

namespace Survery_Certification_App_Consume.Models
{


    public class AdminDashboardViewModel
    {

        public List<PendingUser> PendingUsers { get; set; } = new();
        public List<UserDto> Users { get; set; } = new();
        public List<SavedReportViewModel> SavedReports { get; set; } = new();
        public TotalCertificateCountDto CertificateCounts { get; set; } = new();
        public SurveySummaryDto SurveySummary { get; set; } = new();
        public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
        public int TotalUsers { get; set; }
        public List<ASCENT.Models.VesselSummaryDto> FleetSummary { get; set; } = new();
        public string? FleetErrorMessage { get; set; }
    }


    public class UpcomingDeadlineDto   // <-- also missing, add this
    {
        public string Title { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
    }
    public class SavedReportViewModel
    {
        public int Id { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? VesselName { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateOnly? SavedDate { get; set; }
        public DateTime? SavedAt { get; set; }
    }

    public class AssignedReportViewModel
    {
        public int AssignmentId { get; set; }
        public int ReportId { get; set; }
        public int? SurveyJobId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? VesselName { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool HasFile { get; set; }
        public string? ViewUrl { get; set; }
        public string? DownloadUrl { get; set; }
    }

    public class SurveySummaryDto
    {
        public int TotalSurveyJobs { get; set; }
        public int IhmSurveyJobs { get; set; }
        public int ExSurveyJobs { get; set; }
        public int SubmittedJobs { get; set; }
        public int ApprovedJobs { get; set; }
        public int ReturnedJobs { get; set; }
        public int AttentionJobs { get; set; }
        public int LiveIhmJobs { get; set; }
        public int ThisWeekCompletedJobs { get; set; }
        public int ThisMonthCompletedJobs { get; set; }
        public int ThisYearCompletedJobs { get; set; }
        public int OverallCompletedJobs { get; set; }
        public int IhmUsersCount { get; set; }
        public int ExSurveyUsersCount { get; set; }

        public List<SurveyCompletionTrendPointDto> CompletionTrend { get; set; } = new();
    }


    public class SurveyCompletionTrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }



}
