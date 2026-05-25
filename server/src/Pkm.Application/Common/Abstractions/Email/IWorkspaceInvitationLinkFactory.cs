namespace Pkm.Application.Common.Abstractions.Email;

public interface IWorkspaceInvitationLinkFactory
{
    string CreateAcceptLink(string invitationToken);
}
