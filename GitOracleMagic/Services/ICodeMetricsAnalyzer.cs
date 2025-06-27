// Services/ICodeMetricsAnalyzer.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface ICodeMetricsAnalyzer
    {
        Task<CodeMetricsResult> AnalyzeCodeMetricsAsync(string repoPath, CodeMetricsConfiguration config);
        Task<bool> IsAnalyzerAvailableAsync(CodeMetricsAnalyzer analyzer);
        Task<string> GetAnalyzerVersionAsync(CodeMetricsAnalyzer analyzer);
    }
}
