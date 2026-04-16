using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    public class Block : EntityBase
    {
        public int PageId { get; private set; }
        public int? ParentBlockId { get; private set; }

        public BlockType Type { get; private set; }

        // Nội dung text chính, dùng cho paragraph/heading/todo/code/quote
        public string? TextContent { get; private set; }

        // Dữ liệu linh hoạt cho từng loại block
        // Ví dụ image url, code language, todo checked, rich-text attrs...
        public string? PropsJson { get; private set; }

        // Sắp xếp block trong cùng level
        public string OrderKey { get; private set; }

        public int CreatedBy { get; private set; }
        public int? LastModifiedBy { get; private set; }

        public virtual Page Page { get; private set; } = null!;
        public virtual Block? ParentBlock { get; private set; }
        public virtual ICollection<Block> Children { get; private set; } = new List<Block>();

        protected Block() { }

        public Block(
            int pageId,
            BlockType type,
            string orderKey,
            int createdBy,
            string? textContent = null,
            string? propsJson = null,
            int? parentBlockId = null)
        {
            if (pageId <= 0) throw new DomainException("PageId không hợp lệ.");
            if (createdBy <= 0) throw new DomainException("CreatedBy không hợp lệ.");
            if (string.IsNullOrWhiteSpace(orderKey)) throw new DomainException("OrderKey không hợp lệ.");

            PageId = pageId;
            ParentBlockId = parentBlockId;
            Type = type;
            OrderKey = orderKey.Trim();
            CreatedBy = createdBy;
            TextContent = NormalizeText(textContent);
            PropsJson = NormalizeProps(propsJson);
        }

        public void UpdateText(string? text, int userId)
        {
            EnsureNotDeleted();
            EnsureValidUser(userId);

            TextContent = NormalizeText(text);
            LastModifiedBy = userId;
            MarkUpdated();
        }

        public void UpdateProps(string? propsJson, int userId)
        {
            EnsureNotDeleted();
            EnsureValidUser(userId);

            PropsJson = NormalizeProps(propsJson);
            LastModifiedBy = userId;
            MarkUpdated();
        }

        public void Move(string newOrderKey, int? newParentBlockId, int userId)
        {
            EnsureNotDeleted();
            EnsureValidUser(userId);

            if (string.IsNullOrWhiteSpace(newOrderKey))
                throw new DomainException("OrderKey không hợp lệ.");

            OrderKey = newOrderKey.Trim();
            ParentBlockId = newParentBlockId;
            LastModifiedBy = userId;
            MarkUpdated();
        }

        private static string? NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Trim();
        }

        private static string? NormalizeProps(string? propsJson)
        {
            if (string.IsNullOrWhiteSpace(propsJson)) return null;
            return propsJson.Trim();
        }

        private static void EnsureValidUser(int userId)
        {
            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");
        }
    }
}
