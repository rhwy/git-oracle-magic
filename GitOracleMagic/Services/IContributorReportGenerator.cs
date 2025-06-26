// Services/IContributorReportGenerator.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IContributorReportGenerator
    {
        void GenerateReport(ContributorAnalysisResult result, int topContributors);
    }
}