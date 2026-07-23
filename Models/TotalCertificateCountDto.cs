using Survery_Certification_App_Consume.Models;

namespace Survey_Certification_App.Models
{
    public class TotalCertificateCountDto
    {

        public int TotalCertificates { get; set; }
        public int TowageCount { get; set; }
        public int SeaFasteningCount { get; set; }
        public int LoadoutCount { get; set; }
        public int TotalSurveyJobs { get; set; }
        public int IhmSurveyJobs { get; set; }
        public int ExSurveyJobs { get; set; }
        public int SubmittedSurveyJobs { get; set; }
        public int LiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int DueTodayJobs { get; set; }
    }

    public class SurveyorDashboardSummaryDto
    {
        public int LiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int DueTodayJobs { get; set; }
    }
}
