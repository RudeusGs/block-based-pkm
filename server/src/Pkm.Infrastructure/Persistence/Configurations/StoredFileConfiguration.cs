using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Files;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StorageProvider)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxProviderLength);

        builder.Property(x => x.StorageFileId)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxStorageFileIdLength);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxFileNameLength);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxOriginalFileNameLength);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxContentTypeLength);

        builder.Property(x => x.PublicUrl)
            .IsRequired()
            .HasMaxLength(StoredFile.MaxUrlLength);

        builder.Property(x => x.WebViewLink)
            .HasMaxLength(StoredFile.MaxUrlLength);

        builder.Property(x => x.Purpose)
            .HasMaxLength(StoredFile.MaxPurposeLength);

        builder.HasIndex(x => x.UploadedByUserId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.StorageFileId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
