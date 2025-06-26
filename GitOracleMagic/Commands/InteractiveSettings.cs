// Commands/InteractiveSettings.cs
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GitOracleMagic.Commands
{
    public class InteractiveSettings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("Path to the Git repository")]
        public string? RepositoryPath { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;

        public string GetRepositoryPath()
        {
            return string.IsNullOrWhiteSpace(RepositoryPath) ? Environment.CurrentDirectory : RepositoryPath;
        }
    }
}