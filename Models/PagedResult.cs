namespace Survey_Certification_App.Models
{
    public class PagedResult<T>
    {

        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
