using Pkm.Application.Common.Results;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Documents.Services;

public static class DocumentRevisionGuard
{
    public static Error? ValidateExpectedRevision(Page page, long expectedRevision)
    {
        if (expectedRevision < 0)
            return DocumentErrors.InvalidExpectedRevision;

        if (page.CurrentRevision != expectedRevision)
            return DocumentErrors.RevisionConflict;

        return null;
    }
}