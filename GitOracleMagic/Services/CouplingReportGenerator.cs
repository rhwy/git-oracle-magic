// Services/CouplingReportGenerator.cs

using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GitOracleMagic.Services
{
    public class CouplingReportGenerator : ICouplingReportGenerator
    {
        private readonly ILogger<CouplingReportGenerator> _logger;

        public CouplingReportGenerator(ILogger<CouplingReportGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateReport(ChangeCouplingResult result, int topCouples)
        {
            _logger.LogInformation("Generating change coupling report for top {TopCouples} coupled files", topCouples);

            // Create a beautiful header
            var rule = new Rule($"[yellow]Change Coupling Analysis Report[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Repository information panel
            var sinceInfo = result.SinceDate.HasValue 
                ? $"Since: {result.SinceDate.Value:yyyy-MM-dd}" 
                : "All time";

            var repoPanel = new Panel($"""
                [bold]Repository:[/] {EscapeMarkup(result.RepositoryPath)}
                [bold]Analysis Date:[/] {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}
                [bold]Analysis Period:[/] {sinceInfo}
                [bold]Commits Analyzed:[/] {result.TotalCommitsAnalyzed:N0}
                [bold]Files Analyzed:[/] {result.TotalFilesAnalyzed:N0}
                [bold]Min Coupling Strength:[/] {result.MinimumCouplingStrength:P1}
                """)
                .Header(" Analysis Information ")
                .BorderColor(Color.Blue)
                .RoundedBorder();

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();

            if (!result.CoupledFiles.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No file couples found above {result.MinimumCouplingStrength:P1} coupling strength.[/]");
                AnsiConsole.MarkupLine("[dim]Try lowering the --min-strength parameter or analyzing a longer time period.[/]");
                return;
            }

            // Create a table for the coupling results
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Rank[/]").Centered())
                .AddColumn(new TableColumn("[yellow]File 1[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]File 2[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]Coupled Changes[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Coupling Strength[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Last Shared Change[/]").Centered());

            int rank = 1;
            foreach (var coupling in result.CoupledFiles)
            {
                // Color code the coupling strength
                var strengthColor = coupling.CouplingStrength switch
                {
                    >= 0.8 => "red",      // Very high coupling
                    >= 0.6 => "orange1",  // High coupling
                    >= 0.4 => "yellow",   // Medium coupling
                    >= 0.2 => "green",    // Low coupling
                    _ => "dim"            // Very low coupling
                };

                // Color code the coupling count
                var countColor = coupling.CouplingCount switch
                {
                    >= 20 => "red",
                    >= 10 => "orange1",
                    >= 5 => "yellow",
                    _ => "green"
                };

                var lastSharedDate = coupling.LastSharedCommit == DateTime.MinValue 
                    ? "N/A" 
                    : coupling.LastSharedCommit.ToString("yyyy-MM-dd");

                table.AddRow(
                    $"[bold]{rank}[/]",
                    $"[blue]{EscapeMarkup(TruncatePath(coupling.FilePath1, 40))}[/]",
                    $"[blue]{EscapeMarkup(TruncatePath(coupling.FilePath2, 40))}[/]",
                    $"[{countColor}]{coupling.CouplingCount}[/]",
                    $"[{strengthColor}]{coupling.CouplingStrength:P1}[/]",
                    $"[dim]{lastSharedDate}[/]"
                );

                rank++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Summary statistics
            var totalCouplings = result.CoupledFiles.Count;
            var avgCouplingStrength = result.CoupledFiles.Any() 
                ? result.CoupledFiles.Average(c => c.CouplingStrength) 
                : 0;
            var strongestCouple = result.CoupledFiles.FirstOrDefault();

            var summaryPanel = new Panel($"""
                [bold]Total Coupled Pairs Found:[/] [green]{totalCouplings}[/]
                [bold]Average Coupling Strength:[/] [blue]{avgCouplingStrength:P1}[/]
                [bold]Strongest Couple:[/] [yellow]{EscapeMarkup(strongestCouple?.FilePath1 ?? "N/A")}[/] ↔ [yellow]{EscapeMarkup(TruncatePath(strongestCouple?.FilePath2 ?? "N/A", 30))}[/]
                [bold]Strongest Coupling Strength:[/] [red]{strongestCouple?.CouplingStrength:P1}[/] ([green]{strongestCouple?.CouplingCount ?? 0}[/] shared changes)
                """)
                .Header(" Summary Statistics ")
                .BorderColor(Color.Green)
                .RoundedBorder();

            AnsiConsole.Write(summaryPanel);

            // Interpretation help
            var helpPanel = new Panel($"""
                [bold]Interpretation Guide:[/]
                • [bold]Coupling Strength Calculation:[/] Shared changes ÷ Min(File1 changes, File2 changes)
                  [dim]Example: File A (100 changes) + File B (20 changes) + 15 shared = 15÷20 = 75% coupling[/]
                • [red]High coupling (>60%)[/]: Files that almost always change together - consider refactoring
                • [yellow]Medium coupling (20-60%)[/]: Files with related functionality - monitor for architectural decisions
                • [green]Low coupling (<20%)[/]: Occasional co-changes - likely due to feature development
                • [bold]Why high coupling + low changes?[/] One file rarely changes, but when it does, it usually involves the other file
                """)
                .Header(" How to Read This Report ")
                .BorderColor(Color.Grey)
                .RoundedBorder();

            AnsiConsole.Write(helpPanel);

            _logger.LogInformation("Change coupling report generation completed");
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (path.Length <= maxLength)
                return path;

            var parts = path.Split('/', '\\');
            if (parts.Length <= 2)
                return path.Length > maxLength ? "..." + path.Substring(path.Length - maxLength + 3) : path;

            // Try to keep the filename and some parent directories
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

        // Escape markup characters that might be in file paths
        private static string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }
    }
}