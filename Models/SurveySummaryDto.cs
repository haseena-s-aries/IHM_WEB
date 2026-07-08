namespace ASCENT.Models
{
    public class SurveySummaryDto
    {
        public int TotalSurveyJobs { get; set; }
        public int IhmSurveyJobs { get; set; }
        public int ExSurveyJobs { get; set; }
        public int SubmittedJobs { get; set; }
        public int ApprovedJobs { get; set; }
        public int ReturnedJobs { get; set; }
        public int AttentionJobs { get; set; }
        public int ThisWeekCompletedJobs { get; set; }
        public int ThisMonthCompletedJobs { get; set; }
        public int ThisYearCompletedJobs { get; set; }
        public int OverallCompletedJobs { get; set; }

        public List<SurveyCompletionTrendPointDto> CompletionTrend { get; set; } = new();
    }

    public class SurveyCompletionTrendPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
