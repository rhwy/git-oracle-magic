// Services/ICouplingReportGenerator.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface ICouplingReportGenerator
    {
        void GenerateReport(ChangeCouplingResult result, int topCouples);
    }
}