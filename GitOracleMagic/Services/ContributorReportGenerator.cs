// Services/ContributorReportGenerator.cs
using GitRepoAnalyzer.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GitRepoAnalyzer.Services
{
    public class ContributorReportGenerator : IContributorReportGenerator
    {
        private readonly ILogger<ContributorReportGenerator> _logger;

        public ContributorReportGenerator(ILogger<ContributorReportGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateReport(ContributorAnalysisResult result, int topContributors)
        {
            _logger.LogInformation("Generating contributor report for top {TopContributors} contributors", topContributors);

            // Create a beautiful header
            var rule = new Rule($"[yellow]Top Contributors Report[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            // Repository information panel
            var repoPanel = new Panel($"""
                [bold]Repository:[/] {EscapeMarkup(result.RepositoryPath)}
                [bold]Analysis Date:[/] {result.AnalysisTime:yyyy-MM-dd HH:mm:ss}
                [bold]Total Contributors:[/] {result.TotalContributors}
                [bold]Total Commits:[/] {result.TotalCommits:N0}
                """)
                .Header(" Repository Information ")
                .BorderColor(Color.Blue)
                .RoundedBorder();

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();

            if (!result.Contributors.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No contributors found in the repository.[/]");
                return;
            }

            // Create a table for the results
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("[yellow]Rank[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Name[/]").LeftAligned())
                .AddColumn(new TableColumn("[yellow]Commits[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]First Commit[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Last Commit[/]").Centered())
                .AddColumn(new TableColumn("[yellow]Lines +[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Lines -[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Total Lines[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Avg/Commit[/]").RightAligned())
                .AddColumn(new TableColumn("[yellow]Avg/Week[/]").RightAligned());

            int rank = 1;
            foreach (var contributor in result.Contributors)
            {
                // Color code the commit count based on activity level
                var commitCountColor = contributor.CommitCount switch
                {
                    > 500 => "red",
                    > 100 => "orange1",
                    > 20 => "yellow",
                    _ => "green"
                };

                // Color code total lines changed
                var linesColor = contributor.TotalLinesChanged switch
                {
                    > 10000 => "red",
                    > 5000 => "orange1", 
                    > 1000 => "yellow",
                    _ => "green"
                };

                table.AddRow(
                    $"[bold]{rank}[/]",
                    $"[blue]{EscapeMarkup(contributor.Name)}[/]\n[dim]{EscapeMarkup(contributor.Email)}[/]",
                    $"[{commitCountColor}]{contributor.CommitCount:N0}[/]",
                    $"[dim]{contributor.FirstCommit:yyyy-MM-dd}[/]",
                    $"[dim]{contributor.LastCommit:yyyy-MM-dd}[/]",
                    $"[green]{contributor.LinesAdded:N0}[/]",
                    $"[red]{contributor.LinesDeleted:N0}[/]",
                    $"[{linesColor}]{contributor.TotalLinesChanged:N0}[/]",
                    $"[blue]{contributor.AverageLinesPerCommit:F1}[/]",
                    $"[purple]{contributor.AverageLinesPerWeek:F1}[/]"
                );

                rank++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Summary statistics
            var totalCommits = result.Contributors.Sum(c => c.CommitCount);
            var totalLinesAdded = result.Contributors.Sum(c => c.LinesAdded);
            var totalLinesDeleted = result.Contributors.Sum(c => c.LinesDeleted);
            var totalLinesChanged = result.Contributors.Sum(c => c.TotalLinesChanged);
            var topContributor = result.Contributors.FirstOrDefault();

            var summaryPanel = new Panel($"""
                [bold]Total Commits (Top {topContributors}):[/] [green]{totalCommits:N0}[/] / {result.TotalCommits:N0} ([yellow]{(double)totalCommits / result.TotalCommits * 100:F1}%[/])
                [bold]Total Lines Added:[/] [green]{totalLinesAdded:N0}[/]
                [bold]Total Lines Deleted:[/] [red]{totalLinesDeleted:N0}[/]
                [bold]Total Lines Changed:[/] [blue]{totalLinesChanged:N0}[/]
                [bold]Most Active Contributor:[/] [blue]{EscapeMarkup(topContributor?.Name ?? "N/A")}[/] ([green]{topContributor?.CommitCount ?? 0:N0}[/] commits)
                """)
                .Header(" Summary Statistics ")
                .BorderColor(Color.Green)
                .RoundedBorder();

            AnsiConsole.Write(summaryPanel);

            _logger.LogInformation("Contributor report generation completed");
        }

        // Escape markup characters that might be in names or emails
        private static string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }
    }
}