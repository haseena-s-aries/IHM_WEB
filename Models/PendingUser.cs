namespace Survery_Certification_App_Consume.Models
{
    public class PendingUser
    {

        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RequestedRole { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
    }

    public class ProjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
    }
}
