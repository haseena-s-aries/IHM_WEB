using System.ComponentModel.DataAnnotations;

namespace Survery_Certification_App_Consume.Models
{
    public class ProfileDto
    {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string? Role { get; set; }

        
    }

    }

