namespace ProtectedMcpServer.Models;

/// <summary>
/// Represents a tax return for a specific year
/// SECURITY: UserId field ensures data isolation per user
/// </summary>
public class TaxReturn
{
    /// <summary>
    /// Unique identifier for the tax return
    /// </summary>
    public string ReturnId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID from JWT token - CRITICAL for data isolation
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the taxpayer
    /// </summary>
    public string TaxpayerId { get; set; } = string.Empty;

    /// <summary>
    /// Tax year for this return
    /// </summary>
    public int TaxYear { get; set; }

    /// <summary>
    /// Filing status for this return
    /// </summary>
    public FilingStatus FilingStatus { get; set; }

    /// <summary>
    /// Adjusted Gross Income
    /// </summary>
    public decimal AdjustedGrossIncome { get; set; }

    /// <summary>
    /// Taxable Income
    /// </summary>
    public decimal TaxableIncome { get; set; }

    /// <summary>
    /// Total Tax Liability
    /// </summary>
    public decimal TotalTax { get; set; }

    /// <summary>
    /// Total Deductions
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Date return was filed
    /// </summary>
    public DateTime? FilingDate { get; set; }

    /// <summary>
    /// Current status of the return
    /// </summary>
    public ReturnStatus Status { get; set; } = ReturnStatus.Draft;

    /// <summary>
    /// Notes or additional information
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Date record was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date record was last updated
    /// </summary>
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tax return status enumeration
/// </summary>
public enum ReturnStatus
{
    Draft,
    Filed,
    Amended,
    Accepted,
    Rejected
}

