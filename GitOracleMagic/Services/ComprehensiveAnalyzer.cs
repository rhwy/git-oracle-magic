// Services/ComprehensiveAnalyzer.cs
using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;

namespace GitOracleMagic.Services
{
    public class ComprehensiveAnalyzer : IComprehensiveAnalyzer
    {
        private readonly IGitRepositoryAnalyzer _gitAnalyzer;
        private readonly IContributorAnalyzer _contributorAnalyzer;
        private readonly IChangeCouplingAnalyzer _couplingAnalyzer;
        private readonly ITimelineAnalyzer _timelineAnalyzer;
        private readonly ILogger<ComprehensiveAnalyzer> _logger;

        public ComprehensiveAnalyzer(
            IGitRepositoryAnalyzer gitAnalyzer,
            IContributorAnalyzer contributorAnalyzer,
            IChangeCouplingAnalyzer couplingAnalyzer,
            ITimelineAnalyzer timelineAnalyzer,
            ILogger<ComprehensiveAnalyzer> logger)
        {
            _gitAnalyzer = gitAnalyzer;
            _contributorAnalyzer = contributorAnalyzer;
            _couplingAnalyzer = couplingAnalyzer;
            _timelineAnalyzer = timelineAnalyzer;
            _logger = logger;
        }

        public async Task<ComprehensiveReportData> GenerateComprehensiveReportAsync(
            string repoPath, 
            ExportConfiguration config)
        {
            if (!Directory.Exists(repoPath))
            {
                throw new DirectoryNotFoundException($"Repository path '{repoPath}' does not exist");
            }

            _logger.LogInformation("Starting comprehensive analysis of repository at {RepoPath}", repoPath);

            var reportData = new ComprehensiveReportData
            {
                RepositoryPath = repoPath,
                RepositoryName = Path.GetFileName(Path.GetFullPath(repoPath)),
                SinceDate = config.SinceDate,
                UntilDate = config.UntilDate
            };

            // Run all analyses in parallel for better performance
            _logger.LogInformation("Running parallel analysis tasks...");

            var fileAnalysisTask = _gitAnalyzer.AnalyzeRepositoryAsync(repoPath, config.TopFiles);
            var contributorAnalysisTask = _contributorAnalyzer.AnalyzeContributorsAsync(repoPath, config.TopContributors);
            var couplingAnalysisTask = _couplingAnalyzer.AnalyzeChangeCouplingAsync(
                repoPath, config.TopCouples, config.SinceDate, config.MinCouplingStrength);
            var timelineAnalysisTask = _timelineAnalyzer.AnalyzeTimelineAsync(
                repoPath, config.TimelinePeriod, config.TimelineContributors, config.SinceDate);

            // Wait for all analyses to complete
            await Task.WhenAll(fileAnalysisTask, contributorAnalysisTask, couplingAnalysisTask, timelineAnalysisTask);

            // Collect results
            reportData.FileAnalysis = await fileAnalysisTask;
            reportData.ContributorAnalysis = await contributorAnalysisTask;
            reportData.CouplingAnalysis = await couplingAnalysisTask;
            reportData.TimelineAnalysis = await timelineAnalysisTask;

            _logger.LogInformation("Comprehensive analysis completed successfully");

            return reportData;
        }
    }
}