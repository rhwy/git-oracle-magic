// Services/IGitRepositoryAnalyzer.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IGitRepositoryAnalyzer
    {
        Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repoPath, int topFiles);
    }
}