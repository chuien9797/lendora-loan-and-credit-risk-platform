using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Admin;
using Lendora.Application.Admin;
using Lendora.Application.Loans;
using Lendora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/admin/loan-products")]
public sealed class AdminLoanProductsController(IAdminLoanProductManagementService productManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LoanProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await productManagementService.GetProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<LoanProductDto>>.Ok(products, "Loan products retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LoanProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct(CreateLoanProductRequest request, CancellationToken cancellationToken)
    {
        var result = await productManagementService.CreateProductAsync(request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan product creation failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanProductDto>.Ok(result.Data, "Loan product created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateLoanProductRequest request, CancellationToken cancellationToken)
    {
        var result = await productManagementService.UpdateProductAsync(id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan product update failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanProductDto>.Ok(result.Data, "Loan product updated successfully.", HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await productManagementService.DeleteProductAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(CreateErrorResponse("Loan product deletion failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Loan product deleted successfully.", HttpContext.TraceIdentifier));
    }

    private ApiErrorResponse CreateErrorResponse(string message, int statusCode, IReadOnlyCollection<string> errors) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            TraceId = HttpContext.TraceIdentifier,
            Errors = errors
        };
}
