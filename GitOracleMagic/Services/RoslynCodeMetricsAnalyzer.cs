// Services/RoslynCodeMetricsAnalyzer.cs
using GitOracleMagic.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace GitOracleMagic.Services
{
    public class RoslynCodeMetricsAnalyzer : ICodeMetricsAnalyzer
    {
        private readonly ILogger<RoslynCodeMetricsAnalyzer> _logger;

        public RoslynCodeMetricsAnalyzer(ILogger<RoslynCodeMetricsAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<CodeMetricsResult> AnalyzeCodeMetricsAsync(string repoPath, CodeMetricsConfiguration config)
        {
            var result = new CodeMetricsResult
            {
                RepositoryPath = repoPath,
                AnalyzerTool = "Roslyn",
                AnalyzerVersion = typeof(CSharpSyntaxTree).Assembly.GetName().Version?.ToString() ?? "Unknown"
            };

            try
            {
                _logger.LogInformation("Starting Roslyn code metrics analysis for {RepoPath}", repoPath);

                var fileMetrics = new List<FileCodeMetrics>();

                // Find all C# and VB.NET files
                var csharpFiles = Directory.GetFiles(repoPath, "*.cs", SearchOption.AllDirectories);
                var vbFiles = Directory.GetFiles(repoPath, "*.vb", SearchOption.AllDirectories);
                var allFiles = csharpFiles.Concat(vbFiles).ToList();

                // Filter out excluded patterns
                var filteredFiles = allFiles.Where(file => 
                    !config.ExcludePatterns.Any(pattern => 
                        file.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                _logger.LogInformation("Found {FileCount} .NET files to analyze", filteredFiles.Count);

                foreach (var filePath in filteredFiles)
                {
                    try
                    {
                        var fileMetric = await AnalyzeFileAsync(filePath, repoPath);
                        if (fileMetric != null)
                        {
                            fileMetrics.Add(fileMetric);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to analyze file {FilePath}: {Error}", filePath, ex.Message);
                    }
                }

                result.FileMetrics = fileMetrics.OrderByDescending(f => f.CyclomaticComplexity).ToList();
                result.Summary = GenerateSummary(result.FileMetrics);
                result.AnalysisSuccessful = true;

                _logger.LogInformation("Roslyn analysis completed. Analyzed {FileCount} files", fileMetrics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Roslyn analysis");
                result.AnalysisSuccessful = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<bool> IsAnalyzerAvailableAsync(CodeMetricsAnalyzer analyzer)
        {
            await Task.CompletedTask;
            // Roslyn is always available in .NET applications
            return analyzer == CodeMetricsAnalyzer.Lizard; // We'll reuse the Lizard enum for now
        }

        public async Task<string> GetAnalyzerVersionAsync(CodeMetricsAnalyzer analyzer)
        {
            await Task.CompletedTask;
            return typeof(CSharpSyntaxTree).Assembly.GetName().Version?.ToString() ?? "Unknown";
        }

        private async Task<FileCodeMetrics?> AnalyzeFileAsync(string filePath, string repoPath)
        {
            try
            {
                var sourceCode = await File.ReadAllTextAsync(filePath);
                var relativePath = Path.GetRelativePath(repoPath, filePath);
                var language = Path.GetExtension(filePath).ToLowerInvariant() == ".cs" ? "C#" : "VB.NET";

                if (language == "VB.NET")
                {
                    // For now, we'll skip VB.NET files as they need a different parser
                    // You could extend this to support VB.NET using Microsoft.CodeAnalysis.VisualBasic
                    return null;
                }

                var tree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = await tree.GetRootAsync();

                var fileMetric = new FileCodeMetrics
                {
                    FilePath = relativePath.Replace('\\', '/'), // Normalize path separators
                    Language = language,
                    LinesOfCode = CountLinesOfCode(sourceCode),
                    Functions = new List<FunctionMetrics>()
                };

                // Analyze methods
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var functionMetric = AnalyzeMethod(method, sourceCode);
                    fileMetric.Functions.Add(functionMetric);
                }

                // Analyze properties with getters/setters
                var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                foreach (var property in properties)
                {
                    var functionMetrics = AnalyzeProperty(property, sourceCode);
                    fileMetric.Functions.AddRange(functionMetrics);
                }

                // Analyze constructors
                var constructors = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
                foreach (var constructor in constructors)
                {
                    var functionMetric = AnalyzeConstructor(constructor, sourceCode);
                    fileMetric.Functions.Add(functionMetric);
                }

                // Calculate file-level metrics
                fileMetric.FunctionCount = fileMetric.Functions.Count;
                fileMetric.CyclomaticComplexity = fileMetric.Functions.Sum(f => f.CyclomaticComplexity);
                fileMetric.ParameterCount = fileMetric.Functions.Sum(f => f.ParameterCount);
                fileMetric.AverageComplexityPerFunction = fileMetric.Functions.Any() 
                    ? fileMetric.Functions.Average(f => f.CyclomaticComplexity) 
                    : 0;
                fileMetric.ComplexityRating = GetComplexityRating(fileMetric.CyclomaticComplexity);

                return fileMetric;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error analyzing file {FilePath}: {Error}", filePath, ex.Message);
                return null;
            }
        }

        private static FunctionMetrics AnalyzeMethod(MethodDeclarationSyntax method, string sourceCode)
        {
            var lines = sourceCode.Split('\n');
            var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

            return new FunctionMetrics
            {
                Name = method.Identifier.ValueText,
                StartLine = startLine,
                EndLine = endLine,
                LinesOfCode = endLine - startLine + 1,
                ParameterCount = method.ParameterList.Parameters.Count,
                CyclomaticComplexity = CalculateCyclomaticComplexity(method),
                ComplexityRating = GetComplexityRating(CalculateCyclomaticComplexity(method))
            };
        }

        private static List<FunctionMetrics> AnalyzeProperty(PropertyDeclarationSyntax property, string sourceCode)
        {
            var functions = new List<FunctionMetrics>();
            var lines = sourceCode.Split('\n');
            var startLine = property.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var endLine = property.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

            // Check if property has getter
            var getter = property.AccessorList?.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.GetKeyword));
            if (getter != null)
            {
                functions.Add(new FunctionMetrics
                {
                    Name = $"{property.Identifier.ValueText}.get",
                    StartLine = startLine,
                    EndLine = endLine,
                    LinesOfCode = endLine - startLine + 1,
                    ParameterCount = 0,
                    CyclomaticComplexity = CalculateCyclomaticComplexity(getter),
                    ComplexityRating = GetComplexityRating(CalculateCyclomaticComplexity(getter))
                });
            }

            // Check if property has setter
            var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.Keyword.IsKind(SyntaxKind.SetKeyword));
            if (setter != null)
            {
                functions.Add(new FunctionMetrics
                {
                    Name = $"{property.Identifier.ValueText}.set",
                    StartLine = startLine,
                    EndLine = endLine,
                    LinesOfCode = endLine - startLine + 1,
                    ParameterCount = 1, // setter has implicit 'value' parameter
                    CyclomaticComplexity = CalculateCyclomaticComplexity(setter),
                    ComplexityRating = GetComplexityRating(CalculateCyclomaticComplexity(setter))
                });
            }

            return functions;
        }

        private static FunctionMetrics AnalyzeConstructor(ConstructorDeclarationSyntax constructor, string sourceCode)
        {
            var lines = sourceCode.Split('\n');
            var startLine = constructor.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var endLine = constructor.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

            return new FunctionMetrics
            {
                Name = $"{constructor.Identifier.ValueText}(constructor)",
                StartLine = startLine,
                EndLine = endLine,
                LinesOfCode = endLine - startLine + 1,
                ParameterCount = constructor.ParameterList.Parameters.Count,
                CyclomaticComplexity = CalculateCyclomaticComplexity(constructor),
                ComplexityRating = GetComplexityRating(CalculateCyclomaticComplexity(constructor))
            };
        }

        private static int CalculateCyclomaticComplexity(SyntaxNode node)
        {
            // Start with base complexity of 1
            int complexity = 1;

            // Count decision points that increase cyclomatic complexity
            var descendants = node.DescendantNodes();

            // Conditional statements
            complexity += descendants.OfType<IfStatementSyntax>().Count();
            complexity += descendants.OfType<SwitchStatementSyntax>().Count();
            complexity += descendants.OfType<CaseSwitchLabelSyntax>().Count();

            // Loops
            complexity += descendants.OfType<WhileStatementSyntax>().Count();
            complexity += descendants.OfType<ForStatementSyntax>().Count();
            complexity += descendants.OfType<ForEachStatementSyntax>().Count();
            complexity += descendants.OfType<DoStatementSyntax>().Count();

            // Exception handling
            complexity += descendants.OfType<CatchClauseSyntax>().Count();

            // Conditional expressions (ternary operator)
            complexity += descendants.OfType<ConditionalExpressionSyntax>().Count();

            // Logical operators (&&, ||)
            var binaryExpressions = descendants.OfType<BinaryExpressionSyntax>();
            complexity += binaryExpressions.Count(expr => 
                expr.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
                expr.OperatorToken.IsKind(SyntaxKind.BarBarToken));

            return complexity;
        }

        private static int CountLinesOfCode(string sourceCode)
        {
            var lines = sourceCode.Split('\n');
            int linesOfCode = 0;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines and comment-only lines
                if (!string.IsNullOrEmpty(trimmedLine) && 
                    !trimmedLine.StartsWith("//") && 
                    !trimmedLine.StartsWith("/*") &&
                    !trimmedLine.StartsWith("*") &&
                    !trimmedLine.StartsWith("*/"))
                {
                    linesOfCode++;
                }
            }

            return linesOfCode;
        }

        private static string GetComplexityRating(int complexity)
        {
            return complexity switch
            {
                <= 10 => "Low",
                <= 20 => "Medium",
                <= 50 => "High",
                _ => "Very High"
            };
        }

        private static CodeMetricsSummary GenerateSummary(List<FileCodeMetrics> fileMetrics)
        {
            if (!fileMetrics.Any())
            {
                return new CodeMetricsSummary();
            }

            var allFunctions = fileMetrics.SelectMany(f => f.Functions).ToList();

            var summary = new CodeMetricsSummary
            {
                TotalFiles = fileMetrics.Count,
                TotalFunctions = allFunctions.Count,
                TotalLinesOfCode = fileMetrics.Sum(f => f.LinesOfCode),
                AverageCyclomaticComplexity = fileMetrics.Any() ? fileMetrics.Average(f => f.CyclomaticComplexity) : 0,
                MaxCyclomaticComplexity = fileMetrics.Any() ? fileMetrics.Max(f => f.CyclomaticComplexity) : 0,
                MostComplexFile = fileMetrics.OrderByDescending(f => f.CyclomaticComplexity).FirstOrDefault()?.FilePath ?? "",
                MostComplexFunction = allFunctions.OrderByDescending(f => f.CyclomaticComplexity).FirstOrDefault()?.Name ?? ""
            };

            // Complexity distribution
            summary.ComplexityDistribution = fileMetrics
                .GroupBy(f => f.ComplexityRating)
                .ToDictionary(g => g.Key, g => g.Count());

            // Language distribution
            summary.LanguageDistribution = fileMetrics
                .GroupBy(f => f.Language)
                .ToDictionary(g => g.Key, g => g.Count());

            return summary;
        }
    }
}