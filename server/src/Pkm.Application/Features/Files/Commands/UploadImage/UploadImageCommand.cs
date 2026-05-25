using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Files.Models;

namespace Pkm.Application.Features.Files.Commands.UploadImage;

public sealed record UploadImageCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string? Purpose) : ICommand<StoredFileDto>;
