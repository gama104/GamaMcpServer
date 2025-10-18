// Taxpayer MCP Server - HTTP Transport for Docker Containers
// ✅ WITH JWT AUTHENTICATION & USER-SCOPED DATA ACCESS

using ProtectedMcpServer.Tools;
using ProtectedMcpServer.Auth;
using ProtectedMcpServer.Services;
using ProtectedMcpServer.Data;
using ProtectedMcpServer.Handlers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add HttpContextAccessor (required for UserContext)
builder.Services.AddHttpContextAccessor();

// Add JWT Authentication Service
builder.Services.AddSingleton<JwtService>();

// Add User Context Service (extracts user from JWT)
builder.Services.AddScoped<IUserContext, UserContextService>();

// Add Data Store (user-scoped data access)
builder.Services.AddScoped<IDataStore, JsonDataStore>();

// Add Tax Resource Store (MCP resources - public reference data)
builder.Services.AddSingleton<ITaxResourceStore, TaxResourceStore>();

// Add Resource Handler (handles resources/list and resources/read)
builder.Services.AddScoped<ResourceHandler>();

// Add Taxpayer Tools (MCP tools)
builder.Services.AddScoped<TaxpayerTools>();

// Add CORS with restricted origins (SECURITY: Prevent unauthorized cross-origin access)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Security:CORS:AllowedOrigins")
            .Get<string[]>() ?? new[] { "https://vscode.dev", "https://github.dev" };
        
        var allowCredentials = builder.Configuration.GetValue<bool>("Security:CORS:AllowCredentials", true);
        
        var corsPolicy = policy
            .WithOrigins(allowedOrigins)
            .WithMethods("POST", "GET", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type");
        
        if (allowCredentials)
        {
            corsPolicy.AllowCredentials();
        }
    });
});

var app = builder.Build();

// HTTPS enforcement for production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    
    // HSTS for production
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    context.Response.Headers.Remove("Server");
    await next();
});

app.UseCors();

const string MCP_VERSION = "2025-03-26";
var SUPPORTED_VERSIONS = new[] { "2024-11-05", "2025-03-26" };

// Health endpoint
app.MapGet("/", () => Results.Ok(new
{
    status = "ok",
    name = "taxpayer-mcp-server",
    version = "1.0.0",
    protocol = "MCP",
    protocolVersion = MCP_VERSION,
    supportedVersions = SUPPORTED_VERSIONS,
    endpoint = "/mcp",
    tools = new[] { 
        "GetTaxpayerProfile", 
        "GetTaxReturns",
        "GetTaxReturnByYear",
        "GetDeductionsByYear",
        "GetDeductionsByCategory",
        "CalculateDeductionTotals",
        "CompareDeductionsYearly",
        "GetDocumentsByType",
        "GetDocumentsByYear"
    },
    timestamp = DateTime.UtcNow.ToString("O"),
    environment = app.Environment.EnvironmentName
}));

