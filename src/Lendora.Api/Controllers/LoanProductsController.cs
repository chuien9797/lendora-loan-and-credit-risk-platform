using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Loans;
using Lendora.Application.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/loan-products")]
public sealed class LoanProductsController(ILoanApplicationService loanApplicationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LoanProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoanProducts(CancellationToken cancellationToken)
    {
        var products = await loanApplicationService.GetLoanProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<LoanProductDto>>.Ok(products, "Loan products retrieved successfully.", HttpContext.TraceIdentifier));
    }
}
