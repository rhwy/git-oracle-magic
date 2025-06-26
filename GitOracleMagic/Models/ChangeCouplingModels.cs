// Models/ChangeCouplingModels.cs
namespace GitOracleMagic.Models
{
    public class FileCouplingStatistics
    {
        public string FilePath1 { get; set; } = string.Empty;
        public string FilePath2 { get; set; } = string.Empty;
        public int CouplingCount { get; set; }
        public double CouplingStrength { get; set; } // Percentage of commits where both files changed together
        public List<string> SharedCommits { get; set; } = new();
        public DateTime FirstSharedCommit { get; set; }
        public DateTime LastSharedCommit { get; set; }
    }

    public class FileChangeFrequency
    {
        public string FilePath { get; set; } = string.Empty;
        public int ChangeCount { get; set; }
        public List<string> CommitIds { get; set; } = new();
    }

    public class ChangeCouplingResult
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
        public DateTime? SinceDate { get; set; }
        public List<FileCouplingStatistics> CoupledFiles { get; set; } = new();
        public List<FileChangeFrequency> FileFrequencies { get; set; } = new();
        public int TotalCommitsAnalyzed { get; set; }
        public int TotalFilesAnalyzed { get; set; }
        public double MinimumCouplingStrength { get; set; }
    }
}