// Program.cs
using GitRepoAnalyzer.Commands;
using GitRepoAnalyzer.Infrastructure;
using GitRepoAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

// Create service collection and configure DI
var services = new ServiceCollection();

// Add logging - redirect to file by default, console only for verbose mode
services.AddLogging(builder =>
{
    // Create logs directory if it doesn't exist
    var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
    Directory.CreateDirectory(logsDir);
    
    var logFile = Path.Combine(logsDir, $"git-oracle-magic-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    
    // Only add console logging if verbose mode is detected in args
    var isVerbose = args.Contains("--verbose") || args.Contains("-v");
    
    if (isVerbose)
    {
        builder.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Warning;
        });
    }
    
    // Always add file logging
    //builder.AddFile(logFile, LogLevel.Debug);
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add our services
services.AddTransient<IGitRepositoryAnalyzer, GitRepositoryAnalyzer>();
services.AddTransient<IReportGenerator, ConsoleReportGenerator>();

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

    config.UseStrictParsing();
    config.ValidateExamples();
});

return await app.RunAsync(args);