// Commands/TimelineSettings.cs
using GitOracleMagic.Models;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GitOracleMagic.Commands
{
    public class TimelineSettings : CommandSettings
    {
        [CommandOption("-p|--path")]
        [Description("Path to the Git repository")]
        [DefaultValue(".")]
        public string RepositoryPath { get; set; } = ".";

        [CommandOption("-t|--top")]
        [Description("Number of top contributors to display in the timeline")]
        [DefaultValue(10)]
        public int TopContributors { get; set; } = 10;

        [CommandOption("--period")]
        [Description("Time period for grouping commits (daily, weekly, monthly)")]
        [DefaultValue("monthly")]
        public string PeriodString { get; set; } = "monthly";

        [CommandOption("-s|--since")]
        [Description("Only analyze commits since this date (YYYY-MM-DD)")]
        public string? SinceDate { get; set; }

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose logging to console (logs are always written to file)")]
        [DefaultValue(false)]
        public bool Verbose { get; set; } = false;

        public TimePeriod GetPeriod()
        {
            return PeriodString.ToLowerInvariant() switch
            {
                "daily" or "day" or "d" => TimePeriod.Daily,
                "weekly" or "week" or "w" => TimePeriod.Weekly,
                "monthly" or "month" or "m" => TimePeriod.Monthly,
                _ => throw new ArgumentException($"Invalid period: {PeriodString}. Use 'daily', 'weekly', or 'monthly'.")
            };
        }

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