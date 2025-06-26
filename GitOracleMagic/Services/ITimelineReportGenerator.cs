// Services/ITimelineReportGenerator.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface ITimelineReportGenerator
    {
        void GenerateReport(TimelineResult result);
    }
}