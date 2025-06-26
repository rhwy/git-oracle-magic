// Services/ITimelineAnalyzer.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface ITimelineAnalyzer
    {
        Task<TimelineResult> AnalyzeTimelineAsync(
            string repoPath, 
            TimePeriod period, 
            int topContributors = 10,
            DateTime? sinceDate = null);
    }
}