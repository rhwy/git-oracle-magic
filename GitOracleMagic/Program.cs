// Program.cs

using GitOracleMagic.Commands;
using GitOracleMagic.Services;
using GitOracleMagic.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console.Cli;

// Create logs directory if it doesn't exist
var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
Directory.CreateDirectory(logsDir);

var logFile = Path.Combine(logsDir, $"git-oracle-magic-{DateTime.Now:yyyyMMdd-HHmmss}.log");

// Check if verbose mode is requested
var isVerbose = args.Contains("--verbose") || args.Contains("-v");

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        logFile,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

// Only add console sink if verbose mode is enabled
if (isVerbose)
{
    loggerConfig.WriteTo.Console(
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
}

Log.Logger = loggerConfig.CreateLogger();

// Create service collection and configure DI
var services = new ServiceCollection();

// Add Serilog
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog();
});

// Add our services
services.AddTransient<IGitRepositoryAnalyzer, GitRepositoryAnalyzer>();
services.AddTransient<IReportGenerator, ConsoleReportGenerator>();
services.AddTransient<IContributorAnalyzer, ContributorAnalyzer>();
services.AddTransient<IContributorReportGenerator, ContributorReportGenerator>();
services.AddTransient<IChangeCouplingAnalyzer, ChangeCouplingAnalyzer>();
services.AddTransient<ICouplingReportGenerator, CouplingReportGenerator>();

// Create the command app
var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config.SetApplicationName("git-oracle-magic");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Analyze a Git repository to find the most changed files")
        .WithExample(new[] { "analyze", "--path", "/path/to/repo", "--top", "20" })
        .WithExample(new[] { "analyze", "-p", ".", "-t", "15", "--verbose" });

    config.AddCommand<ContributorsCommand>("contributors")
        .WithDescription("Analyze Git repository contributors and their statistics")
        .WithExample(new[] { "contributors", "--path", "/path/to/repo", "--top", "15" })
        .WithExample(new[] { "contributors", "-p", ".", "-t", "20", "--verbose" });

    config.AddCommand<CouplingCommand>("coupling")
        .WithDescription("Analyze change coupling between files (files that change together)")
        .WithExample(new[] { "coupling", "--path", "/path/to/repo", "--top", "20" })
        .WithExample(new[] { "coupling", "-p", ".", "-t", "15", "--since", "2023-01-01" })
        .WithExample(new[] { "coupling", "--min-strength", "0.3", "--verbose" });

    config.UseStrictParsing();
    config.ValidateExamples();
});

try
{
    return await app.RunAsync(args);
}
finally
{
    Log.CloseAndFlush();
}