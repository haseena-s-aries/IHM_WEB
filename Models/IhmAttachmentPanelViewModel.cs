namespace Survery_Certification_App_Consume.Models
{
    public class IhmAttachmentPanelViewModel
    {
        public int JobId { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string AppendixType { get; set; } = string.Empty;
        public List<IhmAttachmentViewModel> Attachments { get; set; } = new();
    }

    public class IhmAttachmentListViewModel
    {
        public int JobId { get; set; }
        public List<IhmAttachmentViewModel> Attachments { get; set; } = new();
    }
}
