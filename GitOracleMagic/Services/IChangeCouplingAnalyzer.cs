// Services/IChangeCouplingAnalyzer.cs

using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IChangeCouplingAnalyzer
    {
        Task<ChangeCouplingResult> AnalyzeChangeCouplingAsync(
            string repoPath, 
            int topCouples, 
            DateTime? sinceDate = null, 
            double minimumCouplingStrength = 0.1);
    }
}