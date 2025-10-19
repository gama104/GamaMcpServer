using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get documents by type for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetDocumentsByTypeQuery(string UserId, DocumentType DocumentType) : IRequest<List<Document>>;
