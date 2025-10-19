using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Interfaces;

/// <summary>
/// Interface for tax reference data resources
/// These are READ-ONLY reference materials (not user-specific data)
/// </summary>
public interface ITaxResourceStore
{
    // Tax Rules Resources
    Task<TaxRules?> GetTaxRulesAsync(int year);
    Task<List<int>> GetAvailableTaxYearsAsync();
    
    // Tax Brackets Resources
    Task<TaxBrackets?> GetTaxBracketsAsync(int year, FilingStatus? filingStatus = null);
    
    // Standard Deductions Resources
    Task<StandardDeductions?> GetStandardDeductionsAsync(int year);
    Task<StandardDeduction?> GetStandardDeductionAsync(int year, FilingStatus filingStatus);
    
    // Form Instructions Resources
    Task<FormInstructions?> GetFormInstructionsAsync(string formNumber, int year);
    Task<List<string>> GetAvailableFormsAsync(int year);
    
    // Available Deductions Resources
    Task<AvailableDeductions?> GetAvailableDeductionsAsync(int year);
    Task<DeductionInfo?> GetDeductionInfoAsync(int year, DeductionCategory category);
    
    // Deduction Limits Resources
    Task<DeductionLimits?> GetDeductionLimitsAsync(int year);
    Task<DeductionLimit?> GetDeductionLimitAsync(int year, DeductionCategory category);
}

