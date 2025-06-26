// Models/TimelineModels.cs
namespace GitOracleMagic.Models
{
    public enum TimePeriod
    {
        Daily,
        Weekly, 
        Monthly
    }

    public class TimelineEntry
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public int TotalCommits { get; set; }
        public Dictionary<string, ContributorCommits> Contributors { get; set; } = new();
    }

    public class ContributorCommits
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty; // Spectre.Console color name
    }

    public class TimelineResult
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
        public TimePeriod Period { get; set; }
        public DateTime? SinceDate { get; set; }
        public List<TimelineEntry> Timeline { get; set; } = new();
        public Dictionary<string, string> ContributorColors { get; set; } = new(); // Name -> Color
        public List<string> TopContributors { get; set; } = new(); // Ordered by total commits
        public int TotalCommitsAnalyzed { get; set; }
        public int TotalContributors { get; set; }
        public int MaxCommitsInPeriod { get; set; }
    }
}