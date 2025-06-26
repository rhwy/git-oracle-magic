// Services/ContributorAnalyzer.cs
using GitRepoAnalyzer.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitRepoAnalyzer.Services
{
    public class ContributorAnalyzer : IContributorAnalyzer
    {
        private readonly ILogger<ContributorAnalyzer> _logger;

        public ContributorAnalyzer(ILogger<ContributorAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<ContributorAnalysisResult> AnalyzeContributorsAsync(string repoPath, int topContributors)
        {
            if (!Directory.Exists(repoPath))
            {
                throw new DirectoryNotFoundException($"Repository path '{repoPath}' does not exist");
            }

            _logger.LogInformation("Analyzing contributors in repository at {RepoPath}", repoPath);

            var result = new ContributorAnalysisResult
            {
                RepositoryPath = repoPath
            };

            await Task.Run(() =>
            {
                using var repo = new Repository(repoPath);
                var contributorStats = new Dictionary<string, ContributorStatistics>();

                var commitCount = 0;
                _logger.LogInformation("Processing commits for contributor analysis...");

                foreach (var commit in repo.Commits)
                {
                    commitCount++;
                    if (commitCount % 100 == 0)
                    {
                        _logger.LogDebug("Processed {CommitCount} commits", commitCount);
                    }

                    var authorKey = $"{commit.Author.Name} <{commit.Author.Email}>";
                    
                    if (!contributorStats.ContainsKey(authorKey))
                    {
                        contributorStats[authorKey] = new ContributorStatistics
                        {
                            Name = commit.Author.Name,
                            Email = commit.Author.Email,
                            FirstCommit = commit.Author.When.DateTime,
                            LastCommit = commit.Author.When.DateTime,
                            CommitCount = 0,
                            LinesAdded = 0,
                            LinesDeleted = 0
                        };
                    }

                    var stats = contributorStats[authorKey];
                    
                    // Update basic stats
                    stats.CommitCount++;
                    stats.LastCommit = commit.Author.When.DateTime;
                    
                    // Update first commit if this is earlier
                    if (commit.Author.When.DateTime < stats.FirstCommit)
                    {
                        stats.FirstCommit = commit.Author.When.DateTime;
                    }

                    // Calculate line changes
                    try
                    {
                        var parent = commit.Parents.FirstOrDefault();
                        if (parent != null)
                        {
                            var patch = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);
                            
                            foreach (var patchEntryChanges in patch)
                            {
                                stats.LinesAdded += patchEntryChanges.LinesAdded;
                                stats.LinesDeleted += patchEntryChanges.LinesDeleted;
                            }
                        }
                        else
                        {
                            // First commit - count all lines as added
                            var patch = repo.Diff.Compare<Patch>(null, commit.Tree);
                            foreach (var patchEntryChanges in patch)
                            {
                                stats.LinesAdded += patchEntryChanges.LinesAdded;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not calculate line changes for commit {CommitId}: {Error}", 
                            commit.Id.ToString()[..8], ex.Message);
                    }
                }

                _logger.LogInformation("Processed a total of {CommitCount} commits", commitCount);

                // Calculate average lines per week for each contributor
                foreach (var stats in contributorStats.Values)
                {
                    if (stats.ActiveWeeks > 0)
                    {
                        stats.AverageLinesPerWeek = stats.TotalLinesChanged / stats.ActiveWeeks;
                    }
                    else
                    {
                        // For contributors with all commits on the same day, use total lines
                        stats.AverageLinesPerWeek = stats.TotalLinesChanged;
                    }
                }

                // Sort by commit count and take top contributors
                var topContributorsList = contributorStats.Values
                    .OrderByDescending(c => c.CommitCount)
                    .ThenByDescending(c => c.TotalLinesChanged)
                    .Take(topContributors)
                    .ToList();

                result.Contributors = topContributorsList;
                result.TotalCommits = commitCount;
                result.TotalContributors = contributorStats.Count;

                _logger.LogInformation("Found {ContributorCount} total contributors", contributorStats.Count);
            });

            return result;
        }
    }
}