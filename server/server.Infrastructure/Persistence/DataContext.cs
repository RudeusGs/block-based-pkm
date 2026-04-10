using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence
{
    public class DataContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        /// <summary>
        /// Không gian làm việc của người dùng/team.
        /// </summary>
        public DbSet<Workspace> Workspaces { get; set; }

        /// <summary>
        /// Thành viên của một không gian làm việc.
        /// </summary>
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

        /// <summary>
        /// Trang tài liệu trong một không gian làm việc.
        /// </summary>
        public DbSet<Page> Pages { get; set; }

        /// <summary>
        /// Công việc cần làm trong workspace hoặc page.
        /// </summary>
        public DbSet<WorkTask> Tasks { get; set; }

        /// <summary>
        /// Người được giao thực hiện một công việc.
        /// </summary>
        public DbSet<TaskAssignee> TaskAssignees { get; set; }

        /// <summary>
        /// Bình luận trên một công việc.
        /// </summary>
        public DbSet<TaskComment> TaskComments { get; set; }

        /// <summary>
        /// Nhật ký hoạt động trong hệ thống.
        /// </summary>
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        /// <summary>
        /// Phiên làm việc thời gian thực của người dùng.
        /// </summary>
        public DbSet<RealtimeSession> RealtimeSessions { get; set; }

        // ========== Entities hỗ trợ thuật toán gợi ý task ==========

        /// <summary>
        /// Lịch sử hoàn thành task của user.
        /// Dùng để tracking: bao lâu user hoàn thành task, bao giờ hoàn thành.
        /// </summary>
        public DbSet<UserTaskHistory> UserTaskHistories { get; set; }

        /// <summary>
        /// Gợi ý task cho user tại một thời điểm cụ thể.
        /// </summary>
        public DbSet<TaskRecommendation> TaskRecommendations { get; set; }

        /// <summary>
        /// Tùy chỉnh cá nhân cho gợi ý task của user.
        /// </summary>
        public DbSet<UserTaskPreference> UserTaskPreferences { get; set; }

        /// <summary>
        /// Chỉ số hiệu suất của task (completion rate, avg duration, optimal time, ...).
        /// </summary>
        public DbSet<TaskPerformanceMetric> TaskPerformanceMetrics { get; set; }

        /// <summary>
        /// Thông báo cho người dùng.
        /// </summary>
        public DbSet<Notification> Notifications { get; set; }
    }
}

