// Commands/ExportCommand.cs
using GitOracleMagic.Models;
using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;

namespace GitOracleMagic.Commands
{
    public class ExportCommand : AsyncCommand<ExportSettings>
    {
        private readonly IComprehensiveAnalyzer _analyzer;
        private readonly IHtmlReportGenerator _htmlGenerator;
        private readonly ILogger<ExportCommand> _logger;

        public ExportCommand(
            IComprehensiveAnalyzer analyzer,
            IHtmlReportGenerator htmlGenerator,
            ILogger<ExportCommand> logger)
        {
            _analyzer = analyzer;
            _htmlGenerator = htmlGenerator;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ExportSettings settings)
        {
            try
            {
                // Validate settings
                var sinceDate = settings.GetSinceDateTime();
                var untilDate = settings.GetUntilDateTime();
                var outputPath = settings.GetOutputPath();

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
                table.AddRow("Output File", $"[blue]{outputPath}[/]");
                table.AddRow("Since Date", $"[blue]{sinceDate?.ToString("yyyy-MM-dd") ?? "Repository start"}[/]");
                table.AddRow("Until Date", $"[blue]{untilDate?.ToString("yyyy-MM-dd") ?? "Today"}[/]");
                table.AddRow("Auto-open Report", $"[blue]{!settings.NoOpen}[/]");
                table.AddRow("Verbose Logging", $"[blue]{settings.Verbose}[/]");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Create export configuration with defaults
                var config = new ExportConfiguration
                {
                    SinceDate = sinceDate,
                    UntilDate = untilDate,
                    OutputPath = outputPath,
                    OpenAfterExport = !settings.NoOpen,
                    // Use defaults for comprehensive report
                    TopFiles = 20,
                    TopContributors = 20,
                    TopCouples = 20,
                    MinCouplingStrength = 0.1,
                    TimelinePeriod = TimePeriod.Monthly,
                    TimelineContributors = 20
                };

                // Perform the comprehensive analysis with a progress bar
                var reportData = await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask("[green]Running comprehensive analysis...[/]");
                        task.MaxValue = 100;

                        _logger.LogInformation("Starting comprehensive export for repository at {RepoPath}", settings.RepositoryPath);
                        
                        task.Increment(10);
                        task.Description = "[green]Analyzing file changes...[/]";
                        await Task.Delay(100); // Small delay to show progress
                        
                        var result = await _analyzer.GenerateComprehensiveReportAsync(settings.RepositoryPath, config);
                        
                        task.Increment(70);
                        task.Description = "[green]Generating HTML report...[/]";
                        
                        task.Increment(20);
                        task.Description = "[green]Analysis complete![/]";
                        
                        return result;
                    });

                // Generate HTML report
                var htmlPath = await _htmlGenerator.GenerateHtmlReportAsync(reportData, outputPath);

                // Show success message
                var successPanel = new Panel($"""
                    [bold green]✅ Report Generated Successfully![/]
                    
                    [bold]File:[/] [blue]{htmlPath}[/]
                    [bold]Size:[/] [yellow]{GetFileSize(htmlPath)}[/]
                    [bold]Repository:[/] [dim]{reportData.RepositoryName}[/]
                    [bold]Period:[/] [dim]{GetAnalysisPeriod(reportData)}[/]
                    """)
                    .Header(" Export Complete ")
                    .BorderColor(Color.Green)
                    .RoundedBorder();

                AnsiConsole.Write(successPanel);

                // Open the report if requested
                if (!settings.NoOpen)
                {
                    AnsiConsole.MarkupLine("\n[dim]Opening report in default browser...[/]");
                    await OpenFileAsync(htmlPath);
                }

                _logger.LogInformation("HTML report generated successfully at {OutputPath}", htmlPath);

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comprehensive report");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        private static string GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var sizeInKb = fileInfo.Length / 1024.0;
                return $"{sizeInKb:F1} KB";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetAnalysisPeriod(ComprehensiveReportData data)
        {
            if (data.SinceDate.HasValue && data.UntilDate.HasValue)
                return $"{data.SinceDate.Value:yyyy-MM-dd} to {data.UntilDate.Value:yyyy-MM-dd}";
            if (data.SinceDate.HasValue)
                return $"Since {data.SinceDate.Value:yyyy-MM-dd}";
            if (data.UntilDate.HasValue)
                return $"Until {data.UntilDate.Value:yyyy-MM-dd}";
            return "Full repository history";
        }

        private static async Task OpenFileAsync(string filePath)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", filePath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", filePath);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't open the file
                await Task.CompletedTask;
            }
        }
    }
}