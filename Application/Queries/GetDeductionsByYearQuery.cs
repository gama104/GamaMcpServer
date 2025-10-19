using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get deductions by year for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetDeductionsByYearQuery(string UserId, int Year) : IRequest<List<Deduction>>;
