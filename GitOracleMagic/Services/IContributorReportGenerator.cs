// Services/IContributorReportGenerator.cs
using GitRepoAnalyzer.Models;

namespace GitRepoAnalyzer.Services
{
    public interface IContributorReportGenerator
    {
        void GenerateReport(ContributorAnalysisResult result, int topContributors);
    }
}