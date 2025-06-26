// Services/ConsoleReportGenerator.cs

using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GitOracleMagic.Services
{
    public class ConsoleReportGenerator : IReportGenerator
    {
        private readonly ILogger<ConsoleReportGenerator> _logger;

        public ConsoleReportGenerator(ILogger<ConsoleReportGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateReport(RepositoryAnalysisResult result, int topFiles)
        {
            _logger.LogInformation("Generating report for top {TopFiles} changed files", topFiles);

            // Create a beautiful header
            var rule = new Rule($"[yellow]Most Changed Files Report[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Repository information panel
            var repoPanel = new Panel($"""
                [bold]Repository:[/] {EscapeMarkup(result.RepositoryPath)}
                [bold]Analysis Date:[/] {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}
                [bold]Files Analyzed:[/] {result.Files.Count}
                """)
                .Header(" Repository Information ")
                .BorderColor(Color.Blue)
                .RoundedBorder();

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();

            if (!result.Files.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No files with changes found in the repository.[/]");
                return;
            }

            // Create a table for the results
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Rank[/]").Centered())
                .AddColumn(new TableColumn("[yellow]File Path[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]Changes[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]First Change[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Last Change[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Top Contributors[/]").LeftAligned());

            int rank = 1;
            foreach (var stats in result.Files)
            {
                // Prepare the contributors text - escape any markup characters
                var contributorsText = string.Join("\n", 
                    stats.TopCommitters.Select(c => $"[dim]{EscapeMarkup(c.Name)}[/] ([blue]{c.CommitCount}[/])"));

                // Color code the change count based on how many changes
                var changeCountColor = stats.ChangeCount switch
                {
                    > 100 => "red",
                    > 50 => "orange1",
                    > 20 => "yellow",
                    _ => "green"
                };

                table.AddRow(
                    $"[bold]{rank}[/]",
                    $"[blue]{EscapeMarkup(TruncatePath(stats.Path, 50))}[/]",
                    $"[{changeCountColor}]{stats.ChangeCount}[/]",
                    $"[dim]{stats.FirstChange.Date:yyyy-MM-dd}[/]\n[grey]{EscapeMarkup(stats.FirstChange.AuthorName)}[/]",
                    $"[dim]{stats.LastChange.Date:yyyy-MM-dd}[/]\n[grey]{EscapeMarkup(stats.LastChange.AuthorName)}[/]",
                    contributorsText
                );

                rank++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Summary statistics
            var totalChanges = result.Files.Sum(f => f.ChangeCount);
            var avgChanges = result.Files.Any() ? totalChanges / (double)result.Files.Count : 0;
            var mostActiveFile = result.Files.FirstOrDefault();

            var summaryPanel = new Panel($"""
                [bold]Total Changes:[/] [green]{totalChanges:N0}[/]
                [bold]Average Changes per File:[/] [green]{avgChanges:F1}[/]
                [bold]Most Active File:[/] [blue]{EscapeMarkup(mostActiveFile?.Path ?? "N/A")}[/] ([red]{mostActiveFile?.ChangeCount ?? 0}[/] changes)
                """)
                .Header(" Summary Statistics ")
                .BorderColor(Color.Green)
                .RoundedBorder();

            AnsiConsole.Write(summaryPanel);

            _logger.LogInformation("Report generation completed");
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

        // Escape markup characters that might be in file paths or user names
        private static string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }
    }
}