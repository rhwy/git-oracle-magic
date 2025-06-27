// Commands/MetricsSettings.cs
using GitOracleMagic.Models;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GitOracleMagic.Commands
{
    public class MetricsSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-t|--top")]
        [Description("Number of top complex files to display")]
        [DefaultValue(20)]
        public int TopFiles { get; set; } = 20;

        [CommandOption("--analyzer")]
        [Description("Code metrics analyzer to use (lizard, sonar, custom)")]
        [DefaultValue("lizard")]
        public string AnalyzerString { get; set; } = "lizard";

        [CommandOption("--custom-analyzer")]
        [Description("Path to custom analyzer executable")]
        public string? CustomAnalyzerPath { get; set; }

        [CommandOption("--custom-args")]
        [Description("Custom analyzer arguments")]
        public string? CustomAnalyzerArgs { get; set; }

        [CommandOption("--extensions")]
        [Description("File extensions to analyze (comma-separated)")]
        [DefaultValue(".cs,.js,.ts,.py,.java,.cpp,.c,.h")]
        public string Extensions { get; set; } = ".cs,.js,.ts,.py,.java,.cpp,.c,.h";

        [CommandOption("--exclude")]
        [Description("Patterns to exclude (comma-separated)")]
        [DefaultValue("node_modules,bin,obj,.git,packages")]
        public string ExcludePatterns { get; set; } = "node_modules,bin,obj,.git,packages";

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;

        public CodeMetricsAnalyzer GetAnalyzer()
        {
            return AnalyzerString.ToLowerInvariant() switch
            {
                "lizard" => CodeMetricsAnalyzer.Lizard,
                "sonar" => CodeMetricsAnalyzer.SonarScanner,
                "custom" => CodeMetricsAnalyzer.Custom,
                _ => throw new ArgumentException($"Unknown analyzer: {AnalyzerString}")
            };
        }

        public CodeMetricsConfiguration GetConfiguration()
        {
            return new CodeMetricsConfiguration
            {
                Analyzer = GetAnalyzer(),
                CustomAnalyzerPath = CustomAnalyzerPath,
                CustomAnalyzerArgs = CustomAnalyzerArgs,
                TopComplexFiles = TopFiles,
                FileExtensions = Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim())
                    .ToList(),
                ExcludePatterns = ExcludePatterns.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pattern => pattern.Trim())
                    .ToList()
            };
        }
    }
}