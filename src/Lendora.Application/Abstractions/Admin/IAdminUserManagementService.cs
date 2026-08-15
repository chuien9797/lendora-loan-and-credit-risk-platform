using Lendora.Application.Admin;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Admin;

public interface IAdminUserManagementService
{
    Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<AdminUserDto>> CreateUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AdminUserDto>> UpdateUserAsync(Guid id, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteUserAsync(Guid id, Guid currentAdminId, CancellationToken cancellationToken = default);
}
