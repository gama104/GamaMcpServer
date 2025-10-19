using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get deduction totals by category for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetDeductionTotalsByCategoryQuery(string UserId, int Year) : IRequest<Dictionary<DeductionCategory, decimal>>;
