// Services/IComprehensiveAnalyzer.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IComprehensiveAnalyzer
    {
        Task<ComprehensiveReportData> GenerateComprehensiveReportAsync(
            string repoPath, 
            ExportConfiguration config);
    }
}