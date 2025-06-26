// Services/IGitRepositoryAnalyzer.cs
using GitRepoAnalyzer.Models;

namespace GitRepoAnalyzer.Services
{
    public interface IGitRepositoryAnalyzer
    {
        Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repoPath, int topFiles);
    }
}