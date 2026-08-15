using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class ApplicationAuditLogConfiguration : IEntityTypeConfiguration<ApplicationAuditLog>
{
    public void Configure(EntityTypeBuilder<ApplicationAuditLog> builder)
    {
        builder.Property(log => log.ActorRole)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(log => log.Summary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(log => log.Details)
            .HasMaxLength(2000);

        builder.HasIndex(log => new { log.LoanApplicationId, log.CreatedAtUtc });

        builder.HasOne(log => log.LoanApplication)
            .WithMany(application => application.AuditLogs)
            .HasForeignKey(log => log.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