// MCP endpoint
app.MapPost("/mcp", async (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var taxpayerTools = context.RequestServices.GetRequiredService<TaxpayerTools>();
    var jwtService = context.RequestServices.GetRequiredService<JwtService>();
    
    try
    {
        // ✅ JWT Authentication Required
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            logger.LogWarning("Unauthorized access attempt - no Authorization header from IP: {IP}", 
                context.Connection.RemoteIpAddress);
            return Results.Json(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32001,
                    message = "Authentication required. Include 'Authorization: Bearer <token>' header."
                }
            }, statusCode: 401);
        }

        var token = authHeader.ToString();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring("Bearer ".Length).Trim();
        }

        var user = jwtService.ValidateToken(token);
        if (user == null)
        {
            logger.LogWarning("Unauthorized access attempt - invalid token from IP: {IP}", 
                context.Connection.RemoteIpAddress);
            return Results.Json(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32002,
                    message = "Invalid authentication token"
                }
            }, statusCode: 401);
        }

        // SECURITY: Set the authenticated user claims on HttpContext for UserContextService
        var claimsList = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
            new(System.Security.Claims.ClaimTypes.Name, user.Id),
            new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claimsList, "JWT");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        logger.LogInformation("User authenticated: {UserId} ({Role}) from IP: {IP}", 
            user.Id, user.Role, context.Connection.RemoteIpAddress);
        
        var requestBody = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body);
        
        if (!requestBody.TryGetProperty("method", out var method))
        {
            return Results.BadRequest(new { error = "Method is required" });
        }

        var methodName = method.GetString();
        var requestId = requestBody.TryGetProperty("id", out var reqId) ? reqId : (JsonElement?)null;
        
        logger.LogInformation("MCP method: {Method}", methodName);

        // Handle notifications (no response expected)
        if (methodName?.StartsWith("notifications/") == true)
        {
            logger.LogInformation("Received notification: {Method}", methodName);
            return Results.Ok(); // Acknowledge notification
        }

        switch (methodName)
        {
            case "initialize":
                return Results.Ok(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = new
                    {
                        protocolVersion = MCP_VERSION,
                        capabilities = new 
                        { 
                            tools = new { },
                            resources = new { subscribe = false, listChanged = false }
                        },
                        serverInfo = new { name = "taxpayer-mcp-server", version = "1.0.0" }
                    }
                });

            case "tools/list":
                var tools = new object[]
                {
                    new
                    {
                        name = "GetTaxpayerProfile",
                        description = "Get the taxpayer's profile information including name, contact details, and filing status.",
                        inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                    },
                    new
                    {
                        name = "GetTaxReturns",
                        description = "Get all tax returns for the taxpayer.",
                        inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                    },
                    new
                    {
                        name = "GetTaxReturnByYear",
                        description = "Get tax return for a specific year.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["year"] = new { type = "number", description = "Tax year (e.g., 2023, 2024)" }
                            },
                            required = new[] { "year" }
                        }
                    },
                    new
                    {
                        name = "GetDeductionsByYear",
                        description = "Get all deductions for a specific tax year.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["year"] = new { type = "number", description = "Tax year (e.g., 2023, 2024)" }
                            },
                            required = new[] { "year" }
                        }
                    },
                    new
                    {
                        name = "GetDeductionsByCategory",
                        description = "Get deductions by category across all years. Categories: MedicalExpenses, CharitableDonations, MortgageInterest, PropertyTaxes, BusinessExpenses, EducationExpenses, StateLocalTaxes, Other",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["category"] = new { type = "string", description = "Deduction category name" }
                            },
                            required = new[] { "category" }
                        }
                    },
                    new
                    {
                        name = "CalculateDeductionTotals",
                        description = "Calculate total deductions by category for a specific year.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["year"] = new { type = "number", description = "Tax year (e.g., 2023, 2024)" }
                            },
                            required = new[] { "year" }
                        }
                    },
                    new
                    {
                        name = "CompareDeductionsYearly",
                        description = "Compare deductions between two tax years to see changes.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["year1"] = new { type = "number", description = "First year to compare" },
                                ["year2"] = new { type = "number", description = "Second year to compare" }
                            },
                            required = new[] { "year1", "year2" }
                        }
                    },
                    new
                    {
                        name = "GetDocumentsByType",
                        description = "Get documents by type. Types: W2, Form1099, Receipt, Invoice, BankStatement, MortgageStatement, DonationReceipt, MedicalBill, PropertyTaxBill, Other",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["documentType"] = new { type = "string", description = "Document type name" }
                            },
                            required = new[] { "documentType" }
                        }
                    },
                    new
                    {
                        name = "GetDocumentsByYear",
                        description = "Get all documents for a specific tax year.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["year"] = new { type = "number", description = "Tax year" }
                            },
                            required = new[] { "year" }
                        }
                    }
                };

                return Results.Ok(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = new { tools }
                });

            case "resources/list":
                var resourceHandler = context.RequestServices.GetRequiredService<ResourceHandler>();
                return await resourceHandler.HandleResourcesList(requestId);

            case "resources/read":
                if (!requestBody.TryGetProperty("params", out var paramsElementRead) ||
                    !paramsElementRead.TryGetProperty("uri", out var uriElement))
                {
                    return Results.BadRequest(new { error = "URI parameter is required" });
                }

                var resourceHandlerRead = context.RequestServices.GetRequiredService<ResourceHandler>();
                return await resourceHandlerRead.HandleResourceRead(uriElement.GetString(), requestId);

            case "tools/call":
                if (!requestBody.TryGetProperty("params", out var paramsElement))
                {
                    return Results.BadRequest(new { error = "Parameters required" });
                }

                var toolName = paramsElement.GetProperty("name").GetString();
                var args = paramsElement.TryGetProperty("arguments", out var argsElement) 
                    ? argsElement 
                    : new JsonElement();

                logger.LogInformation("Calling tool: {Tool}", toolName);

                string result;
                try
                {
                    // SECURITY: All tools use UserContext to filter data by authenticated user
                    result = toolName switch
                    {
                        "GetTaxpayerProfile" => await taxpayerTools.GetTaxpayerProfile(),
                        "GetTaxReturns" => await taxpayerTools.GetTaxReturns(),
                        "GetTaxReturnByYear" => await taxpayerTools.GetTaxReturnByYear(
                            args.GetProperty("year").GetInt32()),
                        "GetDeductionsByYear" => await taxpayerTools.GetDeductionsByYear(
                            args.GetProperty("year").GetInt32()),
                        "GetDeductionsByCategory" => await taxpayerTools.GetDeductionsByCategory(
                            args.GetProperty("category").GetString()!),
                        "CalculateDeductionTotals" => await taxpayerTools.CalculateDeductionTotals(
                            args.GetProperty("year").GetInt32()),
                        "CompareDeductionsYearly" => await taxpayerTools.CompareDeductionsYearly(
                            args.GetProperty("year1").GetInt32(),
                            args.GetProperty("year2").GetInt32()),
                        "GetDocumentsByType" => await taxpayerTools.GetDocumentsByType(
                            args.GetProperty("documentType").GetString()!),
                        "GetDocumentsByYear" => await taxpayerTools.GetDocumentsByYear(
                            args.GetProperty("year").GetInt32()),
                        _ => throw new Exception($"Unknown tool: {toolName}")
                    };

                    logger.LogInformation("Tool {Tool} executed successfully by user {UserId}", toolName, context.User.Identity?.Name ?? "unknown");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing tool: {Tool}", toolName);
                    return Results.Json(new
                    {
                        jsonrpc = "2.0",
                        id = requestId,
                        error = new { code = -32603, message = $"Tool execution error: {ex.Message}" }
                    }, statusCode: 500);
                }

                return Results.Ok(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = new
                    {
                        content = new object[]
                        {
                            new { type = "text", text = result }
                        }
                    }
                });

            default:
                return Results.Json(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    error = new { code = -32601, message = $"Method not found: {methodName}" }
                }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling MCP request");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "7071";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Logger.LogInformation("=================================");
app.Logger.LogInformation("Taxpayer MCP Server (HTTP Transport)");
app.Logger.LogInformation("Protocol: MCP {Version}", MCP_VERSION);
app.Logger.LogInformation("MCP endpoint: http://localhost:{Port}/mcp", port);
app.Logger.LogInformation("Health check: http://localhost:{Port}/", port);
app.Logger.LogInformation("Tools: 9 taxpayer tools with user-scoped data access");
app.Logger.LogInformation("Security: JWT authentication with per-user data isolation");
app.Logger.LogInformation("Security: OAuth 2.1 (audience/issuer validation) + Restricted CORS");
app.Logger.LogInformation("=================================");

app.Run();
