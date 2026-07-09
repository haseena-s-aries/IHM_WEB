using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Survery_Certification_App_Consume.Models
{
    public class LoginUserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required, Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public string? ProjectName { get; set; }
        public string? RoleName { get; set; }


    }

}
