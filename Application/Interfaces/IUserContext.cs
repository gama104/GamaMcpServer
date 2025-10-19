namespace ProtectedMcpServer.Application.Interfaces;

/// <summary>
/// Interface for accessing the current authenticated user's context
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's unique identifier from JWT claims
    /// </summary>
    string UserId { get; }
    
    /// <summary>
    /// Gets the current user's role from JWT claims
    /// </summary>
    string Role { get; }
    
    /// <summary>
    /// Gets whether a user is currently authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}

