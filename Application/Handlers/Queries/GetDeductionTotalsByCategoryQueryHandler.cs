using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetDeductionTotalsByCategoryQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetDeductionTotalsByCategoryQueryHandler : IRequestHandler<GetDeductionTotalsByCategoryQuery, Dictionary<DeductionCategory, decimal>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDeductionTotalsByCategoryQueryHandler> _logger;

    public GetDeductionTotalsByCategoryQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetDeductionTotalsByCategoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<DeductionCategory, decimal>> Handle(GetDeductionTotalsByCategoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting deduction totals by category for user: {UserId}, year: {Year}", request.UserId, request.Year);

        try
        {
            var deductions = await _context.Deductions
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && d.TaxYear == request.Year)
                .ToListAsync(cancellationToken);

            var totals = deductions
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));

            _logger.LogInformation("Retrieved deduction totals for {Count} categories for user: {UserId}, year: {Year}", 
                totals.Count, request.UserId, request.Year);

            return totals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deduction totals for user: {UserId}, year: {Year}", request.UserId, request.Year);
            throw;
        }
    }
}
