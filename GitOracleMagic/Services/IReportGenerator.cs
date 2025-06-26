// Services/IReportGenerator.cs
using GitRepoAnalyzer.Models;

namespace GitRepoAnalyzer.Services
{
    public interface IReportGenerator
    {
        void GenerateReport(RepositoryAnalysisResult result, int topFiles);
    }
}