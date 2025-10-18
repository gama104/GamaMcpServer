using System.Text.Json;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Data;

/// <summary>
/// Tax Reference Data Store
/// Provides READ-ONLY tax rules, brackets, forms, and deduction information
/// This data is PUBLIC (not user-specific) but authoritative
/// </summary>
public class TaxResourceStore : ITaxResourceStore
{
    private readonly string _resourcesPath;
    private readonly ILogger<TaxResourceStore> _logger;
    private readonly Dictionary<int, TaxRules> _taxRulesCache = new();
    private readonly Dictionary<int, TaxBrackets> _taxBracketsCache = new();
    private readonly Dictionary<int, StandardDeductions> _standardDeductionsCache = new();

    public TaxResourceStore(ILogger<TaxResourceStore> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _resourcesPath = Path.Combine(env.ContentRootPath, "Resources");
        
        // Ensure resources directory exists
        if (!Directory.Exists(_resourcesPath))
        {
            Directory.CreateDirectory(_resourcesPath);
            _logger.LogInformation("Created resources directory: {Path}", _resourcesPath);
        }
        
        // Initialize with sample data
        InitializeSampleResources();
    }

    #region Tax Rules

    public async Task<TaxRules?> GetTaxRulesAsync(int year)
    {
        if (_taxRulesCache.TryGetValue(year, out var cached))
        {
            return cached;
        }

        var filePath = Path.Combine(_resourcesPath, $"tax-rules-{year}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Tax rules for year {Year} not found", year);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var rules = JsonSerializer.Deserialize<TaxRules>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (rules != null)
            {
                _taxRulesCache[year] = rules;
            }
            
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tax rules for year {Year}", year);
            return null;
        }
    }

    public Task<List<int>> GetAvailableTaxYearsAsync()
    {
        var years = new List<int> { 2023, 2024, 2025 };
        return Task.FromResult(years);
    }

    #endregion

    #region Tax Brackets

    public async Task<TaxBrackets?> GetTaxBracketsAsync(int year, FilingStatus? filingStatus = null)
    {
        if (_taxBracketsCache.TryGetValue(year, out var cached))
        {
            if (filingStatus.HasValue)
            {
                var filtered = new TaxBrackets
                {
                    TaxYear = cached.TaxYear,
                    Brackets = cached.Brackets.Where(b => b.FilingStatus == filingStatus.Value).ToList()
                };
                return filtered;
            }
            return cached;
        }

        // Return in-memory data
        var brackets = GetSampleTaxBrackets(year);
        if (brackets != null)
        {
            _taxBracketsCache[year] = brackets;
        }
        
        return await Task.FromResult(brackets);
    }

    #endregion

    #region Standard Deductions

    public async Task<StandardDeductions?> GetStandardDeductionsAsync(int year)
    {
        if (_standardDeductionsCache.TryGetValue(year, out var cached))
        {
            return cached;
        }

        var deductions = GetSampleStandardDeductions(year);
        if (deductions != null)
        {
            _standardDeductionsCache[year] = deductions;
        }
        
        return await Task.FromResult(deductions);
    }

    public async Task<StandardDeduction?> GetStandardDeductionAsync(int year, FilingStatus filingStatus)
    {
        var all = await GetStandardDeductionsAsync(year);
        return all?.Deductions.FirstOrDefault(d => d.FilingStatus == filingStatus);
    }

    #endregion

    #region Form Instructions

    public Task<FormInstructions?> GetFormInstructionsAsync(string formNumber, int year)
    {
        var instructions = GetSampleFormInstructions(formNumber, year);
        return Task.FromResult(instructions);
    }

    public Task<List<string>> GetAvailableFormsAsync(int year)
    {
        var forms = new List<string> { "1040", "Schedule A", "Schedule C", "W-2", "1099-MISC" };
        return Task.FromResult(forms);
    }

    #endregion

    #region Available Deductions

    public Task<AvailableDeductions?> GetAvailableDeductionsAsync(int year)
    {
        var deductions = GetSampleAvailableDeductions(year);
        return Task.FromResult(deductions);
    }

    public async Task<DeductionInfo?> GetDeductionInfoAsync(int year, DeductionCategory category)
    {
        var all = await GetAvailableDeductionsAsync(year);
        return all?.Deductions.FirstOrDefault(d => d.Category == category);
    }

    #endregion

    #region Deduction Limits

    public Task<DeductionLimits?> GetDeductionLimitsAsync(int year)
    {
        var limits = GetSampleDeductionLimits(year);
        return Task.FromResult(limits);
    }

    public async Task<DeductionLimit?> GetDeductionLimitAsync(int year, DeductionCategory category)
    {
        var all = await GetDeductionLimitsAsync(year);
        return all?.Limits.FirstOrDefault(l => l.Category == category);
    }

    #endregion

    #region Sample Data Generation

    private void InitializeSampleResources()
    {
        _logger.LogInformation("Initializing sample tax resources for years 2023-2025");
    }

    private TaxBrackets GetSampleTaxBrackets(int year)
    {
        // 2024 Tax Brackets (example)
        return new TaxBrackets
        {
            TaxYear = year,
            Brackets = new List<TaxBracket>
            {
                new() { FilingStatus = FilingStatus.Single, IncomeMin = 0, IncomeMax = 11600, TaxRate = 0.10m, BaseTax = 0 },
                new() { FilingStatus = FilingStatus.Single, IncomeMin = 11600, IncomeMax = 47150, TaxRate = 0.12m, BaseTax = 1160 },
                new() { FilingStatus = FilingStatus.Single, IncomeMin = 47150, IncomeMax = 100525, TaxRate = 0.22m, BaseTax = 5426 },
                new() { FilingStatus = FilingStatus.Single, IncomeMin = 100525, IncomeMax = 191950, TaxRate = 0.24m, BaseTax = 17168.50m },
                new() { FilingStatus = FilingStatus.MarriedFilingJointly, IncomeMin = 0, IncomeMax = 23200, TaxRate = 0.10m, BaseTax = 0 },
                new() { FilingStatus = FilingStatus.MarriedFilingJointly, IncomeMin = 23200, IncomeMax = 94300, TaxRate = 0.12m, BaseTax = 2320 },
                new() { FilingStatus = FilingStatus.MarriedFilingJointly, IncomeMin = 94300, IncomeMax = 201050, TaxRate = 0.22m, BaseTax = 10852 },
            }
        };
    }

    private StandardDeductions GetSampleStandardDeductions(int year)
    {
        // 2024 Standard Deductions
        return new StandardDeductions
        {
            TaxYear = year,
            Deductions = new List<StandardDeduction>
            {
                new() { FilingStatus = FilingStatus.Single, Amount = 14600, AdditionalAgeAmount = 1950, AdditionalBlindAmount = 1950 },
                new() { FilingStatus = FilingStatus.MarriedFilingJointly, Amount = 29200, AdditionalAgeAmount = 1550, AdditionalBlindAmount = 1550 },
                new() { FilingStatus = FilingStatus.MarriedFilingSeparately, Amount = 14600, AdditionalAgeAmount = 1550, AdditionalBlindAmount = 1550 },
                new() { FilingStatus = FilingStatus.HeadOfHousehold, Amount = 21900, AdditionalAgeAmount = 1950, AdditionalBlindAmount = 1950 },
            }
        };
    }

    private FormInstructions? GetSampleFormInstructions(string formNumber, int year)
    {
        if (formNumber == "1040")
        {
            return new FormInstructions
            {
                FormNumber = "1040",
                FormName = "U.S. Individual Income Tax Return",
                TaxYear = year,
                Purpose = "Report your income, deductions, and calculate tax liability",
                FilingDeadline = "April 15 of the following year",
                Sections = new List<FormSection>
                {
                    new() { SectionNumber = "1", Title = "Filing Status", Instructions = "Check one box for your filing status" },
                    new() { SectionNumber = "2", Title = "Income", Instructions = "Report all income from W-2s, 1099s, and other sources" },
                    new() { SectionNumber = "3", Title = "Deductions", Instructions = "Choose standard or itemized deductions" },
                },
                CommonMistakes = new List<string>
                {
                    "Forgetting to sign the return",
                    "Math errors in calculations",
                    "Missing or incorrect SSN",
                    "Not attaching required forms"
                }
            };
        }
        return null;
    }

    private AvailableDeductions GetSampleAvailableDeductions(int year)
    {
        return new AvailableDeductions
        {
            TaxYear = year,
            Deductions = new List<DeductionInfo>
            {
                new()
                {
                    Category = DeductionCategory.MedicalExpenses,
                    Name = "Medical and Dental Expenses",
                    Description = "Unreimbursed medical and dental expenses exceeding 7.5% of AGI",
                    Type = DeductionType.Itemized,
                    AGIPercentageLimit = 0.075m,
                    EligibilityRequirements = new() { "Expenses must exceed 7.5% of AGI", "Must itemize deductions" },
                    Documentation = new() { "Receipts", "Insurance statements", "Bills from providers" },
                    CommonExamples = new() { "Doctor visits", "Prescriptions", "Medical equipment", "Insurance premiums" }
                },
                new()
                {
                    Category = DeductionCategory.CharitableDonations,
                    Name = "Charitable Contributions",
                    Description = "Cash and property donations to qualified organizations",
                    Type = DeductionType.Itemized,
                    AGIPercentageLimit = 0.60m,
                    EligibilityRequirements = new() { "Organization must be IRS-qualified", "Must have documentation" },
                    Documentation = new() { "Donation receipts", "Bank records", "Appraisals for property >$5000" },
                    CommonExamples = new() { "Cash donations", "Clothing", "Household items", "Vehicles" }
                },
                new()
                {
                    Category = DeductionCategory.MortgageInterest,
                    Name = "Home Mortgage Interest",
                    Description = "Interest paid on mortgages for qualified residences",
                    Type = DeductionType.Itemized,
                    MaxAmount = 750000,
                    EligibilityRequirements = new() { "Mortgage on primary or secondary residence", "Loan used to buy, build, or improve home" },
                    Documentation = new() { "Form 1098 from lender", "Mortgage statements" },
                    CommonExamples = new() { "Primary mortgage interest", "Home equity loan interest", "Refinance interest" }
                },
            }
        };
    }

    private DeductionLimits GetSampleDeductionLimits(int year)
    {
        return new DeductionLimits
        {
            TaxYear = year,
            Limits = new List<DeductionLimit>
            {
                new()
                {
                    Category = DeductionCategory.StateLocalTaxes,
                    DollarCap = 10000,
                    Notes = "Combined cap for state/local income, sales, and property taxes"
                },
                new()
                {
                    Category = DeductionCategory.MortgageInterest,
                    DollarCap = 750000,
                    Notes = "Interest on mortgage debt up to $750,000 ($375,000 if married filing separately)"
                },
                new()
                {
                    Category = DeductionCategory.CharitableDonations,
                    AGIPercentage = 0.60m,
                    Notes = "Cash contributions limited to 60% of AGI; property has different limits"
                },
                new()
                {
                    Category = DeductionCategory.MedicalExpenses,
                    AGIPercentage = 0.075m,
                    Notes = "Only expenses exceeding 7.5% of AGI are deductible"
                },
            }
        };
    }

    #endregion
}

