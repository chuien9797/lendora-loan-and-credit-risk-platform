using Lendora.Infrastructure.Identity;

namespace Lendora.Infrastructure.Authentication;

internal interface ITokenService
{
    Task<TokenResponse> CreateTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
