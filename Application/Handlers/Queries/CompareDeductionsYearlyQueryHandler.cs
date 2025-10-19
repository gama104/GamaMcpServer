using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for CompareDeductionsYearlyQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class CompareDeductionsYearlyQueryHandler : IRequestHandler<CompareDeductionsYearlyQuery, Dictionary<int, List<Deduction>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompareDeductionsYearlyQueryHandler> _logger;

    public CompareDeductionsYearlyQueryHandler(
        IApplicationDbContext context, 
        ILogger<CompareDeductionsYearlyQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<int, List<Deduction>>> Handle(CompareDeductionsYearlyQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Comparing deductions for user: {UserId}, years: {Year1} vs {Year2}", 
            request.UserId, request.Year1, request.Year2);

        try
        {
            var deductions = await _context.Deductions
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && (d.TaxYear == request.Year1 || d.TaxYear == request.Year2))
                .ToListAsync(cancellationToken);

            var groupedDeductions = deductions
                .GroupBy(d => d.TaxYear)
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Category).ThenBy(d => d.Description).ToList());

            _logger.LogInformation("Retrieved deductions comparison for user: {UserId}, years: {Year1} vs {Year2}, " +
                "Year1 count: {Year1Count}, Year2 count: {Year2Count}", 
                request.UserId, request.Year1, request.Year2,
                groupedDeductions.GetValueOrDefault(request.Year1, new List<Deduction>()).Count,
                groupedDeductions.GetValueOrDefault(request.Year2, new List<Deduction>()).Count);

            return groupedDeductions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing deductions for user: {UserId}, years: {Year1} vs {Year2}", 
                request.UserId, request.Year1, request.Year2);
            throw;
        }
    }
}
