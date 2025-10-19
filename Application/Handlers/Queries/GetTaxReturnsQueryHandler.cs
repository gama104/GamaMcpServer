using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetTaxReturnsQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetTaxReturnsQueryHandler : IRequestHandler<GetTaxReturnsQuery, List<TaxReturn>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetTaxReturnsQueryHandler> _logger;

    public GetTaxReturnsQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetTaxReturnsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TaxReturn>> Handle(GetTaxReturnsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tax returns for user: {UserId}", request.UserId);

        try
        {
            var taxReturns = await _context.TaxReturns
                .AsNoTracking()
                .Where(r => r.UserId == request.UserId)
                .OrderByDescending(r => r.TaxYear)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} tax returns for user: {UserId}", taxReturns.Count, request.UserId);

            return taxReturns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax returns for user: {UserId}", request.UserId);
            throw;
        }
    }
}
