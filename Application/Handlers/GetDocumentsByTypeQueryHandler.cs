using MediatR;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Handlers;

/// <summary>
/// Handler for GetDocumentsByTypeQuery following 2025 CQRS best practices
/// Implements efficient Entity Framework Core queries with proper error handling
/// </summary>
public class GetDocumentsByTypeQueryHandler : IRequestHandler<GetDocumentsByTypeQuery, List<Document>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDocumentsByTypeQueryHandler> _logger;

    public GetDocumentsByTypeQueryHandler(
        IApplicationDbContext context, 
        ILogger<GetDocumentsByTypeQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Document>> Handle(GetDocumentsByTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting documents for user: {UserId}, type: {DocumentType}", request.UserId, request.DocumentType);

        try
        {
            var documents = await _context.Documents
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && d.DocumentType == request.DocumentType)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} documents for user: {UserId}, type: {DocumentType}", 
                documents.Count, request.UserId, request.DocumentType);

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for user: {UserId}, type: {DocumentType}", request.UserId, request.DocumentType);
            throw;
        }
    }
}
