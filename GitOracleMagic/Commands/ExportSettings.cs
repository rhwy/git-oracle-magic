// Commands/ExportSettings.cs
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GitOracleMagic.Commands
{
    public class ExportSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-o|--output")]
        [Description("Output file path for the HTML report")]
        public string? OutputPath { get; set; }

        [CommandOption("-s|--since")]
        [Description("Only analyze commits since this date (YYYY-MM-DD)")]
        public string? SinceDate { get; set; }

        [CommandOption("-u|--until")]
        [Description("Only analyze commits until this date (YYYY-MM-DD)")]
        public string? UntilDate { get; set; }

        [CommandOption("--no-open")]
        [Description("Don't automatically open the report after generation")]
        [DefaultValue(false)]
        public bool NoOpen { get; set; } = false;

        [CommandOption("--html-template")]
        [Description("Path to custom HTML template file")]
        public string? HtmlTemplatePath { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;

        public DateTime? GetSinceDateTime()
        {
            if (string.IsNullOrWhiteSpace(SinceDate))
                return null;

            if (DateTime.TryParse(SinceDate, out var date))
                return date;

            throw new ArgumentException($"Invalid since date format: {SinceDate}. Use YYYY-MM-DD format.");
        }

        public DateTime? GetUntilDateTime()
        {
            if (string.IsNullOrWhiteSpace(UntilDate))
                return null;

            if (DateTime.TryParse(UntilDate, out var date))
                return date.AddDays(1).AddTicks(-1); // End of day

            throw new ArgumentException($"Invalid until date format: {UntilDate}. Use YYYY-MM-DD format.");
        }

        public string GetOutputPath()
        {
            if (!string.IsNullOrWhiteSpace(OutputPath))
                return OutputPath;

            var repoName = Path.GetFileName(Path.GetFullPath(RepositoryPath));
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            return $"git-oracle-report-{repoName}-{timestamp}.html";
        }
    }
}