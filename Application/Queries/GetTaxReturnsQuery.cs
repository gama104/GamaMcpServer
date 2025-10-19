using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get all tax returns for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetTaxReturnsQuery(string UserId) : IRequest<List<TaxReturn>>;
