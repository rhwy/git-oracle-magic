// Services/TimelineAnalyzer.cs
using GitOracleMagic.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitOracleMagic.Services
{
    public class TimelineAnalyzer : ITimelineAnalyzer
    {
        private readonly ILogger<TimelineAnalyzer> _logger;

        // Predefined colors for contributors - using Spectre.Console color names
        private readonly string[] _availableColors = 
        {
            "blue", "green", "red", "yellow", "magenta", "cyan", "orange1", 
            "purple", "lime", "pink1", "aqua", "gold1", "violet", "springgreen1",
            "orangered1", "deeppink1", "lightblue", "lightgreen", "lightcoral",
            "lightyellow", "lightpink", "lightcyan", "wheat1", "khaki1"
        };

        public TimelineAnalyzer(ILogger<TimelineAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<TimelineResult> AnalyzeTimelineAsync(
            string repoPath, 
            TimePeriod period, 
            int topContributors = 10,
            DateTime? sinceDate = null)
        {
            if (!Directory.Exists(repoPath))
            {
                throw new DirectoryNotFoundException($"Repository path '{repoPath}' does not exist");
            }

            _logger.LogInformation("Analyzing timeline in repository at {RepoPath} with {Period} periods", repoPath, period);
            
            if (sinceDate.HasValue)
            {
                _logger.LogInformation("Analyzing commits since {SinceDate}", sinceDate.Value.ToString("yyyy-MM-dd"));
            }

            var result = new TimelineResult
            {
                RepositoryPath = repoPath,
                Period = period,
                SinceDate = sinceDate
            };

            await Task.Run(() =>
            {
                using var repo = new Repository(repoPath);
                
                // Dictionary to track contributor total commits for filtering
                var contributorTotals = new Dictionary<string, int>();
                
                // Dictionary to track commits by period
                var periodCommits = new Dictionary<string, Dictionary<string, int>>();

                var commitCount = 0;
                _logger.LogInformation("Processing commits for timeline analysis...");

                // Filter commits by date if specified
                var commitsToAnalyze = sinceDate.HasValue 
                    ? repo.Commits.Where(c => c.Author.When.DateTime >= sinceDate.Value)
                    : repo.Commits;

                foreach (var commit in commitsToAnalyze)
                {
                    commitCount++;
                    if (commitCount % 100 == 0)
                    {
                        _logger.LogDebug("Processed {CommitCount} commits", commitCount);
                    }

                    var authorKey = $"{commit.Author.Name} <{commit.Author.Email}>";
                    var commitDate = commit.Author.When.DateTime;
                    var periodKey = GetPeriodKey(commitDate, period);

                    // Track total commits per contributor
                    if (!contributorTotals.ContainsKey(authorKey))
                    {
                        contributorTotals[authorKey] = 0;
                    }
                    contributorTotals[authorKey]++;

                    // Track commits by period and contributor
                    if (!periodCommits.ContainsKey(periodKey))
                    {
                        periodCommits[periodKey] = new Dictionary<string, int>();
                    }
                    
                    if (!periodCommits[periodKey].ContainsKey(authorKey))
                    {
                        periodCommits[periodKey][authorKey] = 0;
                    }
                    periodCommits[periodKey][authorKey]++;
                }

                _logger.LogInformation("Processed {CommitCount} commits from {ContributorCount} contributors", 
                    commitCount, contributorTotals.Count);

                // Get top contributors
                var topContributorsList = contributorTotals
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(topContributors)
                    .Select(kvp => kvp.Key)
                    .ToList();

                // Assign colors to top contributors
                var contributorColors = new Dictionary<string, string>();
                for (int i = 0; i < topContributorsList.Count; i++)
                {
                    var color = _availableColors[i % _availableColors.Length];
                    contributorColors[topContributorsList[i]] = color;
                }

                // Build timeline entries
                var timelineEntries = new List<TimelineEntry>();
                var maxCommitsInPeriod = 0;

                foreach (var periodData in periodCommits.OrderBy(kvp => kvp.Key))
                {
                    var periodKey = periodData.Key;
                    var contributors = periodData.Value;
                    
                    var (periodStart, periodEnd) = ParsePeriodKey(periodKey, period);
                    var periodLabel = FormatPeriodLabel(periodStart, period);
                    
                    var totalCommitsInPeriod = contributors.Values.Sum();
                    maxCommitsInPeriod = Math.Max(maxCommitsInPeriod, totalCommitsInPeriod);

                    var entry = new TimelineEntry
                    {
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                        PeriodLabel = periodLabel,
                        TotalCommits = totalCommitsInPeriod,
                        Contributors = new Dictionary<string, ContributorCommits>()
                    };

                    // Add contributor data for this period (only top contributors)
                    foreach (var contributor in contributors)
                    {
                        var authorKey = contributor.Key;
                        var commits = contributor.Value;
                        
                        if (topContributorsList.Contains(authorKey))
                        {
                            var name = ExtractName(authorKey);
                            var email = ExtractEmail(authorKey);
                            var percentage = (double)commits / totalCommitsInPeriod * 100;
                            
                            entry.Contributors[authorKey] = new ContributorCommits
                            {
                                Name = name,
                                Email = email,
                                CommitCount = commits,
                                Percentage = percentage,
                                Color = contributorColors[authorKey]
                            };
                        }
                    }

                    timelineEntries.Add(entry);
                }

                result.Timeline = timelineEntries;
                result.ContributorColors = contributorColors;
                result.TopContributors = topContributorsList;
                result.TotalCommitsAnalyzed = commitCount;
                result.TotalContributors = contributorTotals.Count;
                result.MaxCommitsInPeriod = maxCommitsInPeriod;

                _logger.LogInformation("Generated timeline with {PeriodCount} periods", timelineEntries.Count);
            });

            return result;
        }

        private static string GetPeriodKey(DateTime date, TimePeriod period)
        {
            return period switch
            {
                TimePeriod.Daily => date.ToString("yyyy-MM-dd"),
                TimePeriod.Weekly => GetWeekKey(date),
                TimePeriod.Monthly => date.ToString("yyyy-MM"),
                _ => throw new ArgumentException($"Unknown period: {period}")
            };
        }

        private static string GetWeekKey(DateTime date)
        {
            // Get the start of the week (Monday)
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            var weekStart = date.AddDays(-daysFromMonday);
            return weekStart.ToString("yyyy-MM-dd");
        }

        private static (DateTime start, DateTime end) ParsePeriodKey(string periodKey, TimePeriod period)
        {
            return period switch
            {
                TimePeriod.Daily => (DateTime.Parse(periodKey), DateTime.Parse(periodKey).AddDays(1).AddTicks(-1)),
                TimePeriod.Weekly => (DateTime.Parse(periodKey), DateTime.Parse(periodKey).AddDays(7).AddTicks(-1)),
                TimePeriod.Monthly => (DateTime.Parse(periodKey + "-01"), DateTime.Parse(periodKey + "-01").AddMonths(1).AddTicks(-1)),
                _ => throw new ArgumentException($"Unknown period: {period}")
            };
        }

        private static string FormatPeriodLabel(DateTime date, TimePeriod period)
        {
            return period switch
            {
                TimePeriod.Daily => date.ToString("yyyy-MM-dd"),
                TimePeriod.Weekly => $"{date:MMM dd} - {date.AddDays(6):MMM dd}",
                TimePeriod.Monthly => date.ToString("yyyy-MM"),
                _ => throw new ArgumentException($"Unknown period: {period}")
            };
        }

        private static string ExtractName(string authorKey)
        {
            var emailStart = authorKey.LastIndexOf(" <");
            return emailStart > 0 ? authorKey.Substring(0, emailStart) : authorKey;
        }

        private static string ExtractEmail(string authorKey)
        {
            var emailStart = authorKey.LastIndexOf(" <");
            var emailEnd = authorKey.LastIndexOf(">");
            if (emailStart > 0 && emailEnd > emailStart)
            {
                return authorKey.Substring(emailStart + 2, emailEnd - emailStart - 2);
            }
            return "";
        }
    }
}