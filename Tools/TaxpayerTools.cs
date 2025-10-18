using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using ProtectedMcpServer.Data;
using ProtectedMcpServer.Models;
using ProtectedMcpServer.Services;

namespace ProtectedMcpServer.Tools;

/// <summary>
/// MCP tools for taxpayer data access
/// SECURITY: All tools enforce user-scoped data access via UserContext
/// </summary>
[McpServerToolType]
public sealed class TaxpayerTools
{
    private readonly IDataStore _dataStore;
    private readonly IUserContext _userContext;
    private readonly ILogger<TaxpayerTools> _logger;

    public TaxpayerTools(IDataStore dataStore, IUserContext userContext, ILogger<TaxpayerTools> logger)
    {
        _dataStore = dataStore;
        _userContext = userContext;
        _logger = logger;
    }

    [McpServerTool, Description("Get taxpayer profile information")]
    public async Task<string> GetTaxpayerProfile()
    {
        // SECURITY: UserContext ensures we only get current user's data
        _logger.LogInformation("GetTaxpayerProfile called by user: {UserId}", _userContext.UserId);
        
        var taxpayer = await _dataStore.GetTaxpayerProfileAsync();
        
        if (taxpayer == null)
        {
            return "No taxpayer profile found for the current user.";
        }

        return FormatTaxpayerProfile(taxpayer);
    }

    [McpServerTool, Description("Get all tax returns for the taxpayer")]
    public async Task<string> GetTaxReturns()
    {
        // SECURITY: Data store filters by UserContext automatically
        _logger.LogInformation("GetTaxReturns called by user: {UserId}", _userContext.UserId);
        
        var returns = await _dataStore.GetTaxReturnsAsync();
        
        if (!returns.Any())
        {
            return "No tax returns found.";
        }

        return FormatTaxReturns(returns);
    }

    [McpServerTool, Description("Get tax return for a specific year")]
    public async Task<string> GetTaxReturnByYear(
        [Description("The tax year to retrieve (e.g., 2023, 2024)")] int year)
    {
        // SECURITY: Validate input
        if (year < 1900 || year > DateTime.Now.Year)
        {
            return $"Invalid tax year: {year}. Must be between 1900 and {DateTime.Now.Year}.";
        }

        _logger.LogInformation("GetTaxReturnByYear called by user: {UserId} for year: {Year}", 
            _userContext.UserId, year);
        
        var taxReturn = await _dataStore.GetTaxReturnByYearAsync(year);
        
        if (taxReturn == null)
        {
            return $"No tax return found for year {year}.";
        }

        return FormatTaxReturn(taxReturn);
    }

    [McpServerTool, Description("Get all deductions for a specific tax year")]
    public async Task<string> GetDeductionsByYear(
        [Description("The tax year (e.g., 2023, 2024)")] int year)
    {
        // SECURITY: Validate input
        if (year < 1900 || year > DateTime.Now.Year)
        {
            return $"Invalid tax year: {year}.";
        }

        _logger.LogInformation("GetDeductionsByYear called by user: {UserId} for year: {Year}", 
            _userContext.UserId, year);
        
        var deductions = await _dataStore.GetDeductionsByYearAsync(year);
        
        if (!deductions.Any())
        {
            return $"No deductions found for year {year}.";
        }

        return FormatDeductions(deductions, $"Deductions for {year}");
    }

    [McpServerTool, Description("Get deductions by category across all years")]
    public async Task<string> GetDeductionsByCategory(
        [Description("The category (MedicalExpenses, CharitableDonations, MortgageInterest, PropertyTaxes, BusinessExpenses, EducationExpenses, StateLocalTaxes, Other)")] string category)
    {
        // SECURITY: Validate and parse enum
        if (!Enum.TryParse<DeductionCategory>(category, true, out var deductionCategory))
        {
            return $"Invalid category: {category}. Valid options: {string.Join(", ", Enum.GetNames<DeductionCategory>())}";
        }

        _logger.LogInformation("GetDeductionsByCategory called by user: {UserId} for category: {Category}", 
            _userContext.UserId, category);
        
        var deductions = await _dataStore.GetDeductionsByCategoryAsync(deductionCategory);
        
        if (!deductions.Any())
        {
            return $"No deductions found for category: {category}.";
        }

        return FormatDeductions(deductions, $"Deductions in category: {category}");
    }

