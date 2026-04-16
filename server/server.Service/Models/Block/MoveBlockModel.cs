namespace server.Service.Models.Block
{
    public class MoveBlockModel
    {
        public int? NewParentBlockId { get; set; }
        public string NewOrderKey { get; set; } = null!;
    }
}