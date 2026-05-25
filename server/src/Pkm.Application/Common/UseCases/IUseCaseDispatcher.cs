using Pkm.Application.Common.Results;

namespace Pkm.Application.Common.UseCases;

public interface IUseCaseDispatcher
{
    Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<Result<TResponse>> ExecuteAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;

    Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}
