using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Data;

/// <summary>
/// Application database context following 2025 Entity Framework Core best practices
/// Implements in-memory database for demo purposes with production-ready patterns
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Taxpayer> Taxpayers { get; set; }
    public DbSet<TaxReturn> TaxReturns { get; set; }
    public DbSet<Deduction> Deductions { get; set; }
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Taxpayer entity
        modelBuilder.Entity<Taxpayer>(entity =>
        {
            entity.HasKey(t => t.TaxpayerId);
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.Email);
            
            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(t => t.SsnLast4)
                .IsRequired()
                .HasMaxLength(4);
                
            entity.Property(t => t.Address)
                .HasMaxLength(500);
                
            entity.Property(t => t.Phone)
                .HasMaxLength(20);
        });

        // Configure TaxReturn entity
        modelBuilder.Entity<TaxReturn>(entity =>
        {
            entity.HasKey(t => t.ReturnId);
            entity.HasIndex(t => new { t.UserId, t.TaxYear });
            entity.HasIndex(t => t.TaxpayerId);
            
            entity.Property(t => t.AdjustedGrossIncome)
                .HasPrecision(18, 2);
                
            entity.Property(t => t.TaxableIncome)
                .HasPrecision(18, 2);
                
            entity.Property(t => t.TotalTax)
                .HasPrecision(18, 2);
                
            entity.Property(t => t.TotalDeductions)
                .HasPrecision(18, 2);

            // Configure relationship
            entity.HasOne<Taxpayer>()
                .WithMany()
                .HasForeignKey(t => t.TaxpayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Deduction entity
        modelBuilder.Entity<Deduction>(entity =>
        {
            entity.HasKey(d => d.DeductionId);
            entity.HasIndex(d => new { d.UserId, d.TaxYear, d.Category });
            entity.HasIndex(d => d.TaxpayerId);
            
            entity.Property(d => d.Description)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(d => d.Amount)
                .HasPrecision(18, 2);

            // Configure relationship
            entity.HasOne<Taxpayer>()
                .WithMany()
                .HasForeignKey(d => d.TaxpayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.DocumentId);
            entity.HasIndex(d => new { d.UserId, d.TaxYear, d.DocumentType });
            entity.HasIndex(d => d.TaxpayerId);
            
            entity.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(d => d.FilePath)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(d => d.Category)
                .HasMaxLength(100);

            // Configure relationship
            entity.HasOne<Taxpayer>()
                .WithMany()
                .HasForeignKey(d => d.TaxpayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add audit fields if needed
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // You can add audit logic here if needed
            // For example, setting CreatedDate, ModifiedDate, etc.
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
