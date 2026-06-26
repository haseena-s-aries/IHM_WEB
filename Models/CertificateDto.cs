namespace Survery_Certification_App_Consume.Models
{
    public class CertificateDto
    {

        public int Id { get; set; }

        public string ProjectTitle { get; set; }
        public string CertificateNumber { get; set; }
        public string Type { get; set; } // Towage, Sea Fastening, Loadout
        public string ClientName { get; set; }
        public string JobNumber { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } // Active / Archived

    }
}
