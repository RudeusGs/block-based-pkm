using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Features.Authentication.Commands.Login;
using Pkm.Application.Features.Authentication.Commands.Register;
using Pkm.Application.Features.Authentication.Queries.GetUserRoles;
using Pkm.Application.Features.Documents.Commands.AcquireBlockLease;
using Pkm.Application.Features.Documents.Commands.CreateBlock;
using Pkm.Application.Features.Documents.Commands.DeleteBlock;
using Pkm.Application.Features.Documents.Commands.MoveBlock;
using Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;
using Pkm.Application.Features.Documents.Commands.RenewBlockLease;
using Pkm.Application.Features.Documents.Commands.UpdateBlock;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Documents.Queries.GetBlock;
using Pkm.Application.Features.Documents.Queries.GetBlockLease;
using Pkm.Application.Features.Documents.Queries.GetPagePresence;
using Pkm.Application.Features.Documents.Queries.ListPageBlocks;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Pages.Commands.CreatePage;
using Pkm.Application.Features.Pages.Commands.DeletePage;
using Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Pages.Queries.GetPage;
using Pkm.Application.Features.Pages.Queries.ListSubPages;
using Pkm.Application.Features.Pages.Queries.ListWorkspacePages;
using Pkm.Application.Features.Pages.Queries.SearchPages;
using Pkm.Application.Features.Tasks.Commands.AssignTask;
using Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;
using Pkm.Application.Features.Tasks.Commands.CreateTaskComment;
using Pkm.Application.Features.Tasks.Commands.CreateWorkTask;
using Pkm.Application.Features.Tasks.Commands.DeleteTaskComment;
using Pkm.Application.Features.Tasks.Commands.DeleteWorkTask;
using Pkm.Application.Features.Tasks.Commands.RestoreTaskComment;
using Pkm.Application.Features.Tasks.Commands.UnassignTask;
using Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;
using Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Application.Features.Tasks.Queries.GetWorkTaskById;
using Pkm.Application.Features.Tasks.Queries.ListPageTasks;
using Pkm.Application.Features.Tasks.Queries.ListTaskComments;
using Pkm.Application.Features.Tasks.Queries.ListWorkspaceTasks;
using Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;
using Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;
using Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;
using Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;
using Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Application.Features.Workspaces.Queries.GetWorkspaceById;
using Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;
using Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;
using Pkm.Application.Features.Notifications.Commands.DeleteNotification;
using Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsUnread;
using Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using Pkm.Application.Features.Notifications.Queries.ListNotifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Recommendations.Commands.AcceptTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.CompleteTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;
using Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.UpdateUserTaskPreference;
using Pkm.Application.Features.Recommendations.Queries.GetUserTaskPreference;
using Pkm.Application.Features.Recommendations.Queries.ListTaskRecommendations;
using Pkm.Application.Features.Recommendations.Services;
namespace Pkm.Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWorkspaceAccessEvaluator, WorkspaceAccessEvaluator>();
        services.AddScoped<IPageAccessEvaluator, PageAccessEvaluator>();
        services.AddScoped<IDocumentAccessEvaluator, DocumentAccessEvaluator>();
        services.AddScoped<ITaskAccessEvaluator, TaskAccessEvaluator>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRecommendationScoringService, RecommendationScoringService>();

        services.AddScoped<IBlockPayloadValidator, BlockPayloadValidator>();

        services.AddScoped<LoginCommandValidator>();
        services.AddScoped<RegisterCommandValidator>();
        services.AddScoped<CreateWorkspaceCommandValidator>();
        services.AddScoped<UpdateWorkspaceCommandValidator>();
        services.AddScoped<AddWorkspaceMemberCommandValidator>();
        services.AddScoped<CreateWorkTaskCommandValidator>();
        services.AddScoped<UpdateWorkTaskCommandValidator>();
        services.AddScoped<AssignTaskCommandValidator>();
        services.AddScoped<UnassignTaskCommandValidator>();
        services.AddScoped<DeleteWorkTaskCommandValidator>();
        services.AddScoped<ChangeWorkTaskStatusCommandValidator>();
        services.AddScoped<ListPageTasksQueryValidator>();
        services.AddScoped<ListWorkspaceTasksQueryValidator>();
        services.AddScoped<ListNotificationsQueryValidator>();

        services.AddScoped<CreateTaskCommentCommandValidator>();
        services.AddScoped<UpdateTaskCommentCommandValidator>();
        services.AddScoped<DeleteTaskCommentCommandValidator>();
        services.AddScoped<RestoreTaskCommentCommandValidator>();
        services.AddScoped<ListTaskCommentsQueryValidator>();

        services.AddScoped<GenerateTaskRecommendationsCommandValidator>();
        services.AddScoped<ListTaskRecommendationsQueryValidator>();
        services.AddScoped<UpdateUserTaskPreferenceCommandValidator>();

        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<GetUserRolesHandler>();

        services.AddScoped<CreateWorkspaceHandler>();
        services.AddScoped<UpdateWorkspaceHandler>();
        services.AddScoped<DeleteWorkspaceHandler>();
        services.AddScoped<AddWorkspaceMemberHandler>();
        services.AddScoped<ChangeWorkspaceMemberRoleHandler>();
        services.AddScoped<RemoveWorkspaceMemberHandler>();

        services.AddScoped<GetWorkspaceByIdHandler>();
        services.AddScoped<ListMyWorkspacesHandler>();
        services.AddScoped<ListWorkspaceMembersHandler>();

        services.AddScoped<CreatePageHandler>();
        services.AddScoped<UpdatePageMetadataHandler>();
        services.AddScoped<DeletePageHandler>();

        services.AddScoped<GetPageHandler>();
        services.AddScoped<ListWorkspacePagesHandler>();
        services.AddScoped<ListSubPagesHandler>();
        services.AddScoped<SearchPagesHandler>();

        services.AddScoped<GetBlockHandler>();
        services.AddScoped<GetBlockLeaseHandler>();
        services.AddScoped<GetPagePresenceHandler>();
        services.AddScoped<ListPageBlocksHandler>();

        services.AddScoped<CreateBlockHandler>();
        services.AddScoped<UpdateBlockHandler>();
        services.AddScoped<MoveBlockHandler>();
        services.AddScoped<DeleteBlockHandler>();

        services.AddScoped<AcquireBlockLeaseHandler>();
        services.AddScoped<RenewBlockLeaseHandler>();
        services.AddScoped<ReleaseBlockLeaseHandler>();

        services.AddScoped<CreateWorkTaskHandler>();
        services.AddScoped<UpdateWorkTaskHandler>();
        services.AddScoped<DeleteWorkTaskHandler>();
        services.AddScoped<AssignTaskHandler>();
        services.AddScoped<UnassignTaskHandler>();
        services.AddScoped<ChangeWorkTaskStatusHandler>();

        services.AddScoped<GetWorkTaskByIdHandler>();
        services.AddScoped<ListPageTasksHandler>();
        services.AddScoped<ListWorkspaceTasksHandler>();

        services.AddScoped<ListTaskCommentsHandler>();
        services.AddScoped<CreateTaskCommentHandler>();
        services.AddScoped<UpdateTaskCommentHandler>();
        services.AddScoped<DeleteTaskCommentHandler>();
        services.AddScoped<RestoreTaskCommentHandler>();

        services.AddScoped<ListNotificationsHandler>();
        services.AddScoped<GetUnreadNotificationCountHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();
        services.AddScoped<MarkNotificationAsUnreadHandler>();
        services.AddScoped<MarkAllNotificationsAsReadHandler>();
        services.AddScoped<DeleteNotificationHandler>();

        services.AddScoped<GenerateTaskRecommendationsHandler>();
        services.AddScoped<ListTaskRecommendationsHandler>();
        services.AddScoped<AcceptTaskRecommendationHandler>();
        services.AddScoped<RejectTaskRecommendationHandler>();
        services.AddScoped<CompleteTaskRecommendationHandler>();
        services.AddScoped<GetUserTaskPreferenceHandler>();
        services.AddScoped<UpdateUserTaskPreferenceHandler>();

        services.AddSingleton<IOrderKeyGenerator, LexicographicOrderKeyGenerator>();

        return services;
    }
}