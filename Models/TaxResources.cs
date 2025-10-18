namespace ProtectedMcpServer.Models;

/// <summary>
/// MCP Resources for Tax Reference Data
/// These represent static/dynamic knowledge that AI can access
/// </summary>

#region Tax Rules Models

public class TaxRules
{
    public int TaxYear { get; set; }
    public List<DeductionRule> DeductionRules { get; set; } = new();
    public List<EligibilityCriteria> EligibilityCriteria { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class DeductionRule
{
    public DeductionCategory Category { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public decimal? MaximumAmount { get; set; }
    public decimal? PercentageOfAGI { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool RequiresItemization { get; set; }
    public List<string> DocumentationRequired { get; set; } = new();
}

public class EligibilityCriteria
{
    public string CriteriaName { get; set; } = string.Empty;
    public DeductionCategory? AppliesTo { get; set; }
    public decimal? MinimumAGI { get; set; }
    public decimal? MaximumAGI { get; set; }
    public FilingStatus? RequiredFilingStatus { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion

#region Tax Brackets Models

public class TaxBrackets
{
    public int TaxYear { get; set; }
    public List<TaxBracket> Brackets { get; set; } = new();
}

public class TaxBracket
{
    public FilingStatus FilingStatus { get; set; }
    public decimal IncomeMin { get; set; }
    public decimal IncomeMax { get; set; }
    public decimal TaxRate { get; set; }
    public decimal BaseTax { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion

#region Standard Deductions Models

public class StandardDeductions
{
    public int TaxYear { get; set; }
    public List<StandardDeduction> Deductions { get; set; } = new();
}

public class StandardDeduction
{
    public FilingStatus FilingStatus { get; set; }
    public decimal Amount { get; set; }
    public decimal AdditionalAgeAmount { get; set; }  // For age 65+
    public decimal AdditionalBlindAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

#endregion

#region Form Instructions Models

public class FormInstructions
{
    public string FormNumber { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public List<FormSection> Sections { get; set; } = new();
    public List<string> CommonMistakes { get; set; } = new();
    public string FilingDeadline { get; set; } = string.Empty;
}

public class FormSection
{
    public string SectionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> RequiredDocuments { get; set; } = new();
}

#endregion

#region Available Deductions Models

public class AvailableDeductions
{
    public int TaxYear { get; set; }
    public List<DeductionInfo> Deductions { get; set; } = new();
}

public class DeductionInfo
{
    public DeductionCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeductionType Type { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? AGIPercentageLimit { get; set; }
    public List<string> EligibilityRequirements { get; set; } = new();
    public List<string> Documentation { get; set; } = new();
    public List<string> CommonExamples { get; set; } = new();
}

#endregion

#region Deduction Limits Models

public class DeductionLimits
{
    public int TaxYear { get; set; }
    public List<DeductionLimit> Limits { get; set; } = new();
}

public class DeductionLimit
{
    public DeductionCategory Category { get; set; }
    public decimal? DollarCap { get; set; }
    public decimal? AGIPercentage { get; set; }
    public PhaseOutInfo? PhaseOut { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class PhaseOutInfo
{
    public decimal PhaseOutBegins { get; set; }
    public decimal PhaseOutComplete { get; set; }
    public FilingStatus FilingStatus { get; set; }
}

#endregion

