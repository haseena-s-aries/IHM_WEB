namespace Survery_Certification_App_Consume.Models
{
    public class PdfEditorViewModel
    {
        public string ReportType { get; set; } = "";
        public int ReportId { get; set; }
        public string Title { get; set; } = "PDF Editor";
    }

    public class PdfEditorDocument
    {
        public string ReportType { get; set; } = "";
        public int ReportId { get; set; }
        public List<PdfEditorField> Fields { get; set; } = new();
        public List<int> RemovedPages { get; set; } = new();
        public DateTime UpdatedAt { get; set; }

    }

    public class PdfEditorField
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int Page { get; set; } = 1;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 0.25;
        public double Height { get; set; } = 0.04;
        public string Text { get; set; } = "Enter text";
        public float FontSize { get; set; } = 12;
        public bool CoverOriginal { get; set; } = true;
        public string FontWeight { get; set; } = "regular";
        public bool AutoBullet { get; set; }
        public string EditType { get; set; } = "text";
    }
}
