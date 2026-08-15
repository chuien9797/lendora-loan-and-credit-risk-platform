using Lendora.Application.Admin;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Admin;

public interface IAdminLoanProductManagementService
{
    Task<IReadOnlyCollection<LoanProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanProductDto>> CreateProductAsync(CreateLoanProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanProductDto>> UpdateProductAsync(Guid id, UpdateLoanProductRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
