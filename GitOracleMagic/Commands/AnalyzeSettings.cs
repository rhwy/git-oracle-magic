// Commands/AnalyzeSettings.cs

using System.ComponentModel;
using Spectre.Console.Cli;

namespace GitOracleMagic.Commands
{
    public class AnalyzeSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-t|--top")]
        [Description("Number of top files to display")]
        [DefaultValue(10)]
        public int TopFiles { get; set; } = 10;

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;
    }
}