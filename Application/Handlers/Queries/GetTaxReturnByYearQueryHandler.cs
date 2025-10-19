using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetTaxReturnByYearQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetTaxReturnByYearQueryHandler : IRequestHandler<GetTaxReturnByYearQuery, TaxReturn?>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetTaxReturnByYearQueryHandler> _logger;

    public GetTaxReturnByYearQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetTaxReturnByYearQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaxReturn?> Handle(GetTaxReturnByYearQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tax return for user: {UserId}, year: {Year}", request.UserId, request.Year);

        try
        {
            var taxReturn = await _context.TaxReturns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == request.UserId && r.TaxYear == request.Year, cancellationToken);

            if (taxReturn == null)
            {
                _logger.LogWarning("No tax return found for user: {UserId}, year: {Year}", request.UserId, request.Year);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved tax return for user: {UserId}, year: {Year}", request.UserId, request.Year);
            }

            return taxReturn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax return for user: {UserId}, year: {Year}", request.UserId, request.Year);
            throw;
        }
    }
}
