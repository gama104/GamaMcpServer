using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetDeductionsByYearQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetDeductionsByYearQueryHandler : IRequestHandler<GetDeductionsByYearQuery, List<Deduction>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDeductionsByYearQueryHandler> _logger;

    public GetDeductionsByYearQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetDeductionsByYearQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Deduction>> Handle(GetDeductionsByYearQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting deductions for user: {UserId}, year: {Year}", request.UserId, request.Year);

        try
        {
            var deductions = await _context.Deductions
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && d.TaxYear == request.Year)
                .OrderBy(d => d.Category)
                .ThenBy(d => d.Description)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} deductions for user: {UserId}, year: {Year}", 
                deductions.Count, request.UserId, request.Year);

            return deductions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deductions for user: {UserId}, year: {Year}", request.UserId, request.Year);
            throw;
        }
    }
}
