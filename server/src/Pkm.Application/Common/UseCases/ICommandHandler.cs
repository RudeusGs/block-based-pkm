using Pkm.Application.Common.Results;

namespace Pkm.Application.Common.UseCases;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}
