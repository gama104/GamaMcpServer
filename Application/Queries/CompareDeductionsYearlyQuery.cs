using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to compare deductions between two years for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record CompareDeductionsYearlyQuery(string UserId, int Year1, int Year2) : IRequest<Dictionary<int, List<Deduction>>>;
