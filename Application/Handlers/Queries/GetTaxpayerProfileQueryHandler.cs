using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers.Queries;

/// <summary>
/// Handler for GetTaxpayerProfileQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetTaxpayerProfileQueryHandler : IRequestHandler<GetTaxpayerProfileQuery, Taxpayer?>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetTaxpayerProfileQueryHandler> _logger;

    public GetTaxpayerProfileQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetTaxpayerProfileQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Taxpayer?> Handle(GetTaxpayerProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting taxpayer profile for user: {UserId}", request.UserId);

        try
        {
            var taxpayer = await _context.Taxpayers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == request.UserId, cancellationToken);

            if (taxpayer == null)
            {
                _logger.LogWarning("No taxpayer profile found for user: {UserId}", request.UserId);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved taxpayer profile for user: {UserId}", request.UserId);
            }

            return taxpayer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taxpayer profile for user: {UserId}", request.UserId);
            throw;
        }
    }
}
