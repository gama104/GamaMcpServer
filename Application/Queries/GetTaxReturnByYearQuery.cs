using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get specific tax return by year for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetTaxReturnByYearQuery(string UserId, int Year) : IRequest<TaxReturn?>;
