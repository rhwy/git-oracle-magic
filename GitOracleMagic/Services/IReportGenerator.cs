// Services/IReportGenerator.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IReportGenerator
    {
        void GenerateReport(RepositoryAnalysisResult result, int topFiles);
    }
}