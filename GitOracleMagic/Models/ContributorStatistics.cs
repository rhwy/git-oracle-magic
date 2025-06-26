// Models/ContributorStatistics.cs
namespace GitRepoAnalyzer.Models
{
    public class ContributorStatistics
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public DateTime FirstCommit { get; set; }
        public DateTime LastCommit { get; set; }
        public int LinesAdded { get; set; }
        public int LinesDeleted { get; set; }
        public int TotalLinesChanged => LinesAdded + LinesDeleted;
        public double AverageLinesPerCommit => CommitCount > 0 ? (double)TotalLinesChanged / CommitCount : 0;
        public double AverageLinesPerWeek { get; set; }
        public TimeSpan ActivePeriod => LastCommit - FirstCommit;
        public double ActiveWeeks => ActivePeriod.TotalDays / 7.0;
    }

    public class ContributorAnalysisResult
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public List<ContributorStatistics> Contributors { get; set; } = new();
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
        public int TotalCommits { get; set; }
        public int TotalContributors { get; set; }
    }
}