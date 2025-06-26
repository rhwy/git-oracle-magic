// Commands/ContributorsSettings.cs
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GitRepoAnalyzer.Commands
{
    public class ContributorsSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-t|--top")]
        [Description("Number of top contributors to display")]
        [DefaultValue(10)]
        public int TopContributors { get; set; } = 10;

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;
    }
}