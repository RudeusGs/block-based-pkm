namespace Pkm.Application.Features.Documents.Services;

public interface IOrderKeyGenerator
{
    string CreateFirst();

    string CreateLast(string? previous);

    string CreateBetween(string? previous, string? next);
}