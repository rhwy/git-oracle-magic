// Services/TimelineReportGenerator.cs
using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GitOracleMagic.Services
{
    public class TimelineReportGenerator : ITimelineReportGenerator
    {
        private readonly ILogger<TimelineReportGenerator> _logger;

        public TimelineReportGenerator(ILogger<TimelineReportGenerator> logger)
        {
            _logger = logger;
        }

        public void GenerateReport(TimelineResult result)
        {
            _logger.LogInformation("Generating timeline report for {PeriodCount} periods", result.Timeline.Count);

            // Create a beautiful header
            var rule = new Rule($"[yellow]Commit Timeline Analysis ({result.Period})[/]")
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
                [bold]Period Granularity:[/] {result.Period}
                [bold]Total Commits:[/] {result.TotalCommitsAnalyzed:N0}
                [bold]Total Contributors:[/] {result.TotalContributors:N0}
                [bold]Top Contributors Shown:[/] {result.TopContributors.Count}
                """)
                .Header(" Analysis Information ")
                .BorderColor(Color.Blue)
                .RoundedBorder();

            AnsiConsole.Write(repoPanel);
            AnsiConsole.WriteLine();

            if (!result.Timeline.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No commits found in the specified time period.[/]");
                return;
            }

            // Calculate bar scale (max width for the largest bar)
            const int maxBarWidth = 60;
            var scaleFactor = result.MaxCommitsInPeriod > 0 
                ? (double)maxBarWidth / result.MaxCommitsInPeriod 
                : 1.0;

            // Display timeline
            var timelineRule = new Rule("[yellow]Commit Timeline[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(timelineRule);
            AnsiConsole.WriteLine();

            foreach (var entry in result.Timeline)
            {
                // Calculate bar width
                var barWidth = Math.Max(1, (int)(entry.TotalCommits * scaleFactor));
                
                // Build the colored bar representing contributor proportions
                var bar = BuildContributorBar(entry, barWidth);
                
                // Create the timeline entry
                var commitText = entry.TotalCommits == 1 ? "commit" : "commits";
                var line = $"{entry.PeriodLabel.PadRight(15)} {bar} [dim]({entry.TotalCommits} {commitText})[/]";
                
                AnsiConsole.MarkupLine(line);
            }

            AnsiConsole.WriteLine();

            // Contributors legend
            DisplayContributorsLegend(result);

            // Summary statistics
            DisplaySummaryStatistics(result);

            _logger.LogInformation("Timeline report generation completed");
        }

        private static string BuildContributorBar(TimelineEntry entry, int totalBarWidth)
        {
            if (entry.TotalCommits == 0 || totalBarWidth == 0)
                return "[dim]░[/]";

            var bar = "";
            var remainingWidth = totalBarWidth;
            
            // Sort contributors by commit count (descending) for consistent color ordering
            var sortedContributors = entry.Contributors.Values
                .OrderByDescending(c => c.CommitCount)
                .ToList();

            foreach (var contributor in sortedContributors)
            {
                if (remainingWidth <= 0) break;
                
                // Calculate width for this contributor based on their percentage
                var contributorWidth = Math.Max(1, (int)(totalBarWidth * contributor.Percentage / 100.0));
                contributorWidth = Math.Min(contributorWidth, remainingWidth);
                
                // Add colored blocks for this contributor
                var block = new string('█', contributorWidth);
                bar += $"[{contributor.Color}]{block}[/]";
                
                remainingWidth -= contributorWidth;
            }

            // Fill any remaining space with a neutral color (shouldn't happen with proper calculation)
            if (remainingWidth > 0)
            {
                var remaining = new string('█', remainingWidth);
                bar += $"[dim]{remaining}[/]";
            }

            return bar;
        }

        private static void DisplayContributorsLegend(TimelineResult result)
        {
            var legendRule = new Rule("[yellow]Contributors Legend[/]")
                .RuleStyle("grey")
                .LeftJustified();
            
            AnsiConsole.Write(legendRule);
            AnsiConsole.WriteLine();

            // Calculate total commits per contributor across all periods
            var contributorTotals = new Dictionary<string, int>();
            foreach (var entry in result.Timeline)
            {
                foreach (var contributor in entry.Contributors.Values)
                {
                    var key = $"{contributor.Name} <{contributor.Email}>";
                    if (!contributorTotals.ContainsKey(key))
                        contributorTotals[key] = 0;
                    contributorTotals[key] += contributor.CommitCount;
                }
            }

            // Display legend sorted by total commits
            var sortedContributors = contributorTotals
                .OrderByDescending(kvp => kvp.Value)
                .Take(result.TopContributors.Count)
                .ToList();

            var legendTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn("")
                .AddColumn("")
                .AddColumn("");

            foreach (var kvp in sortedContributors)
            {
                var authorKey = kvp.Key;
                var totalCommits = kvp.Value;
                
                if (result.ContributorColors.TryGetValue(authorKey, out var color))
                {
                    var name = ExtractName(authorKey);
                    var colorBlock = $"[{color}]███[/]";
                    var percentage = result.TotalCommitsAnalyzed > 0 
                        ? (double)totalCommits / result.TotalCommitsAnalyzed * 100 
                        : 0;
                    
                    legendTable.AddRow(
                        colorBlock,
                        $"[bold]{EscapeMarkup(name)}[/]",
                        $"[dim]{totalCommits:N0} commits ({percentage:F1}%)[/]"
                    );
                }
            }

            AnsiConsole.Write(legendTable);
            AnsiConsole.WriteLine();
        }

        private static void DisplaySummaryStatistics(TimelineResult result)
        {
            var activePeriods = result.Timeline.Count(e => e.TotalCommits > 0);
            var avgCommitsPerPeriod = result.Timeline.Any() 
                ? result.Timeline.Average(e => e.TotalCommits) 
                : 0;
            var mostActivePeriod = result.Timeline
                .OrderByDescending(e => e.TotalCommits)
                .FirstOrDefault();

            var summaryPanel = new Panel($"""
                [bold]Active Periods:[/] [green]{activePeriods}[/] / {result.Timeline.Count}
                [bold]Average Commits per {result.Period}:[/] [blue]{avgCommitsPerPeriod:F1}[/]
                [bold]Peak Activity:[/] [yellow]{EscapeMarkup(mostActivePeriod?.PeriodLabel ?? "N/A")}[/] ([red]{mostActivePeriod?.TotalCommits ?? 0}[/] commits)
                [bold]Analysis Covers:[/] [green]{result.Timeline.Count}[/] {result.Period.ToString().ToLower()} periods
                """)
                .Header(" Summary Statistics ")
                .BorderColor(Color.Green)
                .RoundedBorder();

            AnsiConsole.Write(summaryPanel);
        }

        private static string ExtractName(string authorKey)
        {
            var emailStart = authorKey.LastIndexOf(" <");
            return emailStart > 0 ? authorKey.Substring(0, emailStart) : authorKey;
        }

        // Escape markup characters that might be in file paths or names
        private static string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }
    }
}