// Services/IHtmlReportGenerator.cs
using GitOracleMagic.Models;

namespace GitOracleMagic.Services
{
    public interface IHtmlReportGenerator
    {
        Task<string> GenerateHtmlReportAsync(ComprehensiveReportData data, string outputPath);
        Task<string> GenerateHtmlReportAsync(ComprehensiveReportData data, string outputPath, string? customTemplatePath);
    }
}