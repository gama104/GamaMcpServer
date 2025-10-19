using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Application.Interfaces;

/// <summary>
/// Interface for data storage operations
/// SECURITY: All operations are implicitly filtered by the current user's context
/// </summary>
public interface IDataStore
{
    // Taxpayer operations
    Task<Taxpayer?> GetTaxpayerProfileAsync();
    Task<List<Taxpayer>> GetAllTaxpayersAsync();
    
    // Tax Return operations
    Task<List<TaxReturn>> GetTaxReturnsAsync();
    Task<TaxReturn?> GetTaxReturnByYearAsync(int year);
    
    // Deduction operations
    Task<List<Deduction>> GetDeductionsByYearAsync(int year);
    Task<List<Deduction>> GetDeductionsByCategoryAsync(DeductionCategory category);
    Task<Dictionary<DeductionCategory, decimal>> GetDeductionTotalsByCategoryAsync(int year);
    Task<Dictionary<int, decimal>> GetDeductionTotalsByYearAsync();
    
    // Document operations
    Task<List<Document>> GetDocumentsByTypeAsync(DocumentType documentType);
    Task<List<Document>> GetDocumentsByYearAsync(int year);
    
    // Analysis operations
    Task<Dictionary<int, List<Deduction>>> CompareDeductionsYearlyAsync(int year1, int year2);
}

