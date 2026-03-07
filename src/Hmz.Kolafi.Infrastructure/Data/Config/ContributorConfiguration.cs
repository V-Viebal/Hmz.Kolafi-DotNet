using Hmz.Kolafi.Core.ContributorAggregate;

namespace Hmz.Kolafi.Infrastructure.Data.Config;

public class ContributorConfiguration : BaseEntityConfiguration<Contributor>
{
  override protected void ConfigureEntitySpecific(EntityTypeBuilder<Contributor> builder)
  {
    // Primary key and value object conversion
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
      .HasVogenConversion()
      .UseIdentityByDefaultColumn()
      .IsRequired();

    builder.Property(entity => entity.Name)
      .HasVogenConversion()
      .HasMaxLength(ContributorName.MaxLength)
      .IsRequired();

    builder.OwnsOne(builder => builder.PhoneNumber);

    builder.Property(x => x.Status)
      .HasConversion(
          x => x.Value,
          x => ContributorStatus.FromValue(x));
  }
}
