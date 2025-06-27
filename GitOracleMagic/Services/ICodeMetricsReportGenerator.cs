// Services/ICodeMetricsReportGenerator.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface ICodeMetricsReportGenerator
    {
        void GenerateReport(CodeMetricsResult result, int topFiles);
    }
}