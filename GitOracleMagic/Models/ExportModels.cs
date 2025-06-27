// Models/ExportModels.cs
namespace GitOracleMagic.Models
{
    public class ComprehensiveReportData
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public DateTime? SinceDate { get; set; }
        public DateTime? UntilDate { get; set; }
        
        // Analysis results
        public RepositoryAnalysisResult FileAnalysis { get; set; } = null!;
        public ContributorAnalysisResult ContributorAnalysis { get; set; } = null!;
        public ChangeCouplingResult CouplingAnalysis { get; set; } = null!;
        public TimelineResult TimelineAnalysis { get; set; } = null!;
        
        // Export metadata
        public string ExportFormat { get; set; } = "HTML";
        public string GeneratedBy { get; set; } = "Git Oracle Magic";
        public string Version { get; set; } = "1.0.0";
    }

    public class ExportConfiguration
    {
        public DateTime? SinceDate { get; set; }
        public DateTime? UntilDate { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public bool OpenAfterExport { get; set; } = true;
        
        // Default parameters for each analysis
        public int TopFiles { get; set; } = 20;
        public int TopContributors { get; set; } = 20;
        public int TopCouples { get; set; } = 20;
        public double MinCouplingStrength { get; set; } = 0.1;
        public TimePeriod TimelinePeriod { get; set; } = TimePeriod.Monthly;
        public int TimelineContributors { get; set; } = 20;
    }
}