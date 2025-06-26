// Commands/ContributorsCommand.cs

using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class ContributorsCommand : AsyncCommand<ContributorsSettings>
    {
        private readonly IContributorAnalyzer _analyzer;
        private readonly IContributorReportGenerator _reporter;
        private readonly ILogger<ContributorsCommand> _logger;

        public ContributorsCommand(
            IContributorAnalyzer analyzer,
            IContributorReportGenerator reporter,
            ILogger<ContributorsCommand> logger)
        {
            _analyzer = analyzer;
            _reporter = reporter;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ContributorsSettings settings)
        {
            try
            {
                // Display a nice header
                AnsiConsole.Write(
                    new FigletText("Git Contributors")
                        .Centered()
                        .Color(Color.Blue));

                AnsiConsole.WriteLine();

                // Show what we're about to do
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey)
                    .AddColumn("[yellow]Setting[/]")
                    .AddColumn("[green]Value[/]");

                table.AddRow("Repository Path", $"[blue]{Path.GetFullPath(settings.RepositoryPath)}[/]");
                table.AddRow("Top Contributors", $"[blue]{settings.TopContributors}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Perform the analysis with a progress bar
                var result = await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Analyzing contributors...[/]");
                        task.IsIndeterminate = true;

                        _logger.LogInformation("Starting contributor analysis of repository at {RepoPath}", settings.RepositoryPath);
                        var analysisResult = await _analyzer.AnalyzeContributorsAsync(settings.RepositoryPath, settings.TopContributors);
                        
                        task.StopTask();
                        return analysisResult;
                    });

                // Generate the report
                _reporter.GenerateReport(result, settings.TopContributors);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing contributors");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}