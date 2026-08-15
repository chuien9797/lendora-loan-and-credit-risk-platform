using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class LoanProductConfiguration : IEntityTypeConfiguration<LoanProduct>
{
    public void Configure(EntityTypeBuilder<LoanProduct> builder)
    {
        builder.ToTable("LoanProducts");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(product => product.Code)
            .IsUnique();

        builder.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(product => product.MinAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.MaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(product => product.InterestRate)
            .HasColumnType("decimal(9,4)");
    }
}
