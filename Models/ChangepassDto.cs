namespace Survery_Certification_App_Consume.Models
{
    public class ChangepassDto
    {
        public int UserId { get; set; }  // ✅ Ensure it's an integer
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
