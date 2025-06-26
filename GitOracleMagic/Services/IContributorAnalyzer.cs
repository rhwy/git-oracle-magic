// Services/IContributorAnalyzer.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IContributorAnalyzer
    {
        Task<ContributorAnalysisResult> AnalyzeContributorsAsync(string repoPath, int topContributors);
    }
}