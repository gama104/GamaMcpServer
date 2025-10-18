namespace ProtectedMcpServer.Models;

/// <summary>
/// Represents a taxpayer in the system
/// SECURITY: UserId field ensures data isolation per user
/// </summary>
public class Taxpayer
{
    /// <summary>
    /// Unique identifier for the taxpayer record
    /// </summary>
    public string TaxpayerId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID from JWT token - CRITICAL for data isolation
    /// SECURITY: This must match the authenticated user's ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the taxpayer
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Social Security Number (last 4 digits only for security)
    /// </summary>
    public string SsnLast4 { get; set; } = string.Empty;

    /// <summary>
    /// Mailing address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Tax filing status
    /// </summary>
    public FilingStatus FilingStatus { get; set; } = FilingStatus.Single;

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
/// Tax filing status enumeration
/// </summary>
public enum FilingStatus
{
    Single,
    MarriedFilingJointly,
    MarriedFilingSeparately,
    HeadOfHousehold,
    QualifyingWidow
}

