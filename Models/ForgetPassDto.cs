namespace Survey_Certification_App.DTOS
{
    public class ForgetPassDto
    {
        public string Email { get; set; }
    }

    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }  // Make sure this exists
        public string NewPassword { get; set; }
    }
}

