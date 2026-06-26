using System.ComponentModel.DataAnnotations;

namespace Survey_Certification_App.DTOS
{
    public class EmailDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }

    public class OtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}