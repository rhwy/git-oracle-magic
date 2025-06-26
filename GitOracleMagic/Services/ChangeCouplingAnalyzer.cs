// Services/ChangeCouplingAnalyzer.cs

using GitOracleMagic.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace GitOracleMagic.Services
{
    public class ChangeCouplingAnalyzer : IChangeCouplingAnalyzer
    {
        private readonly ILogger<ChangeCouplingAnalyzer> _logger;

        public ChangeCouplingAnalyzer(ILogger<ChangeCouplingAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<ChangeCouplingResult> AnalyzeChangeCouplingAsync(
            string repoPath, 
            int topCouples, 
            DateTime? sinceDate = null, 
            double minimumCouplingStrength = 0.1)
        {
            if (!Directory.Exists(repoPath))
            {
                throw new DirectoryNotFoundException($"Repository path '{repoPath}' does not exist");
            }

            _logger.LogInformation("Analyzing change coupling in repository at {RepoPath}", repoPath);
            
            if (sinceDate.HasValue)
            {
                _logger.LogInformation("Analyzing commits since {SinceDate}", sinceDate.Value.ToString("yyyy-MM-dd"));
            }

            var result = new ChangeCouplingResult
            {
                RepositoryPath = repoPath,
                SinceDate = sinceDate,
                MinimumCouplingStrength = minimumCouplingStrength
            };

            await Task.Run(() =>
            {
                using var repo = new Repository(repoPath);
                
                // Dictionary to track which files changed in each commit
                var commitFileChanges = new Dictionary<string, List<string>>();
                
                // Dictionary to track commit dates
                var commitDates = new Dictionary<string, DateTime>();
                
                // Dictionary to track how often each file changes
                var fileChangeFrequency = new Dictionary<string, FileChangeFrequency>();

                var commitCount = 0;
                _logger.LogInformation("Processing commits for change coupling analysis...");

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

                    var filesInCommit = new List<string>();
                    
                    // Store commit date for later reference
                    commitDates[commit.Id.ToString()] = commit.Author.When.DateTime;

                    try
                    {
                        // Skip merge commits with multiple parents for cleaner analysis
                        if (commit.Parents.Count() <= 1)
                        {
                            var parent = commit.Parents.FirstOrDefault();
                            
                            if (parent != null)
                            {
                                // Get files changed in this commit
                                var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                                
                                foreach (var change in changes)
                                {
                                    var filePath = change.Path;
                                    filesInCommit.Add(filePath);

                                    // Track file change frequency
                                    if (!fileChangeFrequency.ContainsKey(filePath))
                                    {
                                        fileChangeFrequency[filePath] = new FileChangeFrequency
                                        {
                                            FilePath = filePath,
                                            ChangeCount = 0,
                                            CommitIds = new List<string>()
                                        };
                                    }

                                    fileChangeFrequency[filePath].ChangeCount++;
                                    fileChangeFrequency[filePath].CommitIds.Add(commit.Id.ToString());
                                }
                            }
                            else
                            {
                                // First commit - all files are "changed"
                                var changes = repo.Diff.Compare<TreeChanges>(null, commit.Tree);
                                foreach (var change in changes)
                                {
                                    var filePath = change.Path;
                                    filesInCommit.Add(filePath);

                                    if (!fileChangeFrequency.ContainsKey(filePath))
                                    {
                                        fileChangeFrequency[filePath] = new FileChangeFrequency
                                        {
                                            FilePath = filePath,
                                            ChangeCount = 0,
                                            CommitIds = new List<string>()
                                        };
                                    }

                                    fileChangeFrequency[filePath].ChangeCount++;
                                    fileChangeFrequency[filePath].CommitIds.Add(commit.Id.ToString());
                                }
                            }
                        }

                        // Store files that changed together in this commit
                        if (filesInCommit.Count > 1)
                        {
                            commitFileChanges[commit.Id.ToString()] = filesInCommit;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not analyze commit {CommitId}: {Error}", 
                            commit.Id.ToString()[..8], ex.Message);
                    }
                }

                _logger.LogInformation("Processed {CommitCount} commits with {FileCount} unique files", 
                    commitCount, fileChangeFrequency.Count);

                // Calculate coupling statistics
                var couplingStats = new Dictionary<string, FileCouplingStatistics>();

                foreach (var kvp in commitFileChanges)
                {
                    var commitId = kvp.Key;
                    var commitFiles = kvp.Value;
                    var commitDate = commitDates[commitId];

                    // Check all pairs of files that changed together
                    for (int i = 0; i < commitFiles.Count - 1; i++)
                    {
                        for (int j = i + 1; j < commitFiles.Count; j++)
                        {
                            var file1 = commitFiles[i];
                            var file2 = commitFiles[j];

                            // Create a consistent key for the pair (alphabetical order)
                            var key = string.Compare(file1, file2, StringComparison.Ordinal) < 0 
                                ? $"{file1}|{file2}" 
                                : $"{file2}|{file1}";

                            if (!couplingStats.ContainsKey(key))
                            {
                                couplingStats[key] = new FileCouplingStatistics
                                {
                                    FilePath1 = string.Compare(file1, file2, StringComparison.Ordinal) < 0 ? file1 : file2,
                                    FilePath2 = string.Compare(file1, file2, StringComparison.Ordinal) < 0 ? file2 : file1,
                                    CouplingCount = 0,
                                    SharedCommits = new List<string>(),
                                    FirstSharedCommit = DateTime.MaxValue, // Initialize to max so first real date becomes minimum
                                    LastSharedCommit = DateTime.MinValue   // Initialize to min so first real date becomes maximum
                                };
                            }

                            var coupling = couplingStats[key];
                            coupling.CouplingCount++;
                            coupling.SharedCommits.Add(commitId);
                            
                            // Update first and last shared commit dates
                            if (coupling.FirstSharedCommit == DateTime.MaxValue || commitDate < coupling.FirstSharedCommit)
                            {
                                coupling.FirstSharedCommit = commitDate;
                            }
                            if (coupling.LastSharedCommit == DateTime.MinValue || commitDate > coupling.LastSharedCommit)
                            {
                                coupling.LastSharedCommit = commitDate;
                            }
                        }
                    }
                }

                // Calculate coupling strength (percentage of commits where both files changed together)
                foreach (var coupling in couplingStats.Values)
                {
                    var file1Frequency = fileChangeFrequency.GetValueOrDefault(coupling.FilePath1)?.ChangeCount ?? 0;
                    var file2Frequency = fileChangeFrequency.GetValueOrDefault(coupling.FilePath2)?.ChangeCount ?? 0;
                    
                    if (file1Frequency > 0 && file2Frequency > 0)
                    {
                        // Coupling strength = shared changes / minimum(file1 changes, file2 changes)
                        // This represents: "What percentage of the less-frequently-changed file's commits involved both files?"
                        // Example: File A changes 100 times, File B changes 20 times, they change together 15 times
                        // Coupling strength = 15/20 = 75% (75% of File B's changes also involved File A)
                        var minChanges = Math.Min(file1Frequency, file2Frequency);
                        coupling.CouplingStrength = (double)coupling.CouplingCount / minChanges;
                    }

                    // Find shared commits
                    var file1Commits = fileChangeFrequency.GetValueOrDefault(coupling.FilePath1)?.CommitIds ?? new List<string>();
                    var file2Commits = fileChangeFrequency.GetValueOrDefault(coupling.FilePath2)?.CommitIds ?? new List<string>();
                    coupling.SharedCommits = file1Commits.Intersect(file2Commits).ToList();
                }

                // Filter by minimum coupling strength and sort by coupling count
                var topCouplings = couplingStats.Values
                    .Where(c => c.CouplingStrength >= minimumCouplingStrength)
                    .OrderByDescending(c => c.CouplingCount)
                    .ThenByDescending(c => c.CouplingStrength)
                    .Take(topCouples)
                    .ToList();

                result.CoupledFiles = topCouplings;
                result.FileFrequencies = fileChangeFrequency.Values
                    .OrderByDescending(f => f.ChangeCount)
                    .ToList();
                result.TotalCommitsAnalyzed = commitCount;
                result.TotalFilesAnalyzed = fileChangeFrequency.Count;

                _logger.LogInformation("Found {CouplingCount} file couples above {MinStrength}% coupling strength", 
                    topCouplings.Count, minimumCouplingStrength * 100);
            });

            return result;
        }
    }
}