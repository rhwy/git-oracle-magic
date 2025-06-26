// Commands/CouplingCommand.cs
using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class CouplingCommand : AsyncCommand<CouplingSettings>
    {
        private readonly IChangeCouplingAnalyzer _analyzer;
        private readonly ICouplingReportGenerator _reporter;
        private readonly ILogger<CouplingCommand> _logger;

        public CouplingCommand(
            IChangeCouplingAnalyzer analyzer,
            ICouplingReportGenerator reporter,
            ILogger<CouplingCommand> logger)
        {
            _analyzer = analyzer;
            _reporter = reporter;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CouplingSettings settings)
        {
            try
            {
                // Validate settings
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
                table.AddRow("Top Couples", $"[blue]{settings.TopCouples}[/]");
                table.AddRow("Since Date", $"[blue]{sinceDate?.ToString("yyyy-MM-dd") ?? "All time"}[/]");
                table.AddRow("Min Coupling Strength", $"[blue]{settings.MinimumCouplingStrength:P1}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Perform the analysis with a progress bar
                var result = await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Analyzing change coupling...[/]");
                        task.IsIndeterminate = true;

                        _logger.LogInformation("Starting change coupling analysis of repository at {RepoPath}", settings.RepositoryPath);
                        var analysisResult = await _analyzer.AnalyzeChangeCouplingAsync(
                            settings.RepositoryPath, 
                            settings.TopCouples, 
                            sinceDate, 
                            settings.MinimumCouplingStrength);
                        
                        task.StopTask();
                        return analysisResult;
                    });

                // Generate the report
                _reporter.GenerateReport(result, settings.TopCouples);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing change coupling");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}