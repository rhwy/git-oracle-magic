// Models/CodeMetricsModels.cs
namespace GitOracleMagic.Models
{
    public class FileCodeMetrics
    {
        public string FilePath { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int LinesOfCode { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int TokenCount { get; set; }
        public int ParameterCount { get; set; }
        public int FunctionCount { get; set; }
        public double AverageComplexityPerFunction { get; set; }
        public string ComplexityRating { get; set; } = string.Empty; // Low, Medium, High, Very High
        public List<FunctionMetrics> Functions { get; set; } = new();
    }

    public class FunctionMetrics
    {
        public string Name { get; set; } = string.Empty;
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int LinesOfCode { get; set; }
        public int ParameterCount { get; set; }
        public string ComplexityRating { get; set; } = string.Empty;
    }

    public class CodeMetricsResult
    {
        public string RepositoryPath { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
        public string AnalyzerTool { get; set; } = string.Empty;
        public string AnalyzerVersion { get; set; } = string.Empty;
        public List<FileCodeMetrics> FileMetrics { get; set; } = new();
        public CodeMetricsSummary Summary { get; set; } = new();
        public bool AnalysisSuccessful { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CodeMetricsSummary
    {
        public int TotalFiles { get; set; }
        public int TotalFunctions { get; set; }
        public int TotalLinesOfCode { get; set; }
        public double AverageCyclomaticComplexity { get; set; }
        public int MaxCyclomaticComplexity { get; set; }
        public string MostComplexFile { get; set; } = string.Empty;
        public string MostComplexFunction { get; set; } = string.Empty;
        public Dictionary<string, int> ComplexityDistribution { get; set; } = new(); // Low, Medium, High, Very High counts
        public Dictionary<string, int> LanguageDistribution { get; set; } = new();
    }

    public enum CodeMetricsAnalyzer
    {
        Lizard,
        SonarScanner,
        Custom
    }

    public class CodeMetricsConfiguration
    {
        public CodeMetricsAnalyzer Analyzer { get; set; } = CodeMetricsAnalyzer.Lizard;
        public string? CustomAnalyzerPath { get; set; }
        public string? CustomAnalyzerArgs { get; set; }
        public int TopComplexFiles { get; set; } = 20;
        public int TopComplexFunctions { get; set; } = 10;
        public bool IncludeFunctionDetails { get; set; } = true;
        public List<string> FileExtensions { get; set; } = new() { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h" };
        public List<string> ExcludePatterns { get; set; } = new() { "node_modules", "bin", "obj", ".git", "packages" };
    }
}