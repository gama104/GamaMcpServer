using System.Text.Json;
using ProtectedMcpServer.Application.Interfaces;

namespace ProtectedMcpServer.Handlers;

/// <summary>
/// Handler for MCP Resource endpoints (resources/list, resources/read)
/// Provides tax reference data (public knowledge, not user-specific)
/// </summary>
public class ResourceHandler
{
    private readonly ITaxResourceStore _resourceStore;
    private readonly ILogger<ResourceHandler> _logger;

    public ResourceHandler(ITaxResourceStore resourceStore, ILogger<ResourceHandler> logger)
    {
        _resourceStore = resourceStore;
        _logger = logger;
    }

    /// <summary>
    /// Handle resources/list - Returns all available tax resources
    /// </summary>
    public async Task<object> HandleResourcesList(JsonElement? requestId)
    {
        _logger.LogInformation("Handling resources/list request");
        
        var years = await _resourceStore.GetAvailableTaxYearsAsync();
        var resources = new List<object>();
        
        // Add resources for each available year
        foreach (var year in years)
        {
            resources.Add(new
            {
                uri = $"tax://rules/{year}",
                name = $"Tax Rules {year}",
                description = $"IRS tax rules, deduction limits, and eligibility criteria for {year}",
                mimeType = "application/json"
            });
            resources.Add(new
            {
                uri = $"tax://brackets/{year}",
                name = $"Tax Brackets {year}",
                description = $"Federal tax brackets and rates by filing status for {year}",
                mimeType = "application/json"
            });
            resources.Add(new
            {
                uri = $"tax://standard-deductions/{year}",
                name = $"Standard Deductions {year}",
                description = $"Standard deduction amounts by filing status for {year}",
                mimeType = "application/json"
            });
            resources.Add(new
            {
                uri = $"tax://deductions/{year}",
                name = $"Available Deductions {year}",
                description = $"Comprehensive list of available tax deductions for {year}",
                mimeType = "application/json"
            });
            resources.Add(new
            {
                uri = $"tax://limits/{year}",
                name = $"Deduction Limits {year}",
                description = $"AGI percentages, dollar caps, and phase-outs for {year}",
                mimeType = "application/json"
            });
        }
        
        // Add form instructions resources
        var forms = new[] { "1040", "Schedule A", "Schedule C" };
        foreach (var form in forms)
        {
            resources.Add(new
            {
                uri = $"tax://forms/{form}/instructions",
                name = $"Form {form} Instructions",
                description = $"Official instructions and guidance for Form {form}",
                mimeType = "application/json"
            });
        }
        
        _logger.LogInformation("Returning {Count} resources", resources.Count);
        
        return new
        {
            jsonrpc = "2.0",
            id = requestId,
            result = new { resources }
        };
    }

    /// <summary>
    /// Handle resources/read - Returns specific resource by URI
    /// </summary>
    public async Task<object> HandleResourceRead(string? uri, JsonElement? requestId)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return new
            {
                jsonrpc = "2.0",
                id = requestId,
                error = new { code = -32602, message = "URI parameter is required" }
            };
        }

        _logger.LogInformation("Reading resource: {URI}", uri);
        
        try
        {
            object? resourceData = null;
            
            // Parse URI and fetch appropriate resource
            if (uri.StartsWith("tax://rules/"))
            {
                var yearStr = uri.Split('/').Last();
                if (int.TryParse(yearStr, out var year))
                {
                    resourceData = await _resourceStore.GetTaxRulesAsync(year);
                }
            }
            else if (uri.StartsWith("tax://brackets/"))
            {
                var yearStr = uri.Split('/').Last();
                if (int.TryParse(yearStr, out var year))
                {
                    resourceData = await _resourceStore.GetTaxBracketsAsync(year);
                }
            }
            else if (uri.StartsWith("tax://standard-deductions/"))
            {
                var yearStr = uri.Split('/').Last();
                if (int.TryParse(yearStr, out var year))
                {
                    resourceData = await _resourceStore.GetStandardDeductionsAsync(year);
                }
            }
            else if (uri.StartsWith("tax://deductions/"))
            {
                var yearStr = uri.Split('/').Last();
                if (int.TryParse(yearStr, out var year))
                {
                    resourceData = await _resourceStore.GetAvailableDeductionsAsync(year);
                }
            }
            else if (uri.StartsWith("tax://limits/"))
            {
                var yearStr = uri.Split('/').Last();
                if (int.TryParse(yearStr, out var year))
                {
                    resourceData = await _resourceStore.GetDeductionLimitsAsync(year);
                }
            }
            else if (uri.StartsWith("tax://forms/"))
            {
                var parts = uri.Split('/');
                if (parts.Length >= 3)
                {
                    var form = parts[2];
                    resourceData = await _resourceStore.GetFormInstructionsAsync(form, DateTime.Now.Year);
                }
            }
            
            if (resourceData == null)
            {
                _logger.LogWarning("Resource not found: {URI}", uri);
                return new
                {
                    jsonrpc = "2.0",
                    id = requestId,
                    error = new { code = -32002, message = $"Resource not found: {uri}" }
                };
            }
            
            var jsonData = JsonSerializer.Serialize(resourceData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            _logger.LogInformation("Successfully read resource: {URI}", uri);
            
            return new
            {
                jsonrpc = "2.0",
                id = requestId,
                result = new
                {
                    contents = new[]
                    {
                        new { uri, mimeType = "application/json", text = jsonData }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource: {URI}", uri);
            return new
            {
                jsonrpc = "2.0",
                id = requestId,
                error = new { code = -32603, message = $"Resource read error: {ex.Message}" }
            };
        }
    }
}

