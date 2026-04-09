namespace server.Service.Models.TaskComment
{
    public class AddCommentModel
    {
        public int TaskId { get; set; }
        public string Content { get; set; }
        public int? ParentId { get; set; }
    }
}
