// Commands/CouplingSettings.cs

using System.ComponentModel;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class CouplingSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-t|--top")]
        [Description("Number of top coupled file pairs to display")]
        [DefaultValue(15)]
        public int TopCouples { get; set; } = 15;

        [CommandOption("-s|--since")]
        [Description("Only analyze commits since this date (YYYY-MM-DD)")]
        public string? SinceDate { get; set; }

        [CommandOption("-m|--min-strength")]
        [Description("Minimum coupling strength percentage (0.0-1.0)")]
        [DefaultValue(0.1)]
        public double MinimumCouplingStrength { get; set; } = 0.1;

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

            throw new ArgumentException($"Invalid date format: {SinceDate}. Use YYYY-MM-DD format.");
        }
    }
}