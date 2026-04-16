namespace server.Service.Models.Block
{
    public class AddBlockModel
    {
        public int PageId { get; set; }
        public int? ParentBlockId { get; set; }

        public string Type { get; set; } = null!;

        public string? TextContent { get; set; }
        public string? PropsJson { get; set; }

        public string OrderKey { get; set; } = null!;
    }
}