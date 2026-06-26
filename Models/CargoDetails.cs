using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Survey_Certification_App.Models
{
    public class CargoDetails
    {
        public string SrNo { get; set; }
        public string? Location { get; set; }
        public string? Cargos { get; set; }
        public string? IDMarkings { get; set; }
        public string? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? Remarks { get; set; }
    }
}