    [McpServerTool, Description("Calculate total deductions by category for a specific year")]
    public async Task<string> CalculateDeductionTotals(
        [Description("The tax year (e.g., 2023, 2024)")] int year)
    {
        // SECURITY: Validate input
        if (year < 1900 || year > DateTime.Now.Year)
        {
            return $"Invalid tax year: {year}.";
        }

        _logger.LogInformation("CalculateDeductionTotals called by user: {UserId} for year: {Year}", 
            _userContext.UserId, year);
        
        var totals = await _dataStore.GetDeductionTotalsByCategoryAsync(year);
        
        if (!totals.Any())
        {
            return $"No deductions found for year {year}.";
        }

        return FormatDeductionTotals(totals, year);
    }

    [McpServerTool, Description("Compare deductions between two years")]
    public async Task<string> CompareDeductionsYearly(
        [Description("First tax year")] int year1,
        [Description("Second tax year")] int year2)
    {
        // SECURITY: Validate inputs
        var currentYear = DateTime.Now.Year;
        if (year1 < 1900 || year1 > currentYear || year2 < 1900 || year2 > currentYear)
        {
            return $"Invalid years. Must be between 1900 and {currentYear}.";
        }

        _logger.LogInformation("CompareDeductionsYearly called by user: {UserId} for years: {Year1} and {Year2}", 
            _userContext.UserId, year1, year2);
        
        var comparison = await _dataStore.CompareDeductionsYearlyAsync(year1, year2);
        
        return FormatDeductionComparison(comparison, year1, year2);
    }

    [McpServerTool, Description("Get documents by type")]
    public async Task<string> GetDocumentsByType(
        [Description("Document type (W2, Form1099, Receipt, Invoice, BankStatement, MortgageStatement, DonationReceipt, MedicalBill, PropertyTaxBill, Other)")] string documentType)
    {
        // SECURITY: Validate and parse enum
        if (!Enum.TryParse<DocumentType>(documentType, true, out var docType))
        {
            return $"Invalid document type: {documentType}. Valid options: {string.Join(", ", Enum.GetNames<DocumentType>())}";
        }

        _logger.LogInformation("GetDocumentsByType called by user: {UserId} for type: {Type}", 
            _userContext.UserId, documentType);
        
        var documents = await _dataStore.GetDocumentsByTypeAsync(docType);
        
        if (!documents.Any())
        {
            return $"No documents found of type: {documentType}.";
        }

        return FormatDocuments(documents);
    }

    [McpServerTool, Description("Get all documents for a specific tax year")]
    public async Task<string> GetDocumentsByYear(
        [Description("The tax year")] int year)
    {
        // SECURITY: Validate input
        if (year < 1900 || year > DateTime.Now.Year)
        {
            return $"Invalid tax year: {year}.";
        }

        _logger.LogInformation("GetDocumentsByYear called by user: {UserId} for year: {Year}", 
            _userContext.UserId, year);
        
        var documents = await _dataStore.GetDocumentsByYearAsync(year);
        
        if (!documents.Any())
        {
            return $"No documents found for year {year}.";
        }

        return FormatDocuments(documents);
    }

    // Formatting helpers
    private string FormatTaxpayerProfile(Taxpayer taxpayer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== TAXPAYER PROFILE ===");
        sb.AppendLine($"Name: {taxpayer.Name}");
        sb.AppendLine($"Email: {taxpayer.Email}");
        sb.AppendLine($"SSN (Last 4): {taxpayer.SsnLast4}");
        sb.AppendLine($"Phone: {taxpayer.Phone}");
        sb.AppendLine($"Address: {taxpayer.Address}");
        sb.AppendLine($"Filing Status: {taxpayer.FilingStatus}");
        sb.AppendLine($"Profile Created: {taxpayer.CreatedDate:yyyy-MM-dd}");
        return sb.ToString();
    }

    private string FormatTaxReturns(List<TaxReturn> returns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== TAX RETURNS ({returns.Count}) ===\n");
        
        foreach (var ret in returns)
        {
            sb.AppendLine($"Tax Year: {ret.TaxYear}");
            sb.AppendLine($"Status: {ret.Status}");
            sb.AppendLine($"Filing Status: {ret.FilingStatus}");
            sb.AppendLine($"AGI: ${ret.AdjustedGrossIncome:N2}");
            sb.AppendLine($"Taxable Income: ${ret.TaxableIncome:N2}");
            sb.AppendLine($"Total Tax: ${ret.TotalTax:N2}");
            sb.AppendLine($"Total Deductions: ${ret.TotalDeductions:N2}");
            if (ret.FilingDate.HasValue)
                sb.AppendLine($"Filed: {ret.FilingDate:yyyy-MM-dd}");
            sb.AppendLine("---");
        }
        
        return sb.ToString();
    }

