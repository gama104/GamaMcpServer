namespace ProtectedMcpServer.Models;

/// <summary>
/// Represents a tax-related document
/// SECURITY: UserId field ensures data isolation per user
/// </summary>
public class Document
{
    /// <summary>
    /// Unique identifier for the document
    /// </summary>
    public string DocumentId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID from JWT token - CRITICAL for data isolation
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the taxpayer
    /// </summary>
    public string TaxpayerId { get; set; } = string.Empty;

    /// <summary>
    /// Tax year this document relates to
    /// </summary>
    public int TaxYear { get; set; }

    /// <summary>
    /// Type of document
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path or reference
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Date document was uploaded
    /// </summary>
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Category for organization
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Additional notes about the document
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Document type enumeration
/// </summary>
public enum DocumentType
{
    W2,
    Form1099,
    Receipt,
    Invoice,
    BankStatement,
    MortgageStatement,
    DonationReceipt,
    MedicalBill,
    PropertyTaxBill,
    Other
}

