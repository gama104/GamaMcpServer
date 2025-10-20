# Start Taxpayer MCP Server with Configurable Port
# Supports both local development and Docker deployment

param(
    [switch]$Docker,
    [switch]$Local,
    [int]$Port = 7071,
    [switch]$Interactive
)

Write-Host "Taxpayer MCP Server Launcher" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Validate port number
if ($Port -lt 1024 -or $Port -gt 65535) {
    Write-Host "Invalid port number: $Port" -ForegroundColor Red
    Write-Host "Port must be between 1024 and 65535" -ForegroundColor Yellow
    exit 1
}

# Interactive mode - ask user for preferences
if ($Interactive) {
    Write-Host "`nServer Configuration:" -ForegroundColor Yellow
    
    # Ask for deployment method
    do {
        $deployment = Read-Host "Deployment method (D)ocker or (L)ocal? [D/L]"
    } while ($deployment -notmatch '^[DLdl]$')
    
    if ($deployment -match '^[Dd]$') {
        $Docker = $true
        $Port = 7071  # Docker always uses 7071
        Write-Host "Selected: Docker deployment on port 7071" -ForegroundColor Green
    } else {
        $Local = $true
        
        # Ask for port
        do {
            $portInput = Read-Host "Enter port number (1024-65535) [7071]"
            if ([string]::IsNullOrEmpty($portInput)) {
                $Port = 7071
                break
            }
            if ([int]::TryParse($portInput, [ref]$Port) -and $Port -ge 1024 -and $Port -le 65535) {
                break
            }
            Write-Host "Invalid port. Please enter a number between 1024 and 65535." -ForegroundColor Red
        } while ($true)
        
        Write-Host "Selected: Local deployment on port $Port" -ForegroundColor Green
    }
}

# Check if .env file exists
if (-not (Test-Path ".env")) {
    Write-Host "`n.env file not found. Creating from template..." -ForegroundColor Yellow
    
    if (Test-Path "env.example.txt") {
        Copy-Item "env.example.txt" ".env"
        
        # Generate secure JWT secret
        $jwtSecret = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
        (Get-Content ".env") -replace 'JWT_SECRET=.*', "JWT_SECRET=$jwtSecret" | Set-Content ".env"
        
        Write-Host "Created .env file with secure JWT secret" -ForegroundColor Green
    } else {
        Write-Host "env.example.txt not found. Please create .env file manually." -ForegroundColor Red
        exit 1
    }
}

# Load environment variables
$envContent = Get-Content ".env"
foreach ($line in $envContent) {
    if ($line -match "^([^#][^=]+)=(.*)$") {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($key, $value, "Process")
    }
}

if ($Docker) {
    Write-Host "`nStarting Docker container..." -ForegroundColor Yellow
    
    # Stop any existing containers
    Write-Host "Stopping existing containers..." -ForegroundColor Gray
    docker-compose down -q 2>$null
    
    # Start Docker container
    Write-Host "Starting Docker container on port 7071..." -ForegroundColor Gray
    docker-compose up -d --build
    
    # Wait for container to be healthy
    Write-Host "Waiting for container to be healthy..." -ForegroundColor Gray
    $maxWait = 60
    $waited = 0
    
    do {
        Start-Sleep -Seconds 2
        $waited++
        $containerStatus = docker ps --filter "name=taxpayer-mcp-server" --format "{{.Status}}" 2>$null
        if ($containerStatus -match "healthy") {
            Write-Host "Docker container is healthy" -ForegroundColor Green
            break
        }
    } while ($waited -lt $maxWait)
    
    if ($waited -ge $maxWait) {
        Write-Host "Docker container failed to become healthy within $maxWait seconds" -ForegroundColor Red
        Write-Host "Container logs:" -ForegroundColor Yellow
        docker logs taxpayer-mcp-server
        exit 1
    }
    
    $serverUrl = "http://localhost:7071"
    
} else {
    Write-Host "`nStarting local server..." -ForegroundColor Yellow
    
    # Check if port is available
    try {
        $connection = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Write-Host "Warning: Port $Port appears to be in use" -ForegroundColor Yellow
            Write-Host "Stopping any existing dotnet processes..." -ForegroundColor Gray
            Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
            Start-Sleep -Seconds 2
        }
    } catch {
        Write-Host "Note: Could not check port availability" -ForegroundColor Gray
    }
    
    # Set environment variables for local development
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    
    Write-Host "Starting server on port $Port..." -ForegroundColor Gray
    
    # Start server in background
    $serverJob = Start-Job -ScriptBlock {
        param($Port)
        $env:ASPNETCORE_URLS = "http://localhost:$Port"
        Set-Location $using:PWD
        dotnet run --no-build
    } -ArgumentList $Port
    
    # Wait for server to start
    Write-Host "Waiting for server to start..." -ForegroundColor Gray
    $maxWait = 30
    $waited = 0
    
    do {
        Start-Sleep -Seconds 1
        $waited++
        try {
            $response = Invoke-RestMethod "http://localhost:$Port/" -TimeoutSec 2
            if ($response.status -eq "ok") {
                Write-Host "Local server started successfully" -ForegroundColor Green
                break
            }
        } catch {
            # Continue waiting
        }
    } while ($waited -lt $maxWait)
    
    if ($waited -ge $maxWait) {
        Write-Host "Local server failed to start within $maxWait seconds" -ForegroundColor Red
        Stop-Job $serverJob
        Remove-Job $serverJob
        exit 1
    }
    
    $serverUrl = "http://localhost:$Port"
}

