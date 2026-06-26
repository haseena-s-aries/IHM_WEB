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


        public string? Password { get; set; }

        public bool IsApproved { get; set; } = false; // default false


        public string? RequestedRole { get; set; }     // store desired role like "Manager" or "Admin"

    }
}
