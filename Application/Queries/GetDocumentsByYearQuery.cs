using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get documents by year for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetDocumentsByYearQuery(string UserId, int Year) : IRequest<List<Document>>;
