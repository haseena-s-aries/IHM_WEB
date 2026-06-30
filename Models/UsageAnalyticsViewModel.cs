namespace Survery_Certification_App_Consume.Models
{
    public class UsageAnalyticsViewModel
    {
        public UsageAnalyticsFilters Filters { get; set; } = new();
        public UsageAnalyticsUserSummary UserSummary { get; set; } = new();
        public List<UsageAnalyticsGroupSummary> DepartmentSummary { get; set; } = new();
        public List<UsageAnalyticsGroupSummary> RoleSummary { get; set; } = new();
        public List<UsageAnalyticsModuleUsage> ModuleWiseUsage { get; set; } = new();
        public List<UsageAnalyticsUserActivity> LastActivityPerUser { get; set; } = new();
        public List<UsageAnalyticsMonthlyTrend> MonthlyUsageTrend { get; set; } = new();
        public UsageAnalyticsTopLists TopLists { get; set; } = new();
        public UsageAnalyticsAvailableFilters AvailableFilters { get; set; } = new();
        public string LimitationNote { get; set; } = string.Empty;
        public UsageAnalyticsImplementationReport ImplementationReport { get; set; } = new();
        public string? LoadError { get; set; }

        public bool HasDepartmentSummary =>
            DepartmentSummary.Any(x => !string.Equals(x.Name, "Not available", StringComparison.OrdinalIgnoreCase));
    }

    public class UsageAnalyticsFilters
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public int? UserId { get; set; }
        public string? Module { get; set; }
        public string? ActivityStatus { get; set; }
    }

    public class UsageAnalyticsUserSummary
    {
        public int TotalRegisteredUsers { get; set; }
        public int? ActiveUsers { get; set; }
        public int? InactiveUsers { get; set; }
        public int UsersWithActivityInRange { get; set; }
        public int UsersWithNoActivityLast30Days { get; set; }
        public int UsersWithNoActivityLast60Days { get; set; }
        public int UsersWithNoActivityLast90Days { get; set; }
    }

    public class UsageAnalyticsGroupSummary
    {
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int? ActiveUserCount { get; set; }
    }

    public class UsageAnalyticsModuleUsage
    {
        public string ModuleName { get; set; } = string.Empty;
        public int ActivityCount { get; set; }
        public int CreatedRecordCount { get; set; }
        public int? UpdatedRecordCount { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int? UniqueActiveUsers { get; set; }
    }

    public class UsageAnalyticsUserActivity
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public int TotalActivityCount { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public string ActivityStatus { get; set; } = string.Empty;
    }

    public class UsageAnalyticsMonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public int ActivityCount { get; set; }
        public int? UniqueActiveUsers { get; set; }
    }

    public class UsageAnalyticsTopLists
    {
        public List<UsageAnalyticsUserActivity> TopActiveUsers { get; set; } = new();
        public List<UsageAnalyticsModuleUsage> MostUsedModules { get; set; } = new();
        public List<UsageAnalyticsModuleUsage> LeastUsedModules { get; set; } = new();
        public List<UsageAnalyticsUserActivity> RecentlyActiveUsers { get; set; } = new();
        public List<UsageAnalyticsUserActivity> NeverActiveUsers { get; set; } = new();
    }

    public class UsageAnalyticsAvailableFilters
    {
        public List<string> Roles { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public List<UsageAnalyticsUserOption> Users { get; set; } = new();
        public List<string> Modules { get; set; } = new();
        public List<string> ActivityStatuses { get; set; } = new();
    }

    public class UsageAnalyticsUserOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UsageAnalyticsImplementationReport
    {
        public List<string> TablesUsed { get; set; } = new();
        public List<string> ColumnsUsed { get; set; } = new();
        public List<string> MetricsAvailable { get; set; } = new();
        public List<string> MetricsNotAvailable { get; set; } = new();
        public List<string> Limitations { get; set; } = new();
    }
}
