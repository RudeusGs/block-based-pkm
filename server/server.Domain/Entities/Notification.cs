using server.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Domain.Entities
{
    public class Notification : EntityBase
    {
        public int UserId { get; private set; } // Người nhận thông báo
        public int? WorkspaceId { get; private set; } 
        
        public NotificationType Type { get; private set; }
        
        public string Title { get; private set; } = null!;
        public string Message { get; private set; } = null!;
        
        public string? ReferenceId { get; private set; }
        public string? ReferenceType { get; private set; }
        
        public bool IsRead { get; private set; }
        public DateTime? ReadAtUtc { get; private set; }

        public virtual User User { get; private set; } = null!;

        protected Notification() { }

        public Notification(int userId, int? workspaceId, NotificationType type, string title, string message, string? referenceId = null, string? referenceType = null)
        {
            UserId = userId;
            WorkspaceId = workspaceId;
            Type = type;
            Title = title;
            Message = message;
            ReferenceId = referenceId;
            ReferenceType = referenceType;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAtUtc = DateTime.UtcNow;
            }
        }
    }
}
