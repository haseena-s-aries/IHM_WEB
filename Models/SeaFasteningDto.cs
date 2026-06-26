namespace Survery_Certification_App_Consume.Models
{
    public class SeaFasteningDto
    {

        public int Id { get; set; }
        public string IssuedBy { get; set; }
        public DateOnly? IssuedDate { get; set; }
        public string StampPath { get; set; }
        public string SignaturePath { get; set; }
    }

    public class Recommendation
    {
       public int Id { get; set; }  
        public string? RecommendationText { get; set; }
     
    }
}
