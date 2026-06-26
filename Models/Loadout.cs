using System.ComponentModel.DataAnnotations;
using Survey_Certification_App.Models;

namespace Survery_Certification_App_Consume.Models
{
    public class Loaodut
    {


        public int Id { get; set; }

        public string? UserId { get; set; }
        public string HeaderImagePath { get; set; }  // 👈 Add this

        [Required(ErrorMessage = "CertificateTitle is required.")]
      
        public string CertificateTitle { get; set; }


        [Required(ErrorMessage = "OperationType is required.")]
       
        public string OperationType { get; set; }


        [Required(ErrorMessage = "JobNo Number is required.")]
      

        
        public string JobNumber { get; set; }

        [Required(ErrorMessage = "ProjectTitle is required.")]
    
        public string ProjectTitle { get; set; }

        [Required(ErrorMessage = "Certificate Number is required.")]
    
        public string CertificateNumber { get; set; }

        [Required(ErrorMessage = "ClientName is required.")]
      
        public string Client { get; set; }

        [Required(ErrorMessage = "Cargo is required.")]
        

        public string Cargo { get; set; }

        [Required(ErrorMessage = "mode of  is required.")]
     
        public string Modeofoperation { get; set; }



        [Required(ErrorMessage = "IssuedBy is required.")]
      
        public string BargeVessel { get; set; }


        [Required(ErrorMessage = "IssuedBy is required.")]
     
        public string IssuedBy { get; set; }


        [Required(ErrorMessage = "Location is required.")]
     
        public string Location { get; set; }



        public string PdfFilePath { get; set; }
        public DateOnly? IssuedDate { get; set; }

        public bool NoDateCheckbox { get; set; } // This must be bound from the view

        public string StampPath { get; set; } // Store uploaded stamp path

        public string SignaturePath { get; set; }

        public string SignatureClient { get; set; }

        public bool IncludeCargoDetails { get; set; }

        public bool IsCargoDetailsNA { get; set; }
        public List<CargoDetails> CargoDetails { get; set; } = new(); // ✅ Add this

    }
}
