namespace server.Service.Models.Block
{
    public class BlockResponseModel
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public int? ParentBlockId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string? PropsJson { get; set; }
        public string OrderKey { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? RowVersion { get; set; }
    }
}