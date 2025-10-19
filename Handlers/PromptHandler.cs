using System.Text.Json;
using ProtectedMcpServer.Application.Interfaces;
using ProtectedMcpServer.Models;

namespace ProtectedMcpServer.Handlers;

/// <summary>
/// MCP Prompts Handler - Implements prompts/list and prompts/get according to MCP specification
/// Prompts are conversation templates that guide AI assistants, not executable functions
/// </summary>
public class PromptHandler
{
    private readonly ILogger<PromptHandler> _logger;

    public PromptHandler(ILogger<PromptHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get list of available prompt templates
    /// MCP Specification: prompts/list
    /// </summary>
    public Task<object> ListPromptsAsync()
    {
        _logger.LogInformation("Listing available prompt templates");

        var prompts = new[]
        {
            new
            {
                name = "GetPersonalizedTaxAdvice",
                description = "Template for providing personalized tax advice based on user's financial situation and tax history",
                arguments = new[]
                {
                    new
                    {
                        name = "situation",
                        description = "User's current financial situation or specific tax question",
                        required = true
                    },
                    new
                    {
                        name = "year",
                        description = "Optional: Specific tax year to analyze (defaults to current year)",
                        required = false
                    }
                }
            },
            new
            {
                name = "CompareDeductionOptions",
                description = "Template for comparing itemized vs standard deduction to help users make the best choice",
                arguments = new[]
                {
                    new
                    {
                        name = "year",
                        description = "Tax year to analyze (defaults to current year)",
                        required = false
                    }
                }
            },
            new
            {
                name = "GetTaxOptimizationAdvice",
                description = "Template for providing year-over-year tax analysis and optimization recommendations",
                arguments = new[]
                {
                    new
                    {
                        name = "yearsToAnalyze",
                        description = "Number of years to analyze (defaults to 2)",
                        required = false
                    }
                }
            }
        };

        return Task.FromResult<object>(new
        {
            prompts = prompts
        });
    }

    /// <summary>
    /// Get a specific prompt template with arguments
    /// MCP Specification: prompts/get
    /// </summary>
    public Task<object> GetPromptAsync(string name, Dictionary<string, object>? arguments = null)
    {
        _logger.LogInformation("Getting prompt template: {Name}", name);

        return Task.FromResult<object>(name switch
        {
            "GetPersonalizedTaxAdvice" => new
            {
                name = "GetPersonalizedTaxAdvice",
                description = "Template for providing personalized tax advice based on user's financial situation and tax history",
                arguments = new[]
                {
                    new
                    {
                        name = "situation",
                        description = "User's current financial situation or specific tax question",
                        required = true
                    },
                    new
                    {
                        name = "year",
                        description = "Optional: Specific tax year to analyze (defaults to current year)",
                        required = false
                    }
                },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new
                        {
                            type = "text",
                            text = GeneratePersonalizedTaxAdvicePrompt(arguments)
                        }
                    }
                }
            },
            "CompareDeductionOptions" => new
            {
                name = "CompareDeductionOptions",
                description = "Template for comparing itemized vs standard deduction to help users make the best choice",
                arguments = new[]
                {
                    new
                    {
                        name = "year",
                        description = "Tax year to analyze (defaults to current year)",
                        required = false
                    }
                },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new
                        {
                            type = "text",
                            text = GenerateDeductionComparisonPrompt(arguments)
                        }
                    }
                }
            },
            "GetTaxOptimizationAdvice" => new
            {
                name = "GetTaxOptimizationAdvice",
                description = "Template for providing year-over-year tax analysis and optimization recommendations",
                arguments = new[]
                {
                    new
                    {
                        name = "yearsToAnalyze",
                        description = "Number of years to analyze (defaults to 2)",
                        required = false
                    }
                },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new
                        {
                            type = "text",
                            text = GenerateTaxOptimizationPrompt(arguments)
                        }
                    }
                }
            },
            _ => throw new ArgumentException($"Unknown prompt: {name}")
        });
    }

    private string GeneratePersonalizedTaxAdvicePrompt(Dictionary<string, object>? arguments)
    {
        var situation = arguments?.GetValueOrDefault("situation", "general tax advice")?.ToString() ?? "general tax advice";
        var year = arguments?.GetValueOrDefault("year", DateTime.Now.Year)?.ToString() ?? DateTime.Now.Year.ToString();

        return $@"I need personalized tax advice for the following situation: {situation}

Please analyze my tax situation for {year} and provide comprehensive guidance including:

1. **Deduction Strategy**: Should I itemize or take the standard deduction?
2. **Tax Planning**: What opportunities exist for tax optimization?
3. **Compliance**: What should I be aware of for this tax year?
4. **Future Planning**: What steps should I take for next year?

Please use my actual tax data (tax returns, deductions, documents) to provide specific, actionable advice tailored to my situation.";
    }

    private string GenerateDeductionComparisonPrompt(Dictionary<string, object>? arguments)
    {
        var year = arguments?.GetValueOrDefault("year", DateTime.Now.Year)?.ToString() ?? DateTime.Now.Year.ToString();

        return $@"I need help deciding between itemized and standard deductions for {year}.

Please analyze my deduction data and provide a detailed comparison including:

1. **Current Deductions**: Show me all my itemized deductions for {year}
2. **Standard vs Itemized**: Calculate both options and show the difference
3. **Recommendation**: Which option saves me more money and why?
4. **Strategy**: If I'm close to the threshold, suggest timing strategies
5. **Documentation**: What records should I keep for my choice?

Use my actual deduction data to provide specific calculations and recommendations.";
    }

    private string GenerateTaxOptimizationPrompt(Dictionary<string, object>? arguments)
    {
        var yearsToAnalyze = arguments?.GetValueOrDefault("yearsToAnalyze", 2)?.ToString() ?? "2";
        var currentYear = DateTime.Now.Year;
        var year1 = currentYear - 1;
        var year2 = currentYear;

        return $@"I need tax optimization advice based on my historical data.

Please analyze my tax situation over the last {yearsToAnalyze} years ({year1}-{year2}) and provide optimization recommendations including:

1. **Year-over-Year Analysis**: Compare my deductions, income, and tax liability
2. **Trends**: Identify patterns and changes in my tax situation
3. **Opportunities**: What new deduction categories or strategies should I consider?
4. **Timing**: When should I make certain payments or decisions?
5. **Future Planning**: What should I do differently next year?

Use my actual tax data to provide specific, data-driven recommendations for tax optimization.";
    }
}
