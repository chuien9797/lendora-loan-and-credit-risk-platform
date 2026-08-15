using Lendora.Application.Abstractions.Admin;
using Lendora.Application.Admin;
using Lendora.Application.Loans;
using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Admin;

internal sealed class AdminLoanProductManagementService(Data.ApplicationDbContext dbContext) : IAdminLoanProductManagementService
{
    public async Task<IReadOnlyCollection<LoanProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.LoanProducts
            .OrderBy(product => product.Code)
            .Select(product => MapToDto(product))
            .ToListAsync(cancellationToken);

    public async Task<ServiceResult<LoanProductDto>> CreateProductAsync(CreateLoanProductRequest request, CancellationToken cancellationToken = default)
    {
        var errors = ValidateProduct(request.Code, request.Name, request.MinAmount, request.MaxAmount, request.MinTermMonths, request.MaxTermMonths, request.InterestRate);
        if (errors.Count > 0)
        {
            return ServiceResult<LoanProductDto>.Failure(errors.ToArray());
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.LoanProducts.AnyAsync(product => product.Code == normalizedCode, cancellationToken);
        if (codeExists)
        {
            return ServiceResult<LoanProductDto>.Failure("A loan product with this code already exists.");
        }

        var product = new LoanProduct
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            ProductType = request.ProductType,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            MinTermMonths = request.MinTermMonths,
            MaxTermMonths = request.MaxTermMonths,
            InterestRate = request.InterestRate,
            IsActive = request.IsActive
        };

        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<LoanProductDto>> UpdateProductAsync(Guid id, UpdateLoanProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<LoanProductDto>.Failure("Loan product not found.");
        }

        var errors = ValidateProduct(request.Code, request.Name, request.MinAmount, request.MaxAmount, request.MinTermMonths, request.MaxTermMonths, request.InterestRate);
        if (errors.Count > 0)
        {
            return ServiceResult<LoanProductDto>.Failure(errors.ToArray());
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.LoanProducts
            .AnyAsync(candidate => candidate.Id != id && candidate.Code == normalizedCode, cancellationToken);
        if (codeExists)
        {
            return ServiceResult<LoanProductDto>.Failure("Another loan product already uses this code.");
        }

        product.Code = normalizedCode;
        product.Name = request.Name.Trim();
        product.ProductType = request.ProductType;
        product.MinAmount = request.MinAmount;
        product.MaxAmount = request.MaxAmount;
        product.MinTermMonths = request.MinTermMonths;
        product.MaxTermMonths = request.MaxTermMonths;
        product.InterestRate = request.InterestRate;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanProductDto>.Success(MapToDto(product));
    }

    public async Task<ServiceResult<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.Failure("Loan product not found.");
        }

        var hasApplications = await dbContext.LoanApplications
            .AnyAsync(application => application.LoanProductId == id, cancellationToken);
        if (hasApplications)
        {
            return ServiceResult<bool>.Failure("This loan product has application history. Disable it instead of deleting it.");
        }

        dbContext.LoanProducts.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static List<string> ValidateProduct(string code, string name, decimal minAmount, decimal maxAmount, int minTerm, int maxTerm, decimal interestRate)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add("Product code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Product name is required.");
        }

        if (minAmount <= 0 || maxAmount <= 0 || minAmount > maxAmount)
        {
            errors.Add("Product amount range is invalid.");
        }

        if (minTerm <= 0 || maxTerm <= 0 || minTerm > maxTerm)
        {
            errors.Add("Product term range is invalid.");
        }

        if (interestRate < 0 || interestRate > 1)
        {
            errors.Add("Interest rate must be between 0 and 1.");
        }

        return errors;
    }

    private static LoanProductDto MapToDto(LoanProduct product) =>
        new(
            product.Id,
            product.Code,
            product.Name,
            product.ProductType,
            product.MinAmount,
            product.MaxAmount,
            product.MinTermMonths,
            product.MaxTermMonths,
            product.InterestRate,
            product.IsActive);
}
