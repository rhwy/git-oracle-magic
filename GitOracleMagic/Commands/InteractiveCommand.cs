// Commands/InteractiveCommand.cs
using GitOracleMagic.Models;
using GitOracleMagic.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class InteractiveCommand : AsyncCommand<InteractiveSettings>
    {
        private readonly IGitRepositoryAnalyzer _gitAnalyzer;
        private readonly IReportGenerator _reportGenerator;
        private readonly IContributorAnalyzer _contributorAnalyzer;
        private readonly IContributorReportGenerator _contributorReportGenerator;
        private readonly IChangeCouplingAnalyzer _couplingAnalyzer;
        private readonly ICouplingReportGenerator _couplingReportGenerator;
        private readonly ITimelineAnalyzer _timelineAnalyzer;
        private readonly ITimelineReportGenerator _timelineReportGenerator;
        private readonly ILogger<InteractiveCommand> _logger;

        public InteractiveCommand(
            IGitRepositoryAnalyzer gitAnalyzer,
            IReportGenerator reportGenerator,
            IContributorAnalyzer contributorAnalyzer,
            IContributorReportGenerator contributorReportGenerator,
            IChangeCouplingAnalyzer couplingAnalyzer,
            ICouplingReportGenerator couplingReportGenerator,
            ITimelineAnalyzer timelineAnalyzer,
            ITimelineReportGenerator timelineReportGenerator,
            ILogger<InteractiveCommand> logger)
        {
            _gitAnalyzer = gitAnalyzer;
            _reportGenerator = reportGenerator;
            _contributorAnalyzer = contributorAnalyzer;
            _contributorReportGenerator = contributorReportGenerator;
            _couplingAnalyzer = couplingAnalyzer;
            _couplingReportGenerator = couplingReportGenerator;
            _timelineAnalyzer = timelineAnalyzer;
            _timelineReportGenerator = timelineReportGenerator;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, InteractiveSettings settings)
        {
            try
            {
                var repoPath = settings.GetRepositoryPath();
                
                // Validate repository path
                if (!Directory.Exists(repoPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Repository path '{repoPath}' does not exist.[/]");
                    return 1;
                }

                // Display welcome header
                ShowWelcomeHeader(repoPath);

                // Main interactive loop
                while (true)
                {
                    var choice = ShowMainMenu();
                    
                    if (choice == "quit")
                    {
                        AnsiConsole.MarkupLine("\n[green]Thanks for using Git Oracle Magic! 🔮✨[/]");
                        break;
                    }

                    AnsiConsole.Clear();
                    ShowWelcomeHeader(repoPath);

                    try
                    {
                        await ExecuteCommand(choice, repoPath, settings.Verbose);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing command {Command}", choice);
                        AnsiConsole.WriteException(ex);
                    }

                    // Wait for user input before returning to menu
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]Press any key to return to the main menu...[/]");
                    Console.ReadKey();
                    AnsiConsole.Clear();
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in interactive mode");
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        private static void ShowWelcomeHeader(string repoPath)
        {
            AnsiConsole.Write(
                new FigletText("Git Oracle Magic")
                    .Centered()
                    .Color(Color.Green));

            AnsiConsole.WriteLine();
            
            var panel = new Panel($"[bold]Interactive Mode[/]\n[blue]Repository:[/] {repoPath}")
                //.Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 0);
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private static string ShowMainMenu()
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]🔮 What would you like to analyze?[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .AddChoices(new[]
                    {
                        "analyze", "contributors", "coupling", "timeline", "quit"
                    })
                    .UseConverter(choice => choice switch
                    {
                        "analyze" => "📊 File Changes Analysis - Find most changed files",
                        "contributors" => "👥 Contributors Analysis - Top contributors statistics", 
                        "coupling" => "🔗 Change Coupling - Files that change together",
                        "timeline" => "📈 Timeline Visualization - Commit activity over time",
                        "quit" => "❌ Quit - Exit Git Oracle Magic",
                        _ => choice
                    }));

            return choice;
        }

        private async Task ExecuteCommand(string command, string repoPath, bool verbose)
        {
            switch (command)
            {
                case "analyze":
                    await ExecuteAnalyzeCommand(repoPath, verbose);
                    break;
                case "contributors":
                    await ExecuteContributorsCommand(repoPath, verbose);
                    break;
                case "coupling":
                    await ExecuteCouplingCommand(repoPath, verbose);
                    break;
                case "timeline":
                    await ExecuteTimelineCommand(repoPath, verbose);
                    break;
            }
        }

        private async Task ExecuteAnalyzeCommand(string repoPath, bool verbose)
        {
            AnsiConsole.MarkupLine("[yellow]📊 File Changes Analysis Configuration[/]\n");

            var topFiles = AnsiConsole.Prompt(
                new TextPrompt<int>("[blue]Number of top files to display:[/]")
                    .DefaultValue(10)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Number must be greater than 0[/]")));

            AnsiConsole.WriteLine();
            
            var result = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing repository...[/]");
                    task.IsIndeterminate = true;
                    
                    return await _gitAnalyzer.AnalyzeRepositoryAsync(repoPath, topFiles);
                });

            _reportGenerator.GenerateReport(result, topFiles);
        }

        private async Task ExecuteContributorsCommand(string repoPath, bool verbose)
        {
            AnsiConsole.MarkupLine("[yellow]👥 Contributors Analysis Configuration[/]\n");

            var topContributors = AnsiConsole.Prompt(
                new TextPrompt<int>("[blue]Number of top contributors to display:[/]")
                    .DefaultValue(10)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Number must be greater than 0[/]")));

            AnsiConsole.WriteLine();

            var result = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing contributors...[/]");
                    task.IsIndeterminate = true;
                    
                    return await _contributorAnalyzer.AnalyzeContributorsAsync(repoPath, topContributors);
                });

            _contributorReportGenerator.GenerateReport(result, topContributors);
        }

        private async Task ExecuteCouplingCommand(string repoPath, bool verbose)
        {
            AnsiConsole.MarkupLine("[yellow]🔗 Change Coupling Analysis Configuration[/]\n");

            var topCouples = AnsiConsole.Prompt(
                new TextPrompt<int>("[blue]Number of top coupled file pairs to display:[/]")
                    .DefaultValue(15)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Number must be greater than 0[/]")));

            var sinceDate = AnsiConsole.Prompt(
                new TextPrompt<string>("[blue]Analyze commits since date (YYYY-MM-DD, or press Enter for all time):[/]")
                    .DefaultValue("")
                    .AllowEmpty()
                    .Validate(date =>
                    {
                        if (string.IsNullOrWhiteSpace(date)) return ValidationResult.Success();
                        return DateTime.TryParse(date, out _) ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid date format. Use YYYY-MM-DD[/]");
                    }));

            var minStrength = AnsiConsole.Prompt(
                new TextPrompt<double>("[blue]Minimum coupling strength (0.0-1.0):[/]")
                    .DefaultValue(0.1)
                    .ValidationErrorMessage("[red]Please enter a valid number between 0.0 and 1.0[/]")
                    .Validate(n => n >= 0.0 && n <= 1.0 ? ValidationResult.Success() : ValidationResult.Error("[red]Number must be between 0.0 and 1.0[/]")));

            AnsiConsole.WriteLine();

            var sinceDateParsed = string.IsNullOrWhiteSpace(sinceDate) ? DateTime.MinValue : DateTime.Parse(sinceDate);

            var result = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing change coupling...[/]");
                    task.IsIndeterminate = true;
                    
                    return await _couplingAnalyzer.AnalyzeChangeCouplingAsync(repoPath, topCouples, sinceDateParsed, minStrength);
                });

            _couplingReportGenerator.GenerateReport(result, topCouples);
        }

        private async Task ExecuteTimelineCommand(string repoPath, bool verbose)
        {
            AnsiConsole.MarkupLine("[yellow]📈 Timeline Visualization Configuration[/]\n");

            var topContributors = AnsiConsole.Prompt(
                new TextPrompt<int>("[blue]Number of top contributors to display:[/]")
                    .DefaultValue(10)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Number must be greater than 0[/]")));

            var period = AnsiConsole.Prompt(
                new SelectionPrompt<TimePeriod>()
                    .Title("[blue]Time period for grouping commits:[/]")
                    .AddChoices(TimePeriod.Daily, TimePeriod.Weekly, TimePeriod.Monthly)
                    .UseConverter(period => period switch
                    {
                        TimePeriod.Daily => "📅 Daily - Individual days",
                        TimePeriod.Weekly => "📋 Weekly - Week by week", 
                        TimePeriod.Monthly => "📊 Monthly - Month by month",
                        _ => period.ToString()
                    }));

            var sinceDate = AnsiConsole.Prompt(
                new TextPrompt<string>("[blue]Analyze commits since date (YYYY-MM-DD, or press Enter for all time):[/]")
                    .DefaultValue("")
                    .AllowEmpty()
                    .Validate(date =>
                    {
                        if (string.IsNullOrWhiteSpace(date)) return ValidationResult.Success();
                        return DateTime.TryParse(date, out _) ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid date format. Use YYYY-MM-DD[/]");
                    }));

            AnsiConsole.WriteLine();

            var sinceDateParsed = string.IsNullOrWhiteSpace(sinceDate) ? DateTime.MinValue : DateTime.Parse(sinceDate);

            var result = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing commit timeline...[/]");
                    task.IsIndeterminate = true;
                    
                    return await _timelineAnalyzer.AnalyzeTimelineAsync(repoPath, period, topContributors, sinceDateParsed);
                });

            _timelineReportGenerator.GenerateReport(result);
        }
    }
}