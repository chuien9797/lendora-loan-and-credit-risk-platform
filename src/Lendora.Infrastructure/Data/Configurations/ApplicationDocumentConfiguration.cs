using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.ToTable("ApplicationDocuments");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(document => document.StoredFileName)
            .HasMaxLength(255);

        builder.Property(document => document.StoragePath)
            .HasMaxLength(500);

        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.ReviewNote)
            .HasMaxLength(1000);

        builder.Property(document => document.OcrProvider)
            .HasMaxLength(100);

        builder.Property(document => document.OcrConfidence)
            .HasPrecision(5, 2);

        builder.Property(document => document.OcrSuggestedMonthlyIncome)
            .HasPrecision(18, 2);

        builder.Property(document => document.OcrSuggestedMonthlyExpenses)
            .HasPrecision(18, 2);

        builder.Property(document => document.OcrSuggestedNationalIdNumber)
            .HasMaxLength(80);

        builder.Property(document => document.OcrSuggestedAddress)
            .HasMaxLength(1000);

        builder.Property(document => document.OcrVerificationStatus)
            .HasMaxLength(40);

        builder.Property(document => document.OcrVerificationFindings)
            .HasMaxLength(2000);

        builder.Property(document => document.OcrSummary)
            .HasMaxLength(1000);

        builder.Property(document => document.OcrExtractedText)
            .HasColumnType("text");

        builder.Property(document => document.OcrFailureReason)
            .HasMaxLength(1000);

        builder.HasOne(document => document.LoanApplication)
            .WithMany(application => application.Documents)
            .HasForeignKey(document => document.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.LoanApplicationId);
        builder.HasIndex(document => document.Status);
        builder.HasIndex(document => document.OcrStatus);
    }
}
