using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Interfaces;

/// <summary>
/// Application database context interface following 2025 best practices
/// Provides abstraction for Entity Framework Core operations
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Taxpayer> Taxpayers { get; }
    DbSet<TaxReturn> TaxReturns { get; }
    DbSet<Deduction> Deductions { get; }
    DbSet<Document> Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
