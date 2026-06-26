using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Survey_Certification_App.Models;

namespace Survery_Certification_App_Consume.Models
{
    public class SeaFastening
    {


        public int Id { get; set; }

        public string? UserId { get; set; }
        public string HeaderImagePath { get; set; } // For image dropdown


        [Required(ErrorMessage = "CertificateTitle is required.")]
     
        public string CertificateTitle { get; set; }


        [Required(ErrorMessage = "IssuedBy is required.")]
      
        public string IssuedBy { get; set; }

        [Required(ErrorMessage = "ClientName is required.")]
      
        public string Client { get; set; }

        [Required(ErrorMessage = "TransportUnit is required.")]
       
        public string TransportUnit { get; set; }

        [Required(ErrorMessage = "ProjectTitle is required.")]
      
        public string ProjectTitle { get; set; }

        [Required(ErrorMessage = "FromPort is required.")]
       
        public string FromPort { get; set; }

        public string CargoType { get; set; }


        public string PdfFilePath { get; set; }

        [Required(ErrorMessage = "ToPort is required.")]
      
        public string ToPort { get; set; }

     
        public string JobNumber { get; set; }



        [Required(ErrorMessage = "Certificate Number is required.")]
    
        public string CertificateNumber { get; set; }
        public DateOnly? IssuedDate { get; set; }

        public int SlNo { get; set; }
        public List<Recommendation> Recommendations { get; set; } 

        public List<CargoDetails> CargoDetails { get; set; }


       public bool IsCargoDetailsNA { get; set; }

        public bool IncludeCargoDetails { get; set; }
        public string? StampPath { get; set; } // Store uploaded stamp path

        public string? SignaturePath { get; set; }

        public string? SignatureClient { get; set; }



        [Required(ErrorMessage = "Cargo is required.")]
       

        public string? Cargo { get; set; }

        public int SerialNo { get; set; }
        public string? Location { get; set; }

        public string? IDMarkings { get; set; }
        public string? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? Remarks { get; set; } 

        public bool? IncludeAnnexure2 { get; set; }  // 🔥 Add this flag

       


    }
}