# Test the server
Write-Host "`nTesting server..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod "$serverUrl/" -TimeoutSec 10
    Write-Host "Server is healthy" -ForegroundColor Green
    Write-Host "   Name: $($response.name)" -ForegroundColor Gray
    Write-Host "   Protocol: $($response.protocolVersion)" -ForegroundColor Gray
    Write-Host "   Tools: $($response.tools.Count)" -ForegroundColor Gray
} catch {
    Write-Host "Server health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Generate JWT token
Write-Host "`nGenerating JWT token..." -ForegroundColor Yellow
try {
    $tokenOutput = & .\generate-jwt.ps1 -UserId "test-user" -Role "Admin"
    $token = $tokenOutput | Select-Object -Last 1 | ForEach-Object { $_.Trim() }
    
    if ($token -match "^[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+$") {
        Write-Host "JWT token generated successfully" -ForegroundColor Green
    } else {
        Write-Host "Failed to generate valid JWT token" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "JWT generation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`nServer Started Successfully!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host "Server URL: $serverUrl" -ForegroundColor White
Write-Host "Deployment: $(if ($Docker) { 'Docker' } else { 'Local' })" -ForegroundColor White
Write-Host "Port: $Port" -ForegroundColor White
Write-Host "Environment: $(if ($Docker) { 'Production' } else { 'Development' })" -ForegroundColor White

Write-Host "`nYour JWT Token (for VS Code):" -ForegroundColor Cyan
Write-Host $token -ForegroundColor White

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "1. Copy the JWT token above" -ForegroundColor White
Write-Host "2. Open .vscode/mcp.json in VS Code" -ForegroundColor White
Write-Host "3. Paste the token in the 'token' field" -ForegroundColor White
Write-Host "4. Open Copilot Chat (Ctrl+Shift+I)" -ForegroundColor White
Write-Host "5. Enable Agent Mode and start using the MCP server!" -ForegroundColor White

Write-Host "`nTo test the server:" -ForegroundColor Cyan
if ($Docker) {
    Write-Host ".\test-mcp-server.ps1 -Docker" -ForegroundColor White
} else {
    Write-Host ".\test-mcp-server.ps1 -Local -Port $Port" -ForegroundColor White
}

Write-Host "`nReady to use!" -ForegroundColor Green

# Keep the script running if it's a local server
if (-not $Docker) {
    Write-Host "`nPress Ctrl+C to stop the server..." -ForegroundColor Yellow
    try {
        Wait-Job $serverJob
    } catch {
        Write-Host "`nServer stopped." -ForegroundColor Yellow
    } finally {
        Stop-Job $serverJob -ErrorAction SilentlyContinue
        Remove-Job $serverJob -ErrorAction SilentlyContinue
    }
}