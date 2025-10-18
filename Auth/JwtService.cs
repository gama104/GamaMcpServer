using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProtectedMcpServer.Auth;

public class JwtService
{
    private readonly string _secret;
    private readonly string _audience;
    private readonly string _issuer;
    private readonly int _tokenExpirationHours;
    private readonly bool _validateAudience;
    private readonly bool _validateIssuer;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        // Priority: Environment variable > Configuration
        _secret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET must be configured in environment or appsettings.json");
        
        // SECURITY: Validate secret strength
        var minSecretLength = configuration.GetValue<int>("Security:JWT:MinimumSecretLength", 32);
        if (_secret.Length < minSecretLength)
        {
            _logger.LogWarning("JWT_SECRET length ({Length}) is below recommended minimum ({Min})", 
                _secret.Length, minSecretLength);
        }

        // SECURITY: OAuth 2.1 - Audience and Issuer for token binding
        _audience = configuration["MCP:Audience"] ?? "taxpayer-mcp-server";
        _issuer = configuration["MCP:Issuer"] ?? "taxpayer-auth-server";
        _tokenExpirationHours = configuration.GetValue<int>("Security:JWT:TokenExpirationHours", 24);
        _validateAudience = configuration.GetValue<bool>("Security:JWT:ValidateAudience", true);
        _validateIssuer = configuration.GetValue<bool>("Security:JWT:ValidateIssuer", true);
        
        _logger = logger;
        _logger.LogInformation("JWT Service initialized - Secret: {Length} chars, Audience: {Audience}, Issuer: {Issuer}, ValidateAud: {ValidateAud}, ValidateIss: {ValidateIss}", 
            _secret.Length, _audience, _issuer, _validateAudience, _validateIssuer);
    }

    public string GenerateToken(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId)
            }),
            // SECURITY: OAuth 2.1 - Bind token to specific resource server
            Audience = _audience,
            Issuer = _issuer,
            // SECURITY: Token lifetime
            Expires = DateTime.UtcNow.AddHours(_tokenExpirationHours),
            NotBefore = DateTime.UtcNow,
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        
        _logger.LogInformation("Token generated for user: {UserId}, expires: {Expiry}", 
            userId, tokenDescriptor.Expires);
        
        return tokenString;
    }

    public AuthenticatedUser? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            // SECURITY: Comprehensive token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                
                // SECURITY: OAuth 2.1 - Validate issuer (configurable)
                ValidateIssuer = _validateIssuer,
                ValidIssuer = _validateIssuer ? _issuer : null,
                
                // SECURITY: OAuth 2.1 - Validate audience/resource indicator (configurable)
                ValidateAudience = _validateAudience,
                ValidAudience = _validateAudience ? _audience : null,
                
                // SECURITY: Validate token lifetime
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,  // No clock skew tolerance
                
                // SECURITY: Require expiration and signed tokens
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // SECURITY: Additional validation - ensure token is JWT with correct algorithm
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Token validation failed: Invalid algorithm");
                return null;
            }
            
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (userId == null || roleString == null)
            {
                _logger.LogWarning("Token missing required claims");
                return null;
            }

            if (!Enum.TryParse<Role>(roleString, out var role))
            {
                _logger.LogWarning("Invalid role in token: {Role}", roleString);
                return null;
            }

            _logger.LogDebug("Token validated successfully for user: {UserId}", userId);
            return new AuthenticatedUser(userId, role);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expired");
            return null;
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid audience");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid issuer");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Security token exception");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Unexpected error");
            return null;
        }
    }
}

public record AuthenticatedUser(string Id, Role Role);

public enum Role
{
    Admin,
    User,
    ReadOnly
}


