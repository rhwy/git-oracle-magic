// Services/CodeMetricsReportGenerator.cs
using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GitOracleMagic.Services
{
    public class CodeMetricsReportGenerator : ICodeMetricsReportGenerator
    {
        private readonly ILogger<CodeMetricsReportGenerator> _logger;

        public CodeMetricsReportGenerator(ILogger<CodeMetricsReportGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateReport(CodeMetricsResult result, int topFiles)
        {
            _logger.LogInformation("Generating code metrics report for {FileCount} files", result.FileMetrics.Count);

            // Create a beautiful header
            var rule = new Rule($"[yellow]Code Metrics Analysis Report ({result.AnalyzerTool})[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Repository information panel
            var repoPanel = new Panel($"""
                [bold]Repository:[/] {EscapeMarkup(result.RepositoryPath)}
                [bold]Analysis Date:[/] {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}
                [bold]Analyzer:[/] {result.AnalyzerTool} v{result.AnalyzerVersion}
                [bold]Files Analyzed:[/] {result.Summary.TotalFiles:N0}
                [bold]Functions Analyzed:[/] {result.Summary.TotalFunctions:N0}
                [bold]Total Lines of Code:[/] {result.Summary.TotalLinesOfCode:N0}
                """)
                .Header(" Analysis Summary ")
                .BorderColor(Color.Blue)
                .RoundedBorder();

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();

            if (!result.FileMetrics.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files with metrics found.[/]");
                return;
            }

            // Summary statistics
            DisplaySummaryStatistics(result.Summary);

            // Top complex files table
            DisplayTopComplexFiles(result.FileMetrics, topFiles);

            // Most complex functions
            DisplayMostComplexFunctions(result.FileMetrics);

            _logger.LogInformation("Code metrics report generation completed");
        }

        private static void DisplaySummaryStatistics(CodeMetricsSummary summary)
        {
            var summaryRule = new Rule("[yellow]Complexity Overview[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(summaryRule);
            AnsiConsole.WriteLine();

            // Complexity distribution
            var complexityTable = new Table()
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Complexity Level[/]").Centered())
                .AddColumn(new TableColumn("[yellow]File Count[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Percentage[/]").RightAligned());

            foreach (var kvp in summary.ComplexityDistribution.OrderBy(x => GetComplexityOrder(x.Key)))
            {
                var percentage = summary.TotalFiles > 0 ? (double)kvp.Value / summary.TotalFiles * 100 : 0;
                var levelColor = GetComplexityColor(kvp.Key);
                
                complexityTable.AddRow(
                    $"[{levelColor}]{kvp.Key}[/]",
                    $"[{levelColor}]{kvp.Value:N0}[/]",
                    $"[{levelColor}]{percentage:F1}%[/]"
                );
            }

            AnsiConsole.Write(complexityTable);
            AnsiConsole.WriteLine();

            // Key metrics panel
            var keyMetricsPanel = new Panel($"""
                [bold]Average Complexity:[/] [blue]{summary.AverageCyclomaticComplexity:F1}[/]
                [bold]Maximum Complexity:[/] [red]{summary.MaxCyclomaticComplexity}[/]
                [bold]Most Complex File:[/] [yellow]{EscapeMarkup(summary.MostComplexFile)}[/]
                [bold]Most Complex Function:[/] [yellow]{EscapeMarkup(summary.MostComplexFunction)}[/]
                """)
                .Header(" Key Metrics ")
                .BorderColor(Color.Green)
                .RoundedBorder();

            AnsiConsole.Write(keyMetricsPanel);
            AnsiConsole.WriteLine();
        }

        private static void DisplayTopComplexFiles(List<FileCodeMetrics> fileMetrics, int topFiles)
        {
            var filesRule = new Rule($"[yellow]Top {topFiles} Most Complex Files[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(filesRule);
            AnsiConsole.WriteLine();

            var filesTable = new Table()
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Rank[/]").Centered())
                .AddColumn(new TableColumn("[yellow]File Path[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]Language[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Complexity[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Functions[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Lines of Code[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Avg Complexity[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Rating[/]").Centered());

            int rank = 1;
            foreach (var file in fileMetrics.Take(topFiles))
            {
                var complexityColor = GetComplexityColor(file.ComplexityRating);
                var filePath = TruncatePath(file.FilePath, 50);

                filesTable.AddRow(
                    $"[bold]{rank}[/]",
                    $"[blue]{EscapeMarkup(filePath)}[/]",
                    $"[dim]{file.Language}[/]",
                    $"[{complexityColor}]{file.CyclomaticComplexity}[/]",
                    $"[dim]{file.FunctionCount}[/]",
                    $"[dim]{file.LinesOfCode}[/]",
                    $"[blue]{file.AverageComplexityPerFunction:F1}[/]",
                    $"[{complexityColor}]{file.ComplexityRating}[/]"
                );

                rank++;
            }

            AnsiConsole.Write(filesTable);
            AnsiConsole.WriteLine();
        }

        private static void DisplayMostComplexFunctions(List<FileCodeMetrics> fileMetrics)
        {
            var allFunctions = fileMetrics
                .SelectMany(f => f.Functions.Select(func => new { File = f.FilePath, Function = func }))
                .OrderByDescending(x => x.Function.CyclomaticComplexity)
                .Take(10)
                .ToList();

            if (!allFunctions.Any()) return;

            var functionsRule = new Rule("[yellow]Top 10 Most Complex Functions[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(functionsRule);
            AnsiConsole.WriteLine();

            var functionsTable = new Table()
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Rank[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Function Name[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]File[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]Complexity[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Lines[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Parameters[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Rating[/]").Centered());

            int rank = 1;
            foreach (var item in allFunctions)
            {
                var complexityColor = GetComplexityColor(item.Function.ComplexityRating);
                var fileName = Path.GetFileName(item.File);

                functionsTable.AddRow(
                    $"[bold]{rank}[/]",
                    $"[blue]{EscapeMarkup(item.Function.Name)}[/]",
                    $"[dim]{EscapeMarkup(fileName)}[/]",
                    $"[{complexityColor}]{item.Function.CyclomaticComplexity}[/]",
                    $"[dim]{item.Function.LinesOfCode}[/]",
                    $"[dim]{item.Function.ParameterCount}[/]",
                    $"[{complexityColor}]{item.Function.ComplexityRating}[/]"
                );

                rank++;
            }

            AnsiConsole.Write(functionsTable);
            AnsiConsole.WriteLine();
        }

        private static string GetComplexityColor(string rating)
        {
            return rating switch
            {
                "Low" => "green",
                "Medium" => "yellow",
                "High" => "orange1",
                "Very High" => "red",
                _ => "dim"
            };
        }

        private static int GetComplexityOrder(string rating)
        {
            return rating switch
            {
                "Low" => 1,
                "Medium" => 2,
                "High" => 3,
                "Very High" => 4,
                _ => 5
            };
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (path.Length <= maxLength)
                return path;

            var parts = path.Split('/', '\\');
            if (parts.Length <= 2)
                return path.Length > maxLength ? "..." + path.Substring(path.Length - maxLength + 3) : path;

            var filename = parts[^1];
            var result = filename;
            
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                var newResult = parts[i] + "/" + result;
                if (newResult.Length > maxLength - 3)
                    break;
                result = newResult;
            }

            return result.Length < path.Length ? "..." + result : result;
        }

        private static string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }
    }
}