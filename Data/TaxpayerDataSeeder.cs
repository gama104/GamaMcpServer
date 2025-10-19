using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Data;

/// <summary>
/// Data seeding service following 2025 best practices
/// Seeds in-memory database from existing JSON data file
/// </summary>
public static class TaxpayerDataSeeder
{
    /// <summary>
    /// Seeds the database with data from the JSON file
    /// </summary>
    public static async Task SeedFromJsonAsync(IApplicationDbContext context, string jsonFilePath)
    {
        // Check if data already exists
        if (await context.Taxpayers.AnyAsync())
        {
            return; // Already seeded
        }

        try
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"JSON data file not found: {jsonFilePath}");
            }

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var dataContainer = JsonSerializer.Deserialize<DataContainer>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dataContainer == null)
            {
                throw new InvalidOperationException("Failed to deserialize JSON data");
            }

            // Add entities to context
            if (dataContainer.Taxpayers?.Any() == true)
            {
                context.Taxpayers.AddRange(dataContainer.Taxpayers);
            }

            if (dataContainer.TaxReturns?.Any() == true)
            {
                context.TaxReturns.AddRange(dataContainer.TaxReturns);
            }

            if (dataContainer.Deductions?.Any() == true)
            {
                context.Deductions.AddRange(dataContainer.Deductions);
            }

            if (dataContainer.Documents?.Any() == true)
            {
                context.Documents.AddRange(dataContainer.Documents);
            }

            // Save changes
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error seeding database from JSON file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates sample data if JSON file doesn't exist
    /// </summary>
    public static async Task CreateSampleDataAsync(IApplicationDbContext context)
    {
        // Check if data already exists
        if (await context.Taxpayers.AnyAsync())
        {
            return; // Already seeded
        }

        var sampleData = CreateSampleDataContainer();

        // Add entities to context
        context.Taxpayers.AddRange(sampleData.Taxpayers);
        context.TaxReturns.AddRange(sampleData.TaxReturns);
        context.Deductions.AddRange(sampleData.Deductions);
        context.Documents.AddRange(sampleData.Documents);

        // Save changes
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates sample data container with multiple users for testing data isolation
    /// </summary>
    private static DataContainer CreateSampleDataContainer()
    {
        // Sample data for User 1 (test-user)
        var user1Id = "test-user";
        var taxpayer1Id = Guid.NewGuid().ToString();

        // Sample data for User 2 (another-user) - to test data isolation
        var user2Id = "another-user";
        var taxpayer2Id = Guid.NewGuid().ToString();

        return new DataContainer
        {
            Taxpayers = new List<Taxpayer>
            {
                new Taxpayer
                {
                    TaxpayerId = taxpayer1Id,
                    UserId = user1Id,
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    SsnLast4 = "1234",
                    Address = "123 Main St, Anytown, CA 90210",
                    Phone = "(555) 123-4567",
                    FilingStatus = FilingStatus.MarriedFilingJointly,
                    CreatedDate = DateTime.UtcNow.AddYears(-3)
                },
                new Taxpayer
                {
                    TaxpayerId = taxpayer2Id,
                    UserId = user2Id,
                    Name = "Jane Smith",
                    Email = "jane.smith@example.com",
                    SsnLast4 = "5678",
                    Address = "456 Oak Ave, Other City, NY 10001",
                    Phone = "(555) 987-6543",
                    FilingStatus = FilingStatus.Single,
                    CreatedDate = DateTime.UtcNow.AddYears(-2)
                }
            },
            TaxReturns = CreateSampleTaxReturns(user1Id, taxpayer1Id, user2Id, taxpayer2Id),
            Deductions = CreateSampleDeductions(user1Id, taxpayer1Id, user2Id, taxpayer2Id),
            Documents = CreateSampleDocuments(user1Id, taxpayer1Id, user2Id, taxpayer2Id)
        };
    }

    private static List<TaxReturn> CreateSampleTaxReturns(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<TaxReturn>
        {
            // User 1 - 2023
            new TaxReturn
            {
                ReturnId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                FilingStatus = FilingStatus.MarriedFilingJointly,
                AdjustedGrossIncome = 125000,
                TaxableIncome = 95000,
                TotalTax = 15200,
                TotalDeductions = 30000,
                FilingDate = new DateTime(2024, 4, 10),
                Status = ReturnStatus.Filed
            },
            // User 1 - 2024
            new TaxReturn
            {
                ReturnId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2024,
                FilingStatus = FilingStatus.MarriedFilingJointly,
                AdjustedGrossIncome = 135000,
                TaxableIncome = 102000,
                TotalTax = 16800,
                TotalDeductions = 33000,
                Status = ReturnStatus.Draft
            },
            // User 2 - 2023
            new TaxReturn
            {
                ReturnId = Guid.NewGuid().ToString(),
                UserId = user2Id,
                TaxpayerId = taxpayer2Id,
                TaxYear = 2023,
                FilingStatus = FilingStatus.Single,
                AdjustedGrossIncome = 85000,
                TaxableIncome = 72000,
                TotalTax = 11500,
                TotalDeductions = 13000,
                FilingDate = new DateTime(2024, 3, 15),
                Status = ReturnStatus.Filed
            }
        };
    }

    private static List<Deduction> CreateSampleDeductions(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<Deduction>
        {
            // User 1 - 2023
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                Category = DeductionCategory.CharitableDonations,
                Description = "Annual charity donations",
                Amount = 5000,
                DateIncurred = new DateTime(2023, 12, 15)
            },
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                Category = DeductionCategory.MortgageInterest,
                Description = "Home mortgage interest",
                Amount = 18000,
                DateIncurred = new DateTime(2023, 12, 31)
            },
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                Category = DeductionCategory.PropertyTaxes,
                Description = "Property tax payment",
                Amount = 7000,
                DateIncurred = new DateTime(2023, 6, 30)
            },
            // User 1 - 2024
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2024,
                Category = DeductionCategory.CharitableDonations,
                Description = "Charitable contributions",
                Amount = 6500,
                DateIncurred = new DateTime(2024, 12, 10)
            },
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2024,
                Category = DeductionCategory.MedicalExpenses,
                Description = "Medical expenses",
                Amount = 8500,
                DateIncurred = new DateTime(2024, 8, 15)
            },
            // User 2 - 2023 (different user!)
            new Deduction
            {
                DeductionId = Guid.NewGuid().ToString(),
                UserId = user2Id,
                TaxpayerId = taxpayer2Id,
                TaxYear = 2023,
                Category = DeductionCategory.StateLocalTaxes,
                Description = "State and local taxes",
                Amount = 10000,
                DateIncurred = new DateTime(2023, 12, 31)
            }
        };
    }

    private static List<Document> CreateSampleDocuments(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<Document>
        {
            // User 1 documents
            new Document
            {
                DocumentId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                DocumentType = DocumentType.W2,
                FileName = "W2-2023.pdf",
                FilePath = "/documents/w2-2023.pdf",
                Category = "Income",
                FileSize = 524288,
                UploadDate = DateTime.UtcNow.AddDays(-30)
            },
            new Document
            {
                DocumentId = Guid.NewGuid().ToString(),
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                DocumentType = DocumentType.MortgageStatement,
                FileName = "Mortgage-2023.pdf",
                FilePath = "/documents/mortgage-2023.pdf",
                Category = "Housing",
                FileSize = 102400,
                UploadDate = DateTime.UtcNow.AddDays(-25)
            },
            // User 2 documents (different user!)
            new Document
            {
                DocumentId = Guid.NewGuid().ToString(),
                UserId = user2Id,
                TaxpayerId = taxpayer2Id,
                TaxYear = 2023,
                DocumentType = DocumentType.W2,
                FileName = "W2-Jane-2023.pdf",
                FilePath = "/documents/w2-jane-2023.pdf",
                Category = "Income",
                FileSize = 450000,
                UploadDate = DateTime.UtcNow.AddDays(-20)
            }
        };
    }
}

/// <summary>
/// Container for all data types (used for JSON deserialization)
/// </summary>
public class DataContainer
{
    public List<Taxpayer> Taxpayers { get; set; } = new();
    public List<TaxReturn> TaxReturns { get; set; } = new();
    public List<Deduction> Deductions { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
}
