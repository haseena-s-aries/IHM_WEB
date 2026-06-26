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

        public int TotalUsers { get; set; }

    }

    public class SavedReportViewModel
    {
        public int Id { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? VesselName { get; set; }
        public string? Reference { get; set; }
        public DateOnly? SavedDate { get; set; }
        public DateTime? SavedAt { get; set; }
    }

    public class SurveySummaryDto
    {
        public int TotalSurveyJobs { get; set; }
        public int IhmSurveyJobs { get; set; }
        public int ExSurveyJobs { get; set; }
        public int SubmittedJobs { get; set; }
        public int ApprovedJobs { get; set; }
        public int ReturnedJobs { get; set; }
    }
}
