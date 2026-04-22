using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class UserTaskPreferenceConfiguration : IEntityTypeConfiguration<UserTaskPreference>
{
    public void Configure(EntityTypeBuilder<UserTaskPreference> builder)
    {
        builder.ToTable("UserTaskPreferences");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.MinPriorityForRecommendation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        var converter = new ValueConverter<List<int>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<int>>(v ?? "[]", (JsonSerializerOptions?)null) ?? new List<int>());

        var comparer = new ValueComparer<List<int>>(
            (left, right) => (left ?? new List<int>()).SequenceEqual(right ?? new List<int>()),
            value => (value ?? new List<int>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => (value ?? new List<int>()).ToList());

        var property = builder.Property<List<int>>("_preferredDaysOfWeek")
            .HasColumnName("PreferredDaysOfWeek")
            .HasConversion(converter)
            .Metadata;

        property.SetValueComparer(comparer);
        property.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}