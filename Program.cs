// Taxpayer MCP Server - HTTP Transport for Docker Containers
// ✅ WITH JWT AUTHENTICATION & USER-SCOPED DATA ACCESS

using ProtectedMcpServer.Tools;
using ProtectedMcpServer.Auth;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Application.Services;
using ProtectedMcpServer.Data;
using ProtectedMcpServer.Handlers;
using Microsoft.EntityFrameworkCore;
using MediatR;
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

// Add Entity Framework Core with In-Memory Database
builder.Services.AddDbContext<TaxpayerDbContext>(options =>
{
    options.UseInMemoryDatabase("TaxpayerDemoDb");
});

// Add Application DbContext interface
builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<TaxpayerDbContext>());

// Add MediatR for CQRS pattern
builder.Services.AddMediatR(typeof(Program).Assembly);

// Add Data Store (user-scoped data access) - Now using CQRS pattern
builder.Services.AddScoped<IDataStore, TaxpayerDataRepository>();

// Add Tax Resource Store (MCP resources - public reference data)
builder.Services.AddSingleton<ITaxResourceStore, TaxReferenceDataRepository>();

// Add Resource Handler (handles resources/list and resources/read)
builder.Services.AddScoped<ResourceHandler>();

// Add Taxpayer Tools (MCP tools)
builder.Services.AddScoped<TaxpayerTools>();

// Add Prompt Handler (MCP prompts - conversation templates)
builder.Services.AddScoped<PromptHandler>();

// Add Health Checks for monitoring
builder.Services.AddHealthChecks()
    .AddCheck("jwt-service", () => 
    {
        // Simple health check - in production, you'd check actual JWT service health
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("JWT service is available");
    });

// Add Rate Limiting for API protection
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
});

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

// Seed data from JSON file or create sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var jsonPath = Path.Combine(app.Environment.ContentRootPath, "Data", "taxpayer-data.json");
        if (File.Exists(jsonPath))
        {
            logger.LogInformation("Seeding database from JSON file: {Path}", jsonPath);
            await TaxpayerDataSeeder.SeedFromJsonAsync(context, jsonPath);
        }
        else
        {
            logger.LogInformation("JSON file not found, creating sample data");
            await TaxpayerDataSeeder.CreateSampleDataAsync(context);
        }
        
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding database");
        throw;
    }
}

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

// Add Rate Limiting middleware
app.UseRateLimiter();

// Add Health Checks endpoint
app.MapHealthChecks("/health");

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
    prompts = new[] {
        "GetPersonalizedTaxAdvice",
        "CompareDeductionOptions", 
        "GetTaxOptimizationAdvice"
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
                            resources = new { subscribe = false, listChanged = false },
                            prompts = new { listChanged = false }
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

            case "prompts/list":
                var promptHandler = context.RequestServices.GetRequiredService<PromptHandler>();
                var promptsResult = await promptHandler.ListPromptsAsync();
                return Results.Ok(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = promptsResult
                });

            case "prompts/get":
                if (!requestBody.TryGetProperty("params", out var paramsElementPrompt) ||
                    !paramsElementPrompt.TryGetProperty("name", out var nameElement))
                {
                    return Results.BadRequest(new { error = "Name parameter is required" });
                }

                var promptName = nameElement.GetString() ?? throw new ArgumentException("Prompt name cannot be null");
                var promptArguments = new Dictionary<string, object>();
                
                if (paramsElementPrompt.TryGetProperty("arguments", out var argsElementPrompt))
                {
                    foreach (var prop in argsElementPrompt.EnumerateObject())
                    {
                        promptArguments[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString()!,
                            JsonValueKind.Number => prop.Value.GetInt32(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => prop.Value.ToString()
                        };
                    }
                }

                var promptHandlerGet = context.RequestServices.GetRequiredService<PromptHandler>();
                var promptResult = await promptHandlerGet.GetPromptAsync(promptName, promptArguments);
                return Results.Ok(new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    result = promptResult
                });

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
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrEmpty(urls))
{
    app.Urls.Add($"http://127.0.0.1:{port}");
}

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
