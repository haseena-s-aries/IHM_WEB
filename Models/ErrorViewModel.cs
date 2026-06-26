namespace Survery_Certification_App_Consume.Models
{
    public class ErrorViewModel
    {


        public string? Message { get; set; }
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
