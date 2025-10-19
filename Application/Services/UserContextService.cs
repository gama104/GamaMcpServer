using System.Security.Claims;
using ProtectedMcpServer.Application.Interfaces;

namespace ProtectedMcpServer.Application.Services;

/// <summary>
/// Service that extracts and provides the current user's context from HTTP request
/// SECURITY: This is the single source of truth for user identity
/// </summary>
public class UserContextService : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user's ID from the JWT nameidentifier claim
    /// SECURITY: This value is extracted from the validated JWT token
    /// </summary>
    public string UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID claim not found in token");
            }

            return userId;
        }
    }

    /// <summary>
    /// Gets the current user's role from the JWT role claim
    /// </summary>
    public string Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Role)?.Value
                ?? user?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value
                ?? "User";
        }
    }

    /// <summary>
    /// Gets whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }
    }
}

