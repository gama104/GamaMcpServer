# Generate JWT Token for Taxpayer MCP Server
# Enhanced with OAuth 2.1: audience and issuer validation

param(
    [string]$UserId = "test-user",
    [string]$Role = "Admin"
)

# Read JWT_SECRET from .env file
$envFile = ".env"
$SECRET = ""
if (Test-Path $envFile) {
    $envContent = Get-Content $envFile
    $secretLine = $envContent | Where-Object { $_ -match "^JWT_SECRET=" }
    if ($secretLine) {
        $SECRET = $secretLine -replace "^JWT_SECRET=", ""
    }
}

# Fallback to default if not found in .env
if ([string]::IsNullOrEmpty($SECRET)) {
    Write-Host "Warning: JWT_SECRET not found in .env, using fallback" -ForegroundColor Yellow
    $SECRET = "PLACEHOLDER-DO-NOT-USE-IN-PRODUCTION"
}

Write-Host "`n=== JWT Token Generator (OAuth 2.1) ===" -ForegroundColor Cyan

# Create header
$header = @{
    alg = "HS256"
    typ = "JWT"
} | ConvertTo-Json -Compress

# Create claims with OAuth 2.1 audience and issuer
$now = [DateTimeOffset]::UtcNow
$claims = @{
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" = $UserId
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = $Role
    sub = $UserId
    jti = [Guid]::NewGuid().ToString()
    aud = "taxpayer-mcp-server"
    iss = "taxpayer-auth-server"
    nbf = $now.ToUnixTimeSeconds()
    iat = $now.ToUnixTimeSeconds()
    exp = $now.AddHours(24).ToUnixTimeSeconds()
} | ConvertTo-Json -Compress

# Base64URL encode
function ConvertTo-Base64Url {
    param([string]$text)
    $bytes = [Text.Encoding]::UTF8.GetBytes($text)
    $base64 = [Convert]::ToBase64String($bytes)
    return $base64 -replace '\+','-' -replace '/','_' -replace '='
}

$headerBase64 = ConvertTo-Base64Url $header
$claimsBase64 = ConvertTo-Base64Url $claims

# Create signature
$message = "$headerBase64.$claimsBase64"
$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($SECRET))
$signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($message))
$signatureBase64 = ([Convert]::ToBase64String($signatureBytes) -replace '\+','-' -replace '/','_' -replace '=')

# Complete token
$token = "$message.$signatureBase64"

Write-Host "`nUser: $UserId" -ForegroundColor Yellow
Write-Host "Role: $Role" -ForegroundColor Yellow
Write-Host "Audience: taxpayer-mcp-server" -ForegroundColor Yellow
Write-Host "Issuer: taxpayer-auth-server" -ForegroundColor Yellow
Write-Host "Expires: 24 hours`n" -ForegroundColor Yellow

Write-Host "JWT Token:" -ForegroundColor Cyan
Write-Host $token -ForegroundColor White

Write-Host "`nReady to use in VS Code!" -ForegroundColor Green
Write-Host "Note: Token includes OAuth 2.1 audience/issuer claims" -ForegroundColor Gray
Write-Host ""

