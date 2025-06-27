// Services/GitRepositoryAnalyzer.cs

using GitOracleMagic.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitOracleMagic.Services
{
    public class GitRepositoryAnalyzer : IGitRepositoryAnalyzer
    {
        private readonly ILogger<GitRepositoryAnalyzer> _logger;

        public GitRepositoryAnalyzer(ILogger<GitRepositoryAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<RepositoryAnalysisResult> AnalyzeRepositoryAsync(string repoPath, int topFiles)
        {
            if (!Directory.Exists(repoPath))
            {
                throw new DirectoryNotFoundException($"Repository path '{repoPath}' does not exist");
            }

            _logger.LogInformation("Analyzing repository at {RepoPath}", repoPath);

            var result = new RepositoryAnalysisResult
            {
                RepositoryPath = repoPath
            };

            // This would block the thread for large repositories
            // For a real app, consider making the LibGit2Sharp calls asynchronous
            // or using a background service
            await Task.Run(() =>
            {
                using var repo = new Repository(repoPath);
                var fileStats = new Dictionary<string, (
                    int ChangeCount,
                    Commit FirstChange,
                    Commit LastChange,
                    Dictionary<string, int> CommitterStats
                )>();

                var commitCount = 0;
                _logger.LogInformation("Processing commits...");

                foreach (var commit in repo.Commits)
                {
                    commitCount++;
                    if (commitCount % 100 == 0)
                    {
                        _logger.LogDebug("Processed {CommitCount} commits", commitCount);
                    }

                    // Skip merge commits with multiple parents
                    if (commit.Parents.Count() <= 1)
                    {
                        var parent = commit.Parents.FirstOrDefault();
                        
                        if (parent != null)
                        {
                            // Compare this commit with its parent to find changes
                            foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree))
                            {
                                string path = change.Path;
                                
                                if (!fileStats.ContainsKey(path))
                                {
                                    fileStats[path] = (
                                        ChangeCount: 0,
                                        FirstChange: commit,
                                        LastChange: commit,
                                        CommitterStats: new Dictionary<string, int>()
                                    );
                                }

                                var stats = fileStats[path];
                                var (changeCount, firstChange, _, committerStats) = stats;
                                
                                // Update statistics
                                fileStats[path] = (
                                    ChangeCount: changeCount + 1,
                                    FirstChange: firstChange, // Keep the first change as is
                                    LastChange: commit, // Update to the latest commit
                                    CommitterStats: committerStats
                                );

                                // Update committer stats
                                string committer = commit.Committer.Name;
                                if (!committerStats.ContainsKey(committer))
                                {
                                    committerStats[committer] = 0;
                                }
                                committerStats[committer]++;
                            }
                        }
                    }
                }

                _logger.LogInformation("Processed a total of {CommitCount} commits", commitCount);
                _logger.LogInformation("Found {FileCount} files with changes", fileStats.Count);

                var totalFilesCount = fileStats.Count;
                
                // Convert to model and sort by change count
                var topChangedFiles = fileStats
                    .Select(kvp => new FileStatistics
                    {
                        Path = kvp.Key,
                        ChangeCount = kvp.Value.ChangeCount,
                        FirstChange = new CommitInfo
                        {
                            AuthorName = kvp.Value.FirstChange.Author.Name,
                            Date = kvp.Value.FirstChange.Author.When.DateTime,
                            CommitId = kvp.Value.FirstChange.Id.ToString()
                        },
                        LastChange = new CommitInfo
                        {
                            AuthorName = kvp.Value.LastChange.Author.Name,
                            Date = kvp.Value.LastChange.Author.When.DateTime,
                            CommitId = kvp.Value.LastChange.Id.ToString()
                        },
                        TopCommitters = kvp.Value.CommitterStats
                            .OrderByDescending(c => c.Value)
                            .Take(3)
                            .Select(c => new CommitterStat
                            {
                                Name = c.Key,
                                CommitCount = c.Value
                            })
                            .ToList()
                    })
                    .OrderByDescending(stats => stats.ChangeCount)
                    .Take(topFiles)
                    .ToList();

                result.Files = topChangedFiles;
                result.TotalFilesAnalyzed = totalFilesCount;
            });

            return result;
        } 
    }
}