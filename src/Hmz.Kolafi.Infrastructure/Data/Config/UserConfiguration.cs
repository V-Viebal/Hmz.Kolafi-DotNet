using Hmz.Kolafi.Core.UserAggregate;

namespace Hmz.Kolafi.Infrastructure.Data.Config;

/// <summary>
/// Configuration for the User aggregate root.
/// Inherits cross-cutting concern configuration (auditing, soft-delete, row version) from BaseEntityConfiguration.
/// </summary>
public class UserConfiguration : BaseEntityConfiguration<User>
{
  protected override void ConfigureEntitySpecific(EntityTypeBuilder<User> builder)
  {
    // Primary key and value object conversion
    builder.HasKey(x => x.Id);

    builder.Property(x => x.Id)
      .HasVogenConversion()
      .UseIdentityByDefaultColumn()
      .IsRequired();

    // Unique external identity
    builder.Property(x => x.SubjectId)
      .HasMaxLength(128)
      .IsRequired();

    builder.HasIndex(x => x.SubjectId)
      .IsUnique();

    // Email
    builder.Property(x => x.Email)
      .HasMaxLength(256)
      .IsRequired();

    builder.HasIndex(x => x.Email)
      .IsUnique();

    // Name
    builder.Property(x => x.Name)
      .HasMaxLength(256)
      .IsRequired();

    // Picture URL
    builder.Property(x => x.Picture)
      .HasMaxLength(2048);

    // Roles stored as JSONB with GIN index for efficient querying
    builder.Property(x => x.Roles)
      .HasColumnType("jsonb")
      .IsRequired();

    builder.HasIndex(x => x.Roles)
      .HasMethod("gin");

    // Last sign-in tracking
    builder.Property(x => x.LastSignedInAt);
  }
}
