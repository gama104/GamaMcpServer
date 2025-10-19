using MediatR;
using ProtectedMcpServer.Application.Queries;
using ProtectedMcpServer.Models;
using ProtectedMcpServer.Services;

namespace ProtectedMcpServer.Data;

/// <summary>
/// CQRS-based data store following 2025 best practices
/// Uses MediatR for query handling with Entity Framework Core
/// </summary>
public class CqrsDataStore : IDataStore
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;
    private readonly ILogger<CqrsDataStore> _logger;

    public CqrsDataStore(IMediator mediator, IUserContext userContext, ILogger<CqrsDataStore> logger)
    {
        _mediator = mediator;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's taxpayer profile using CQRS pattern
    /// </summary>
    public async Task<Taxpayer?> GetTaxpayerProfileAsync()
    {
        _logger.LogInformation("Getting taxpayer profile for user: {UserId}", _userContext.UserId);
        
        var query = new GetTaxpayerProfileQuery(_userContext.UserId);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get all taxpayers for current user using CQRS pattern
    /// </summary>
    public async Task<List<Taxpayer>> GetAllTaxpayersAsync()
    {
        _logger.LogInformation("Getting all taxpayers for user: {UserId}", _userContext.UserId);
        
        var taxpayer = await GetTaxpayerProfileAsync();
        return taxpayer != null ? new List<Taxpayer> { taxpayer } : new List<Taxpayer>();
    }

    /// <summary>
    /// Get tax returns for current user using CQRS pattern
    /// </summary>
    public async Task<List<TaxReturn>> GetTaxReturnsAsync()
    {
        _logger.LogInformation("Getting tax returns for user: {UserId}", _userContext.UserId);
        
        var query = new GetTaxReturnsQuery(_userContext.UserId);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get specific year tax return for current user using CQRS pattern
    /// </summary>
    public async Task<TaxReturn?> GetTaxReturnByYearAsync(int year)
    {
        _logger.LogInformation("Getting tax return for user: {UserId}, year: {Year}", _userContext.UserId, year);
        
        var query = new GetTaxReturnByYearQuery(_userContext.UserId, year);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get deductions for specific year for current user using CQRS pattern
    /// </summary>
    public async Task<List<Deduction>> GetDeductionsByYearAsync(int year)
    {
        _logger.LogInformation("Getting deductions for user: {UserId}, year: {Year}", _userContext.UserId, year);
        
        var query = new GetDeductionsByYearQuery(_userContext.UserId, year);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get deductions by category for current user using CQRS pattern
    /// </summary>
    public async Task<List<Deduction>> GetDeductionsByCategoryAsync(DeductionCategory category)
    {
        _logger.LogInformation("Getting deductions for user: {UserId}, category: {Category}", _userContext.UserId, category);
        
        var query = new GetDeductionsByCategoryQuery(_userContext.UserId, category);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Calculate deduction totals by category for current user using CQRS pattern
    /// </summary>
    public async Task<Dictionary<DeductionCategory, decimal>> GetDeductionTotalsByCategoryAsync(int year)
    {
        _logger.LogInformation("Getting deduction totals by category for user: {UserId}, year: {Year}", _userContext.UserId, year);
        
        var query = new GetDeductionTotalsByCategoryQuery(_userContext.UserId, year);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Calculate deduction totals by year for current user using CQRS pattern
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetDeductionTotalsByYearAsync()
    {
        _logger.LogInformation("Getting deduction totals by year for user: {UserId}", _userContext.UserId);
        
        var taxReturns = await GetTaxReturnsAsync();
        var totals = new Dictionary<int, decimal>();
        
        foreach (var taxReturn in taxReturns)
        {
            var yearTotals = await GetDeductionTotalsByCategoryAsync(taxReturn.TaxYear);
            totals[taxReturn.TaxYear] = yearTotals.Values.Sum();
        }
        
        return totals;
    }

    /// <summary>
    /// Get documents by type for current user using CQRS pattern
    /// </summary>
    public async Task<List<Document>> GetDocumentsByTypeAsync(DocumentType documentType)
    {
        _logger.LogInformation("Getting documents for user: {UserId}, type: {DocumentType}", _userContext.UserId, documentType);
        
        var query = new GetDocumentsByTypeQuery(_userContext.UserId, documentType);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get documents by year for current user using CQRS pattern
    /// </summary>
    public async Task<List<Document>> GetDocumentsByYearAsync(int year)
    {
        _logger.LogInformation("Getting documents for user: {UserId}, year: {Year}", _userContext.UserId, year);
        
        var query = new GetDocumentsByYearQuery(_userContext.UserId, year);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Compare deductions between two years for current user using CQRS pattern
    /// </summary>
    public async Task<Dictionary<int, List<Deduction>>> CompareDeductionsYearlyAsync(int year1, int year2)
    {
        _logger.LogInformation("Comparing deductions for user: {UserId}, years: {Year1} vs {Year2}", _userContext.UserId, year1, year2);
        
        var query = new CompareDeductionsYearlyQuery(_userContext.UserId, year1, year2);
        return await _mediator.Send(query);
    }
}
