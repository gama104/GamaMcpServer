using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get deductions by category for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetDeductionsByCategoryQuery(string UserId, DeductionCategory Category) : IRequest<List<Deduction>>;
