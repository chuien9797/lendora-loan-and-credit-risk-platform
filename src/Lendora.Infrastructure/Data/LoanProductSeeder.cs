using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lendora.Infrastructure.Data;

public sealed class LoanProductSeeder(
    ApplicationDbContext dbContext,
    ILogger<LoanProductSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.LoanProducts.ToListAsync(cancellationToken);
        var productsByCode = products.ToDictionary(product => product.Code, StringComparer.OrdinalIgnoreCase);
        var defaults = new[]
        {
            new LoanProduct
            {
                Code = "PERSONAL_STANDARD",
                Name = "Personal Loan",
                ProductType = LoanProductType.PersonalLoan,
                MinAmount = 1000,
                MaxAmount = 25000,
                MinTermMonths = 12,
                MaxTermMonths = 60,
                InterestRate = 0.0799m,
                IsActive = true
            },
            new LoanProduct
            {
                Code = "CAR_STANDARD",
                Name = "Car Loan",
                ProductType = LoanProductType.CarLoan,
                MinAmount = 5000,
                MaxAmount = 50000,
                MinTermMonths = 12,
                MaxTermMonths = 72,
                InterestRate = 0.0550m,
                IsActive = true
            },
            new LoanProduct
            {
                Code = "MORTGAGE_HOME",
                Name = "Mortgage",
                ProductType = LoanProductType.Mortgage,
                MinAmount = 50000,
                MaxAmount = 750000,
                MinTermMonths = 120,
                MaxTermMonths = 360,
                InterestRate = 0.0425m,
                IsActive = true
            },
            new LoanProduct
            {
                Code = "BUSINESS_GROWTH",
                Name = "Business Loan",
                ProductType = LoanProductType.BusinessLoan,
                MinAmount = 10000,
                MaxAmount = 150000,
                MinTermMonths = 12,
                MaxTermMonths = 84,
                InterestRate = 0.0650m,
                IsActive = true
            }
        };

        foreach (var product in defaults)
        {
            if (!productsByCode.TryGetValue(product.Code, out var existingProduct))
            {
                dbContext.LoanProducts.Add(product);
                continue;
            }

            logger.LogDebug("Loan product {Code} already exists; preserving admin-managed configuration.", existingProduct.Code);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded missing default loan products.");
    }
}
