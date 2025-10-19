using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers;

/// <summary>
/// Handler for GetDocumentsByYearQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetDocumentsByYearQueryHandler : IRequestHandler<GetDocumentsByYearQuery, List<Document>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDocumentsByYearQueryHandler> _logger;

    public GetDocumentsByYearQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetDocumentsByYearQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Document>> Handle(GetDocumentsByYearQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting documents for user: {UserId}, year: {Year}", request.UserId, request.Year);

        try
        {
            var documents = await _context.Documents
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && d.TaxYear == request.Year)
                .OrderBy(d => d.DocumentType)
                .ThenByDescending(d => d.UploadDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} documents for user: {UserId}, year: {Year}", 
                documents.Count, request.UserId, request.Year);

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for user: {UserId}, year: {Year}", request.UserId, request.Year);
            throw;
        }
    }
}
