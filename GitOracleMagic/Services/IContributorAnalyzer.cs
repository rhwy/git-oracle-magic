// Services/IContributorAnalyzer.cs
using GitRepoAnalyzer.Models;

namespace GitRepoAnalyzer.Services
{
    public interface IContributorAnalyzer
    {
        Task<ContributorAnalysisResult> AnalyzeContributorsAsync(string repoPath, int topContributors);
    }
}