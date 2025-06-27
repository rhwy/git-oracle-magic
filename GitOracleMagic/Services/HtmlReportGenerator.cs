// Services/HtmlReportGenerator.cs
using GitOracleMagic.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace GitOracleMagic.Services
{
    public class HtmlReportGenerator : IHtmlReportGenerator
    {
        private readonly ILogger<HtmlReportGenerator> _logger;

        public HtmlReportGenerator(ILogger<HtmlReportGenerator> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateHtmlReportAsync(ComprehensiveReportData data, string outputPath)
        {
            return await GenerateHtmlReportAsync(data, outputPath, null);
        }

        public async Task<string> GenerateHtmlReportAsync(ComprehensiveReportData data, string outputPath, string? customTemplatePath)
        {
            _logger.LogInformation("Generating HTML report at {OutputPath}", outputPath);

            // Load template
            string template;
            if (!string.IsNullOrWhiteSpace(customTemplatePath) && File.Exists(customTemplatePath))
            {
                _logger.LogInformation("Using custom template from {TemplatePath}", customTemplatePath);
                template = await File.ReadAllTextAsync(customTemplatePath, Encoding.UTF8);
            }
            else
            {
                _logger.LogInformation("Using embedded template");
                template = await LoadEmbeddedTemplateAsync();
            }

            // Replace placeholders
            var html = await ReplaceTemplatePlaceholdersAsync(template, data);
            
            await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8);
            
            _logger.LogInformation("HTML report written successfully to {OutputPath}", outputPath);
            
            return Path.GetFullPath(outputPath);
        }

        private async Task<string> LoadEmbeddedTemplateAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // First try the logical name we set in csproj
            var resourceName = "GitOracleMagic.Templates.report_template.html";
            var stream = assembly.GetManifestResourceStream(resourceName);
            
            // If that doesn't work, try to find it dynamically
            if (stream == null)
            {
                var resourceNames = assembly.GetManifestResourceNames();
                _logger.LogDebug("Available embedded resources: {Resources}", string.Join(", ", resourceNames));
                
                resourceName = resourceNames.FirstOrDefault(name => name.EndsWith("report-template.html"));
                
                if (resourceName == null)
                {
                    throw new InvalidOperationException($"Could not find report template resource. Available resources: {string.Join(", ", resourceNames)}");
                }
                
                stream = assembly.GetManifestResourceStream(resourceName);
            }
            
            if (stream == null)
            {
                throw new InvalidOperationException($"Could not load embedded template resource: {resourceName}");
            }
            
            _logger.LogDebug("Successfully loaded template resource: {ResourceName}", resourceName);
            
            await using (stream)
            {
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
        }

        private async Task<string> ReplaceTemplatePlaceholdersAsync(string template, ComprehensiveReportData data)
        {
            await Task.CompletedTask; // Make method async for consistency

            var coupledFilesAboveThreshold = data.CouplingAnalysis.CoupledFiles.Count;
            var thresholdPercentage = (data.CouplingAnalysis.MinimumCouplingStrength * 100).ToString("F1");

            var replacements = new Dictionary<string, string>
            {
                {"{{REPOSITORY_NAME}}", EscapeHtml(data.RepositoryName)},
                {"{{REPOSITORY_PATH}}", EscapeHtml(data.RepositoryPath)},
                {"{{GENERATED_DATE}}", data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss")},
                {"{{GENERATED_BY}}", EscapeHtml(data.GeneratedBy)},
                {"{{VERSION}}", EscapeHtml(data.Version)},
                {"{{ANALYSIS_PERIOD}}", GetPeriodText(data)},
                {"{{TIMELINE_PERIOD}}", data.TimelineAnalysis.Period.ToString()},
                {"{{FILE_ANALYSIS_TITLE}}", $"{GetTotalFilesAnalyzed(data.FileAnalysis)} Analyzed Files (Top {data.FileAnalysis.Files.Count} Most Changed)"},
                {"{{COUPLING_ANALYSIS_TITLE}}", $"{coupledFilesAboveThreshold} Coupled Files Above {thresholdPercentage}% Coupling"},
                {"{{SUMMARY_CARDS}}", GenerateSummaryCards(data)},
                {"{{FILE_ANALYSIS_ROWS}}", GenerateFileAnalysisRows(data.FileAnalysis, data.RepositoryPath)},
                {"{{CONTRIBUTORS_ROWS}}", GenerateContributorsRows(data.ContributorAnalysis)},
                {"{{COUPLING_ROWS}}", GenerateCouplingRows(data.CouplingAnalysis, data.RepositoryPath)},
                {"{{TIMELINE_ROWS}}", GenerateTimelineRows(data.TimelineAnalysis)}
            };

            var result = template;
            foreach (var replacement in replacements)
            {
                result = result.Replace(replacement.Key, replacement.Value);
            }

            return result;
        }

        private static string GenerateSummaryCards(ComprehensiveReportData data)
        {
            var coupledFilesAboveThreshold = data.CouplingAnalysis.CoupledFiles.Count;
            var thresholdPercentage = (data.CouplingAnalysis.MinimumCouplingStrength * 100).ToString("F1");
            var totalFilesAnalyzed = GetTotalFilesAnalyzed(data.FileAnalysis);
            
            return $"""
                <div class="summary-card">
                    <span class="number">{totalFilesAnalyzed}</span>
                    <div class="label">Total Files Analyzed</div>
                </div>
                <div class="summary-card">
                    <span class="number">{data.ContributorAnalysis.TotalContributors}</span>
                    <div class="label">Total Contributors</div>
                </div>
                <div class="summary-card">
                    <span class="number">{data.ContributorAnalysis.TotalCommits:N0}</span>
                    <div class="label">Total Commits</div>
                </div>
                <div class="summary-card">
                    <span class="number">{coupledFilesAboveThreshold}</span>
                    <div class="label">Coupled Pairs (>{thresholdPercentage}%)</div>
                </div>
                <div class="summary-card">
                    <span class="number">{data.TimelineAnalysis.Timeline.Count}</span>
                    <div class="label">Time Periods</div>
                </div>
                <div class="summary-card">
                    <span class="number">{data.TimelineAnalysis.MaxCommitsInPeriod}</span>
                    <div class="label">Peak Activity</div>
                </div>
                """;
        }

        private static string CreateFileLink(string filePath, string repositoryPath)
        {
            // Build the full file path
            var fullPath = Path.Combine(repositoryPath, filePath);
            var normalizedPath = Path.GetFullPath(fullPath);
            
            // Create file URI for local file system
            var fileUri = new Uri(normalizedPath).AbsoluteUri;
            
            // Truncate the displayed path for readability
            var displayPath = TruncatePath(filePath, 50);
            
            // Create clickable link with title showing full path
            return $"""<a href="{fileUri}" target="_blank" title="{EscapeHtml(filePath)}" class="file-link">{EscapeHtml(displayPath)}</a>""";
        }

        private static int GetTotalFilesAnalyzed(RepositoryAnalysisResult fileAnalysis)
        {
            // Try to use TotalFilesAnalyzed property if it exists
            var propertyInfo = typeof(RepositoryAnalysisResult).GetProperty("TotalFilesAnalyzed");
            if (propertyInfo != null)
            {
                var value = propertyInfo.GetValue(fileAnalysis);
                if (value is int intValue)
                    return intValue;
            }
            
            // Fallback: estimate based on displayed files (not ideal but prevents crash)
            return fileAnalysis.Files.Count;
        }

        private static string GenerateFileAnalysisRows(RepositoryAnalysisResult fileAnalysis, string repositoryPath)
        {
            var sb = new StringBuilder();
            int rank = 1;
            
            foreach (var file in fileAnalysis.Files.Take(20))
            {
                var changeClass = file.ChangeCount switch
                {
                    > 100 => "high",
                    > 50 => "medium",
                    _ => "low"
                };

                // Format contributors with line breaks for better wrapping
                var contributors = string.Join("<br>", file.TopCommitters.Take(3).Select(c => $"{EscapeHtml(c.Name)} ({c.CommitCount})"));

                // Create file link
                var fileLink = CreateFileLink(file.Path, repositoryPath);

                sb.AppendLine($"""
                            <tr>
                                <td class="rank">{rank}</td>
                                <td>{fileLink}</td>
                                <td class="metric {changeClass}">{file.ChangeCount}</td>
                                <td>{file.FirstChange.Date:yyyy-MM-dd}</td>
                                <td>{file.LastChange.Date:yyyy-MM-dd}</td>
                                <td>{contributors}</td>
                            </tr>
                    """);
                rank++;
            }

            return sb.ToString();
        }

        private static string GenerateContributorsRows(ContributorAnalysisResult contributorAnalysis)
        {
            var sb = new StringBuilder();
            int rank = 1;
            
            foreach (var contributor in contributorAnalysis.Contributors.Take(20))
            {
                var commitClass = contributor.CommitCount switch
                {
                    > 500 => "high",
                    > 100 => "medium",
                    _ => "low"
                };

                var linesClass = contributor.TotalLinesChanged switch
                {
                    > 10000 => "high",
                    > 5000 => "medium",
                    _ => "low"
                };

                sb.AppendLine($"""
                            <tr>
                                <td class="rank">{rank}</td>
                                <td>{EscapeHtml(contributor.Name)}<br><small>{EscapeHtml(contributor.Email)}</small></td>
                                <td class="metric {commitClass}">{contributor.CommitCount:N0}</td>
                                <td class="metric low">{contributor.LinesAdded:N0}</td>
                                <td class="metric high">{contributor.LinesDeleted:N0}</td>
                                <td class="metric {linesClass}">{contributor.TotalLinesChanged:N0}</td>
                                <td class="metric">{contributor.AverageLinesPerCommit:F1}</td>
                                <td>{contributor.FirstCommit:yyyy-MM-dd}</td>
                                <td>{contributor.LastCommit:yyyy-MM-dd}</td>
                            </tr>
                    """);
                rank++;
            }

            return sb.ToString();
        }

        private static string GenerateCouplingRows(ChangeCouplingResult couplingAnalysis, string repositoryPath)
        {
            var sb = new StringBuilder();
            int rank = 1;
            
            foreach (var coupling in couplingAnalysis.CoupledFiles.Take(20))
            {
                var strengthClass = coupling.CouplingStrength switch
                {
                    >= 0.6 => "high",
                    >= 0.2 => "medium",
                    _ => "low"
                };

                var countClass = coupling.CouplingCount switch
                {
                    >= 20 => "high",
                    >= 10 => "medium",
                    _ => "low"
                };

                var lastSharedDate = coupling.LastSharedCommit == DateTime.MinValue 
                    ? "N/A" 
                    : coupling.LastSharedCommit.ToString("yyyy-MM-dd");

                // Create file links
                var file1Link = CreateFileLink(coupling.FilePath1, repositoryPath);
                var file2Link = CreateFileLink(coupling.FilePath2, repositoryPath);

                sb.AppendLine($"""
                            <tr>
                                <td class="rank">{rank}</td>
                                <td>{file1Link}</td>
                                <td>{file2Link}</td>
                                <td class="metric {countClass}">{coupling.CouplingCount}</td>
                                <td class="metric {strengthClass}">{coupling.CouplingStrength:P1}</td>
                                <td>{lastSharedDate}</td>
                            </tr>
                    """);
                rank++;
            }

            return sb.ToString();
        }

        private static string GenerateTimelineRows(TimelineResult timelineAnalysis)
        {
            var sb = new StringBuilder();
            var maxCommits = timelineAnalysis.MaxCommitsInPeriod;
            
            foreach (var period in timelineAnalysis.Timeline.Take(20))
            {
                var barWidth = maxCommits > 0 ? (period.TotalCommits * 100.0 / maxCommits) : 0;
                var commitClass = period.TotalCommits switch
                {
                    > 50 => "high",
                    > 20 => "medium",
                    _ => "low"
                };

                // Format contributors with line breaks for better wrapping
                var topContributors = string.Join("<br>", 
                    period.Contributors.Values
                        .OrderByDescending(c => c.CommitCount)
                        .Take(3)
                        .Select(c => $"{EscapeHtml(c.Name)} ({c.CommitCount})"));

                sb.AppendLine($"""
                            <tr>
                                <td>{EscapeHtml(period.PeriodLabel)}</td>
                                <td>
                                    <div class="timeline-bar" style="width: {barWidth:F1}%">
                                        <div class="timeline-text">{period.TotalCommits} commits</div>
                                    </div>
                                </td>
                                <td class="metric {commitClass}">{period.TotalCommits}</td>
                                <td>{topContributors}</td>
                            </tr>
                    """);
            }

            return sb.ToString();
        }

        private static string GetPeriodText(ComprehensiveReportData data)
        {
            if (data.SinceDate.HasValue && data.SinceDate != DateTime.MinValue && data.UntilDate.HasValue)
                return $"Analysis Period: {data.SinceDate.Value:yyyy-MM-dd} to {data.UntilDate.Value:yyyy-MM-dd}";
            if (data.SinceDate.HasValue && data.SinceDate != DateTime.MinValue)
                return $"Analysis Period: Since {data.SinceDate.Value:yyyy-MM-dd}";
            if (data.UntilDate.HasValue)
                return $"Analysis Period: Until {data.UntilDate.Value:yyyy-MM-dd}";
            return "Analysis Period: Full repository history";
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (path.Length <= maxLength)
                return path;

            var parts = path.Split('/', '\\');
            if (parts.Length <= 2)
                return path.Length > maxLength ? "..." + path.Substring(path.Length - maxLength + 3) : path;

            var filename = parts[^1];
            var result = filename;
            
            for (int i = parts.Length - 2; i >= 0; i--)
            {
                var newResult = parts[i] + "/" + result;
                if (newResult.Length > maxLength - 3)
                    break;
                result = newResult;
            }

            return result.Length < path.Length ? "..." + result : result;
        }

        private static string EscapeHtml(string text)
        {
            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&#39;");
        }
    }
}