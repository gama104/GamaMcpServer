using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetDeductionsByCategoryQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetDeductionsByCategoryQueryHandler : IRequestHandler<GetDeductionsByCategoryQuery, List<Deduction>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDeductionsByCategoryQueryHandler> _logger;

    public GetDeductionsByCategoryQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetDeductionsByCategoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Deduction>> Handle(GetDeductionsByCategoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting deductions for user: {UserId}, category: {Category}", request.UserId, request.Category);

        try
        {
            var deductions = await _context.Deductions
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && d.Category == request.Category)
                .OrderByDescending(d => d.TaxYear)
                .ThenBy(d => d.Description)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} deductions for user: {UserId}, category: {Category}", 
                deductions.Count, request.UserId, request.Category);

            return deductions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deductions for user: {UserId}, category: {Category}", request.UserId, request.Category);
            throw;
        }
    }
}
