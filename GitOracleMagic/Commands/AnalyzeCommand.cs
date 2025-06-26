// Commands/AnalyzeCommand.cs

using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class AnalyzeCommand : AsyncCommand<AnalyzeSettings>
    {
        private readonly IGitRepositoryAnalyzer _analyzer;
        private readonly IReportGenerator _reporter;
        private readonly ILogger<AnalyzeCommand> _logger;

        public AnalyzeCommand(
            IGitRepositoryAnalyzer analyzer,
            IReportGenerator reporter,
            ILogger<AnalyzeCommand> logger)
        {
            _analyzer = analyzer;
            _reporter = reporter;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, AnalyzeSettings settings)
        {
            try
            {
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
                table.AddRow("Top Files", $"[blue]{settings.TopFiles}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Perform the analysis with a progress bar
                var result = await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Analyzing repository...[/]");
                        task.IsIndeterminate = true;

                        _logger.LogInformation("Starting analysis of repository at {RepoPath}", settings.RepositoryPath);
                        var analysisResult = await _analyzer.AnalyzeRepositoryAsync(settings.RepositoryPath, settings.TopFiles);
                        
                        task.StopTask();
                        return analysisResult;
                    });

                // Generate the report
                _reporter.GenerateReport(result, settings.TopFiles);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing repository");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}