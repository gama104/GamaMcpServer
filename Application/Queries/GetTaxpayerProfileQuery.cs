using MediatR;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Queries;

/// <summary>
/// Query to get taxpayer profile for authenticated user
/// Following CQRS pattern with MediatR
/// </summary>
public record GetTaxpayerProfileQuery(string UserId) : IRequest<Taxpayer?>;
