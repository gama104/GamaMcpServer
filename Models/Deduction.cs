namespace ProtectedMcpServer.Models;

/// <summary>
/// Represents a tax deduction
/// SECURITY: UserId field ensures data isolation per user
/// </summary>
public class Deduction
{
    /// <summary>
    /// Unique identifier for the deduction
    /// </summary>
    public string DeductionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID from JWT token - CRITICAL for data isolation
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the taxpayer
    /// </summary>
    public string TaxpayerId { get; set; } = string.Empty;

    /// <summary>
    /// Tax year this deduction applies to
    /// </summary>
    public int TaxYear { get; set; }

    /// <summary>
    /// Category of the deduction
    /// </summary>
    public DeductionCategory Category { get; set; }

    /// <summary>
    /// Description of the deduction
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount of the deduction
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date the expense was incurred
    /// </summary>
    public DateTime DateIncurred { get; set; }

    /// <summary>
    /// Reference to supporting document
    /// </summary>
    public string? DocumentReference { get; set; }

    /// <summary>
    /// Type of deduction
    /// </summary>
    public DeductionType DeductionType { get; set; } = DeductionType.Itemized;

    /// <summary>
    /// Date record was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Deduction category enumeration
/// </summary>
public enum DeductionCategory
{
    MedicalExpenses,
    CharitableDonations,
    MortgageInterest,
    PropertyTaxes,
    BusinessExpenses,
    EducationExpenses,
    StateLocalTaxes,
    Other
}

/// <summary>
/// Deduction type enumeration
/// </summary>
public enum DeductionType
{
    Standard,
    Itemized
}

