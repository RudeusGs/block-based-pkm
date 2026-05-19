using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Email;

namespace Pkm.Infrastructure.Email;

public static class EmailServiceCollection
{
    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.SectionName));

        services.Configure<ApplicationUrlOptions>(
            configuration.GetSection(ApplicationUrlOptions.SectionName));

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IWorkspaceInvitationLinkFactory, WorkspaceInvitationLinkFactory>();

        return services;
    }
}
