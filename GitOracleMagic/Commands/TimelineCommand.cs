// Commands/TimelineCommand.cs
using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class TimelineCommand : AsyncCommand<TimelineSettings>
    {
        private readonly ITimelineAnalyzer _analyzer;
        private readonly ITimelineReportGenerator _reporter;
        private readonly ILogger<TimelineCommand> _logger;

        public TimelineCommand(
            ITimelineAnalyzer analyzer,
            ITimelineReportGenerator reporter,
            ILogger<TimelineCommand> logger)
        {
            _analyzer = analyzer;
            _reporter = reporter;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, TimelineSettings settings)
        {
            try
            {
                // Validate settings
                var period = settings.GetPeriod();
                var sinceDate = settings.GetSinceDateTime();

                // Display a nice header
                AnsiConsole.Write(
                    new FigletText("Git Oracle Magic")
                        .Centered()
                        .Color(Color.Green));

                AnsiConsole.WriteLine();

                // Show what we're about to do
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey)
                    .AddColumn("[yellow]Setting[/]")
                    .AddColumn("[green]Value[/]");

                table.AddRow("Repository Path", $"[blue]{Path.GetFullPath(settings.RepositoryPath)}[/]");
                table.AddRow("Top Contributors", $"[blue]{settings.TopContributors}[/]");
                table.AddRow("Period", $"[blue]{period}[/]");
                table.AddRow("Since Date", $"[blue]{sinceDate?.ToString("yyyy-MM-dd") ?? "All time"}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Perform the analysis with a progress bar
                var result = await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Analyzing commit timeline...[/]");
                        task.IsIndeterminate = true;

                        _logger.LogInformation("Starting timeline analysis of repository at {RepoPath}", settings.RepositoryPath);
                        var analysisResult = await _analyzer.AnalyzeTimelineAsync(
                            settings.RepositoryPath, 
                            period, 
                            settings.TopContributors,
                            sinceDate);
                        
                        task.StopTask();
                        return analysisResult;
                    });

                // Generate the report
                _reporter.GenerateReport(result);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing timeline");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}