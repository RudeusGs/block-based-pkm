using Pkm.Application.Common.Results;

namespace Pkm.Application.Common.UseCases;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default);
}
