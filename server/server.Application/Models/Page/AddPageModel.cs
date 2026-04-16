namespace server.Service.Models.Page
{
    public class AddPageModel
    {
        public int WorkspaceId { get; set; }

        public int? ParentPageId { get; set; }

        public string Title { get; set; } = default!;

        public string? Icon { get; set; }

        public string? CoverImage { get; set; }
    }
}