using System.ComponentModel.DataAnnotations;

namespace Survey_Certification_App.Models
{
    public class OtpVerification
    {

        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OtpCode { get; set; }

        public DateTime ExpiryTime { get; set; }
    }
}
