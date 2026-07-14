using System.ComponentModel.DataAnnotations;

namespace Survery_Certification_App_Consume.Models
{
    public class UserDto
    {

        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? Password { get; set; }

        public bool IsApproved { get; set; } = false; // default false


        public string? RequestedRole { get; set; }     // store desired role like "Manager" or "Admin"

        public bool IsRejected { get; set; } = false;

        public List<UserProjectAccessDto> ProjectAccesses { get; set; } = new();
    }

    public class UserProjectAccessDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectCode { get; set; }
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = true;
        public bool IsRejected { get; set; }
    }
}