    private string FormatTaxReturn(TaxReturn taxReturn)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== TAX RETURN {taxReturn.TaxYear} ===");
        sb.AppendLine($"Status: {taxReturn.Status}");
        sb.AppendLine($"Filing Status: {taxReturn.FilingStatus}");
        sb.AppendLine($"Adjusted Gross Income: ${taxReturn.AdjustedGrossIncome:N2}");
        sb.AppendLine($"Taxable Income: ${taxReturn.TaxableIncome:N2}");
        sb.AppendLine($"Total Tax: ${taxReturn.TotalTax:N2}");
        sb.AppendLine($"Total Deductions: ${taxReturn.TotalDeductions:N2}");
        if (taxReturn.FilingDate.HasValue)
            sb.AppendLine($"Filing Date: {taxReturn.FilingDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(taxReturn.Notes))
            sb.AppendLine($"Notes: {taxReturn.Notes}");
        return sb.ToString();
    }

    private string FormatDeductions(List<Deduction> deductions, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {title.ToUpper()} ({deductions.Count}) ===\n");
        
        var total = 0m;
        foreach (var deduction in deductions)
        {
            sb.AppendLine($"Year: {deduction.TaxYear}");
            sb.AppendLine($"Category: {deduction.Category}");
            sb.AppendLine($"Description: {deduction.Description}");
            sb.AppendLine($"Amount: ${deduction.Amount:N2}");
            sb.AppendLine($"Date: {deduction.DateIncurred:yyyy-MM-dd}");
            sb.AppendLine("---");
            total += deduction.Amount;
        }
        
        sb.AppendLine($"\nTOTAL: ${total:N2}");
        return sb.ToString();
    }

    private string FormatDeductionTotals(Dictionary<DeductionCategory, decimal> totals, int year)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== DEDUCTION TOTALS FOR {year} ===\n");
        
        var grandTotal = 0m;
        foreach (var (category, amount) in totals.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"{category}: ${amount:N2}");
            grandTotal += amount;
        }
        
        sb.AppendLine($"\nGRAND TOTAL: ${grandTotal:N2}");
        return sb.ToString();
    }

    private string FormatDeductionComparison(Dictionary<int, List<Deduction>> comparison, int year1, int year2)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== DEDUCTION COMPARISON: {year1} vs {year2} ===\n");
        
        var year1Total = comparison.ContainsKey(year1) ? comparison[year1].Sum(d => d.Amount) : 0m;
        var year2Total = comparison.ContainsKey(year2) ? comparison[year2].Sum(d => d.Amount) : 0m;
        var difference = year2Total - year1Total;
        var percentChange = year1Total > 0 ? (difference / year1Total) * 100 : 0;
        
        sb.AppendLine($"{year1} Total: ${year1Total:N2}");
        sb.AppendLine($"{year2} Total: ${year2Total:N2}");
        sb.AppendLine($"Difference: ${difference:N2} ({percentChange:+0.0;-0.0}%)");
        sb.AppendLine("\nCategory Breakdown:");
        
        // Get all categories
        var allCategories = comparison.Values
            .SelectMany(d => d.Select(x => x.Category))
            .Distinct()
            .OrderBy(c => c);
        
        foreach (var category in allCategories)
        {
            var cat1Total = comparison.ContainsKey(year1) 
                ? comparison[year1].Where(d => d.Category == category).Sum(d => d.Amount) 
                : 0m;
            var cat2Total = comparison.ContainsKey(year2) 
                ? comparison[year2].Where(d => d.Category == category).Sum(d => d.Amount) 
                : 0m;
            var catDiff = cat2Total - cat1Total;
            
            sb.AppendLine($"\n{category}:");
            sb.AppendLine($"  {year1}: ${cat1Total:N2}");
            sb.AppendLine($"  {year2}: ${cat2Total:N2}");
            sb.AppendLine($"  Change: ${catDiff:N2}");
        }
        
        return sb.ToString();
    }

    private string FormatDocuments(List<Document> documents)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== DOCUMENTS ({documents.Count}) ===\n");
        
        foreach (var doc in documents)
        {
            sb.AppendLine($"Type: {doc.DocumentType}");
            sb.AppendLine($"File: {doc.FileName}");
            sb.AppendLine($"Year: {doc.TaxYear}");
            sb.AppendLine($"Category: {doc.Category}");
            sb.AppendLine($"Size: {FormatFileSize(doc.FileSize)}");
            sb.AppendLine($"Uploaded: {doc.UploadDate:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(doc.Notes))
                sb.AppendLine($"Notes: {doc.Notes}");
            sb.AppendLine("---");
        }
        
        return sb.ToString();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

