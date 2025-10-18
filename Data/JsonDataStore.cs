using System.Text.Json;
using ProtectedMcpServer.Models;
using ProtectedMcpServer.Services;

namespace ProtectedMcpServer.Data;

/// <summary>
/// JSON file-based data store with user-scoped access
/// SECURITY: ALL queries are filtered by the authenticated user's ID
/// </summary>
public class JsonDataStore : IDataStore
{
    private readonly IUserContext _userContext;
    private readonly string _dataFilePath;
    private readonly ILogger<JsonDataStore> _logger;
    private DataContainer? _cachedData;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public JsonDataStore(IUserContext userContext, ILogger<JsonDataStore> logger, IWebHostEnvironment env)
    {
        _userContext = userContext;
        _logger = logger;
        _dataFilePath = Path.Combine(env.ContentRootPath, "Data", "taxpayer-data.json");
        
        // Ensure data directory and file exist
        EnsureDataFileExists();
    }

    /// <summary>
    /// SECURITY: Loads data and filters by current user
    /// </summary>
    private async Task<DataContainer> LoadDataAsync()
    {
        // Use cache if valid
        if (_cachedData != null && DateTime.UtcNow - _lastLoadTime < _cacheExpiration)
        {
            return _cachedData;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath);
            _cachedData = JsonSerializer.Deserialize<DataContainer>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DataContainer();
            
            _lastLoadTime = DateTime.UtcNow;
            return _cachedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data file");
            return new DataContainer();
        }
    }

    /// <summary>
    /// SECURITY: Get current user's taxpayer profile
    /// </summary>
    public async Task<Taxpayer?> GetTaxpayerProfileAsync()
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID
        return data.Taxpayers.FirstOrDefault(t => t.UserId == _userContext.UserId);
    }

    /// <summary>
    /// SECURITY: Get all taxpayers for current user (usually just one)
    /// </summary>
    public async Task<List<Taxpayer>> GetAllTaxpayersAsync()
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID
        return data.Taxpayers.Where(t => t.UserId == _userContext.UserId).ToList();
    }

    /// <summary>
    /// SECURITY: Get tax returns for current user only
    /// </summary>
    public async Task<List<TaxReturn>> GetTaxReturnsAsync()
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID
        return data.TaxReturns
            .Where(r => r.UserId == _userContext.UserId)
            .OrderByDescending(r => r.TaxYear)
            .ToList();
    }

    /// <summary>
    /// SECURITY: Get specific year tax return for current user
    /// </summary>
    public async Task<TaxReturn?> GetTaxReturnByYearAsync(int year)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND year
        return data.TaxReturns.FirstOrDefault(r => 
            r.UserId == _userContext.UserId && r.TaxYear == year);
    }

    /// <summary>
    /// SECURITY: Get deductions for specific year for current user only
    /// </summary>
    public async Task<List<Deduction>> GetDeductionsByYearAsync(int year)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND year
        return data.Deductions
            .Where(d => d.UserId == _userContext.UserId && d.TaxYear == year)
            .OrderBy(d => d.Category)
            .ToList();
    }

    /// <summary>
    /// SECURITY: Get deductions by category for current user only
    /// </summary>
    public async Task<List<Deduction>> GetDeductionsByCategoryAsync(DeductionCategory category)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND category
        return data.Deductions
            .Where(d => d.UserId == _userContext.UserId && d.Category == category)
            .OrderByDescending(d => d.TaxYear)
            .ToList();
    }

    /// <summary>
    /// SECURITY: Calculate deduction totals by category for current user
    /// </summary>
    public async Task<Dictionary<DeductionCategory, decimal>> GetDeductionTotalsByCategoryAsync(int year)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND year
        return data.Deductions
            .Where(d => d.UserId == _userContext.UserId && d.TaxYear == year)
            .GroupBy(d => d.Category)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));
    }

    /// <summary>
    /// SECURITY: Calculate deduction totals by year for current user
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetDeductionTotalsByYearAsync()
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID
        return data.Deductions
            .Where(d => d.UserId == _userContext.UserId)
            .GroupBy(d => d.TaxYear)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));
    }

    /// <summary>
    /// SECURITY: Get documents by type for current user only
    /// </summary>
    public async Task<List<Document>> GetDocumentsByTypeAsync(DocumentType documentType)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND type
        return data.Documents
            .Where(d => d.UserId == _userContext.UserId && d.DocumentType == documentType)
            .OrderByDescending(d => d.UploadDate)
            .ToList();
    }

    /// <summary>
    /// SECURITY: Get documents by year for current user only
    /// </summary>
    public async Task<List<Document>> GetDocumentsByYearAsync(int year)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND year
        return data.Documents
            .Where(d => d.UserId == _userContext.UserId && d.TaxYear == year)
            .OrderBy(d => d.DocumentType)
            .ToList();
    }

    /// <summary>
    /// SECURITY: Compare deductions between two years for current user
    /// </summary>
    public async Task<Dictionary<int, List<Deduction>>> CompareDeductionsYearlyAsync(int year1, int year2)
    {
        var data = await LoadDataAsync();
        
        // SECURITY: Filter by authenticated user's ID AND specified years
        var deductions = data.Deductions
            .Where(d => d.UserId == _userContext.UserId && (d.TaxYear == year1 || d.TaxYear == year2))
            .GroupBy(d => d.TaxYear)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Category).ToList());
        
        return deductions;
    }

    /// <summary>
    /// Ensures the data file exists with sample data
    /// </summary>
    private void EnsureDataFileExists()
    {
        var directory = Path.GetDirectoryName(_dataFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        if (!File.Exists(_dataFilePath))
        {
            var sampleData = CreateSampleData();
            var json = JsonSerializer.Serialize(sampleData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_dataFilePath, json);
            _logger.LogInformation("Created sample data file at {Path}", _dataFilePath);
        }
    }

    /// <summary>
    /// Creates sample data with multiple users for testing data isolation
    /// </summary>
    private DataContainer CreateSampleData()
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

    private List<TaxReturn> CreateSampleTaxReturns(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<TaxReturn>
        {
            // User 1 - 2023
            new TaxReturn
            {
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

    private List<Deduction> CreateSampleDeductions(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<Deduction>
        {
            // User 1 - 2023
            new Deduction
            {
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

    private List<Document> CreateSampleDocuments(string user1Id, string taxpayer1Id, string user2Id, string taxpayer2Id)
    {
        return new List<Document>
        {
            // User 1 documents
            new Document
            {
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                DocumentType = DocumentType.W2,
                FileName = "W2-2023.pdf",
                FilePath = "/documents/w2-2023.pdf",
                Category = "Income",
                FileSize = 524288
            },
            new Document
            {
                UserId = user1Id,
                TaxpayerId = taxpayer1Id,
                TaxYear = 2023,
                DocumentType = DocumentType.MortgageStatement,
                FileName = "Mortgage-2023.pdf",
                FilePath = "/documents/mortgage-2023.pdf",
                Category = "Housing",
                FileSize = 102400
            },
            // User 2 documents (different user!)
            new Document
            {
                UserId = user2Id,
                TaxpayerId = taxpayer2Id,
                TaxYear = 2023,
                DocumentType = DocumentType.W2,
                FileName = "W2-Jane-2023.pdf",
                FilePath = "/documents/w2-jane-2023.pdf",
                Category = "Income",
                FileSize = 450000
            }
        };
    }
}

/// <summary>
/// Container for all data types
/// </summary>
public class DataContainer
{
    public List<Taxpayer> Taxpayers { get; set; } = new();
    public List<TaxReturn> TaxReturns { get; set; } = new();
    public List<Deduction> Deductions { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
}

