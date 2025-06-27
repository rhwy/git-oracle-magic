// Models/FileStatistics.cs
namespace GitOracleMagic.Models
{
    public class FileStatistics
    {
        public string Path { get; set; } = string.Empty;
        public int ChangeCount { get; set; }
        public CommitInfo FirstChange { get; set; } = null!;
        public CommitInfo LastChange { get; set; } = null!;
        public List<CommitterStat> TopCommitters { get; set; } = new();
    }

    public class CommitInfo
    {
        public string AuthorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string CommitId { get; set; } = string.Empty;
    }

    public class CommitterStat
    {
        public string Name { get; set; } = string.Empty;
        public int CommitCount { get; set; }
    }

    public class RepositoryAnalysisResult
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public List<FileStatistics> Files { get; set; } = new();
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
        public int TotalFilesAnalyzed { get; set; } 
    }
}