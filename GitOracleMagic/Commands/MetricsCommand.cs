// Commands/MetricsCommand.cs
using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class MetricsCommand : AsyncCommand<MetricsSettings>
    {
        private readonly ICodeMetricsAnalyzer _analyzer;
        private readonly ICodeMetricsReportGenerator _reporter;
        private readonly ILogger<MetricsCommand> _logger;

        public MetricsCommand(
            ICodeMetricsAnalyzer analyzer,
            ICodeMetricsReportGenerator reporter,
            ILogger<MetricsCommand> logger)
        {
            _analyzer = analyzer;
            _reporter = reporter;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, MetricsSettings settings)
        {
            try
            {
                var config = settings.GetConfiguration();

                // Display a nice header
                AnsiConsole.Write(
                    new FigletText("Git Oracle Magic")
                        .Centered()
                        .Color(Color.Green));

                AnsiConsole.WriteLine();

                // Show what we're about to do
                var table = new Table()
                    .BorderColor(Color.Grey)
                    .AddColumn("[yellow]Setting[/]")
                    .AddColumn("[green]Value[/]");

                table.AddRow("Repository Path", $"[blue]{Path.GetFullPath(settings.RepositoryPath)}[/]");
                table.AddRow("Analyzer", $"[blue]{config.Analyzer}[/]");
                table.AddRow("Top Files", $"[blue]{settings.TopFiles}[/]");
                table.AddRow("File Extensions", $"[blue]{string.Join(", ", config.FileExtensions)}[/]");
                table.AddRow("Exclude Patterns", $"[blue]{string.Join(", ", config.ExcludePatterns)}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Check if analyzer is available
                var isAvailable = await _analyzer.IsAnalyzerAvailableAsync(config.Analyzer);
                if (!isAvailable)
                {
                    var errorPanel = new Panel($"[red]❌ {config.Analyzer} analyzer is not available![/]\n\n" +
                        GetInstallationInstructions(config.Analyzer))
                        .Header(" Analyzer Not Found ")
                        .BorderColor(Color.Red);

                    AnsiConsole.Write(errorPanel);
                    return 1;
                }

                // Show analyzer version
                var version = await _analyzer.GetAnalyzerVersionAsync(config.Analyzer);
                AnsiConsole.MarkupLine($"[green]✓[/] Using {config.Analyzer} version: [dim]{version}[/]\n");

                // Perform the analysis with a progress bar
                var result = await AnsiConsole.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Analyzing code metrics...[/]");
                        task.IsIndeterminate = true;

                        _logger.LogInformation("Starting code metrics analysis of repository at {RepoPath}", settings.RepositoryPath);
                        var analysisResult = await _analyzer.AnalyzeCodeMetricsAsync(settings.RepositoryPath, config);
                        
                        task.StopTask();
                        return analysisResult;
                    });

                // Check if analysis was successful
                if (!result.AnalysisSuccessful)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Analysis failed: {result.ErrorMessage}[/]");
                    return 1;
                }

                // Generate the report
                _reporter.GenerateReport(result, settings.TopFiles);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing code metrics");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        private static string GetInstallationInstructions(GitOracleMagic.Models.CodeMetricsAnalyzer analyzer)
        {
            return analyzer switch
            {
                GitOracleMagic.Models.CodeMetricsAnalyzer.Lizard => """
                    [bold]To install Lizard:[/]
                    [blue]pip install lizard[/]
                    
                    [bold]Or using conda:[/]
                    [blue]conda install -c conda-forge lizard[/]
                    """,
                GitOracleMagic.Models.CodeMetricsAnalyzer.SonarScanner => """
                    [bold]To install SonarScanner:[/]
                    1. Download from: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/
                    2. Add to PATH
                    """,
                _ => "Please ensure the analyzer is installed and available in PATH."
            };
        }
    }
}