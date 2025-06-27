# Git Oracle Magic 🔮

A powerful .NET CLI tool for analyzing Git repositories to uncover insights about file changes and contributor activity. Built with modern .NET practices, beautiful console output, and comprehensive logging.

## Features

### 📊 File Analysis (`analyze` command)
- Find the most changed files in your repository
- View first and last change dates for each file
- Identify top 3 contributors for each file
- Beautiful tabular output with color-coded metrics

### 🔗 Change Coupling Analysis (`coupling` command)
- Identify files that frequently change together
- Coupling strength analysis with percentage metrics
- Configurable time periods and minimum coupling thresholds
- Most frequently changed files overview
- Architectural insights for refactoring decisions

### 📈 Timeline Visualization (`timeline` command)
- Visual commit timeline with colored bars showing contributor proportions
- Multiple time periods: daily, weekly, monthly
- Top contributor filtering with color-coded legend
- Rich visualization using Spectre.Console colors
- Activity pattern analysis and peak identification

### 🎮 Interactive Mode (`interactive` command)
- User-friendly guided interface with menus
- Step-by-step parameter configuration
- All commands available through interactive prompts
- Perfect for exploring repository insights
- Clean, intuitive navigation with quit option

### 📋 HTML Export (`export` command)
- Comprehensive HTML report combining all analyses
- Beautiful, professional styling with gradients and responsive design
- Executive summary with key metrics
- All analysis tables with color-coded data
- Timeline visualization with activity bars
- Shareable reports for teams and stakeholders
- Comprehensive contributor statistics
- Commit counts and activity periods
- Lines added, deleted, and total changes
- Average lines per commit and per week
- Activity-based color coding

## Installation

### Prerequisites
- .NET 8.0 or later (for building)
- Git repository to analyze

### Building Single-File Executable

Choose your platform and run the appropriate command:

```bash
# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./dist

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./dist

# Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./dist

# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./dist
```

The executable will be created as `./dist/gom` (or `gom.exe` on Windows). Copy it to your PATH for global access:

```bash
# macOS/Linux
sudo cp ./dist/gom /usr/local/bin/
# or to user bin
cp ./dist/gom ~/bin/

# Windows
copy dist\gom.exe C:\Windows\System32\
# or add dist folder to PATH
```

## Usage

### 🎮 Interactive Mode (Recommended)
The easiest way to use Git Oracle Magic:

```bash
# Interactive mode in current directory
gom -i

# Interactive mode for specific repository
gom -i /path/to/your/repo

# Alternative syntax
gom interactive .
gom interactive /path/to/repo
```

The interactive mode provides:
- ✨ Beautiful guided menus
- 🔧 Step-by-step parameter configuration
- 📋 All commands available with explanations
- ❌ Easy quit option to exit

### 📟 Direct Command Usage

### File Analysis
Analyze the most changed files in a repository:

```bash
# Analyze current directory (default: top 10 files)
dotnet run -- analyze

# Analyze specific repository
dotnet run -- analyze --path /path/to/your/repo

# Show top 20 files with verbose logging
dotnet run -- analyze --path /path/to/repo --top 20 --verbose

# Short form
dotnet run -- analyze -p /path/to/repo -t 15 -v
```

### Contributor Analysis
Analyze contributor statistics:

```bash
# Analyze current directory (default: top 10 contributors)
dotnet run -- contributors

# Analyze specific repository
dotnet run -- contributors --path /path/to/your/repo

# Show top 15 contributors
dotnet run -- contributors --path /path/to/repo --top 15

# Short form with verbose logging
dotnet run -- contributors -p /path/to/repo -t 20 -v
```

### Timeline Visualization
Show commit activity over time with beautiful contributor visualizations:

```bash
# Monthly timeline (default: top 10 contributors)
dotnet run -- timeline

# Weekly timeline with more contributors
dotnet run -- timeline --period weekly --top 15

# Daily timeline for recent activity
dotnet run -- timeline --period daily --since 2024-01-01

# Analyze specific repository
dotnet run -- timeline --path /path/to/repo --period monthly

# Short form with verbose logging
dotnet run -- timeline -p /path/to/repo --period w -t 20 -s 2023-06-01 -v
```

### After Installation
Once the `gom` executable is in your PATH:

```bash
# Interactive mode (easiest)
gom -i                    # Current directory
gom -i /path/to/repo      # Specific repository

# Direct commands
gom analyze --top 20
gom contributors --top 15  
gom coupling --since 2023-01-01
gom timeline --period weekly

# Get help
gom --help
gom analyze --help
```
- `-p, --path <path>`: Path to the Git repository (default: current directory)
- `-t, --top <number>`: Number of top coupled file pairs to display (default: 15)
- `-s, --since <date>`: Only analyze commits since this date (YYYY-MM-DD format)
- `-m, --min-strength <value>`: Minimum coupling strength percentage (0.0-1.0, default: 0.1)
- `-v, --verbose`: Enable verbose logging to console
- `-h, --help`: Show help information
- `-p, --path <path>`: Path to the Git repository (default: current directory)
- `-t, --top <number>`: Number of top results to display (default: 10)
- `-v, --verbose`: Enable verbose logging to console (logs always written to file)
- `-h, --help`: Show help information

## Sample Output

### File Analysis
```
── Most Changed Files Report ──────────────────────────────────────────────────────

╭─Repository Information─────────────────────────╮
│ Repository: /Users/username/project            │
│ Analysis Date: 2025-06-26 01:15:32             │
│ Files Analyzed: 15                             │
╰────────────────────────────────────────────────╯

╭──────┬─────────────────────────┬─────────┬──────────────┬──────────────┬─────────────────────╮
│ Rank │ File Path               │ Changes │ First Change │ Last Change  │ Top Contributors    │
├──────┼─────────────────────────┼─────────┼──────────────┼──────────────┼─────────────────────┤
│ 1    │ src/main.rs             │ 47      │ 2023-01-15   │ 2025-06-20   │ alice: 25 commits   │
│      │                         │         │ by alice     │ by bob       │ bob: 15 commits     │
│      │                         │         │              │              │ charlie: 7 commits  │
╰──────┴─────────────────────────┴─────────┴──────────────┴──────────────┴─────────────────────╯
```

### Change Coupling Analysis
```
── Change Coupling Analysis Report ────────────────────────────────────────────────

╭─Analysis Information───────────────────────────╮
│ Repository: /Users/username/project            │
│ Analysis Date: 2025-06-26 01:25:42             │
│ Analysis Period: Since: 2023-01-01             │
│ Commits Analyzed: 847                          │
│ Files Analyzed: 234                            │
│ Min Coupling Strength: 10.0%                   │
╰────────────────────────────────────────────────╯

╭──────┬─────────────────────────┬─────────────────────────┬─────────────────┬─────────────────────┬─────────────────────╮
│ Rank │ File 1                  │ File 2                  │ Coupled Changes │ Coupling Strength   │ Last Shared Change  │
├──────┼─────────────────────────┼─────────────────────────┼─────────────────┼─────────────────────┼─────────────────────┤
│ 1    │ src/components/user.ts  │ src/types/user.ts       │ 23              │ 85.2%               │ 2025-06-20          │
│ 2    │ src/api/auth.ts         │ src/middleware/auth.ts  │ 18              │ 72.0%               │ 2025-06-15          │
╰──────┴─────────────────────────┴─────────────────────────┴─────────────────┴─────────────────────┴─────────────────────╯
```
```
╭─Repository Information─────────────────────────╮
│ Repository: /Users/username/project            │
│ Analysis Date: 2025-06-26 01:20:15             │
│ Total Contributors: 23                         │
│ Total Commits: 1,247                           │
╰────────────────────────────────────────────────╯

╭──────┬─────────────────────┬─────────┬──────────────┬──────────────┬─────────┬─────────┬─────────────┬─────────────┬─────────────╮
│ Rank │ Name                │ Commits │ First Commit │ Last Commit  │ Lines + │ Lines - │ Total Lines │ Avg/Commit  │ Avg/Week    │
├──────┼─────────────────────┼─────────┼──────────────┼──────────────┼─────────┼─────────┼─────────────┼─────────────┼─────────────┤
│ 1    │ Alice Developer     │ 342     │ 2023-01-15   │ 2025-06-20   │ 15,234  │ 8,123   │ 23,357      │ 68.3        │ 1,247.2     │
│      │ alice@company.com   │         │              │              │         │         │             │             │             │
╰──────┴─────────────────────┴─────────┴──────────────┴──────────────┴─────────┴─────────┴─────────────┴─────────────┴─────────────╯
```

## Logging

Git Oracle Magic uses structured logging with Serilog:

- **File Logging**: Always enabled, logs saved to `logs/git-oracle-magic-{timestamp}.log`
- **Console Logging**: Only enabled with `--verbose` flag to keep output clean
- **Log Levels**: Information and above by default

## Architecture

Built with modern .NET practices:

- **Dependency Injection**: Full DI container with service registration
- **Separation of Concerns**: Clear separation between analysis, reporting, and command handling
- **Async/Await**: Proper asynchronous patterns for I/O operations
- **Spectre.Console**: Beautiful CLI with progress bars, tables, and colors
- **LibGit2Sharp**: Robust Git repository analysis

### Project Structure
```
GitRepoAnalyzer/
├── Commands/           # CLI command definitions
├── Models/             # Data models and DTOs
├── Services/           # Business logic and analysis
├── Infrastructure/     # DI and framework setup
└── Program.cs          # Application entry point
```

## Dependencies

- **LibGit2Sharp** (0.28.0): Git repository operations
- **Spectre.Console.Cli** (0.47.0): Beautiful CLI framework
- **Serilog** (8.0.0): Structured logging
- **Microsoft.Extensions.Hosting** (8.0.0): Modern .NET hosting

## Performance Considerations

- **Large Repositories**: Analysis time scales with repository size and history depth
- **Memory Usage**: Optimized for memory efficiency with streaming operations
- **Progress Indicators**: Visual feedback for long-running operations
- **Logging**: Detailed progress logging for troubleshooting

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the Apache License 2.0 - see the LICENSE file for details.

## Acknowledgments

- **LibGit2Sharp** team for the excellent Git library
- **Spectre.Console** team for the beautiful CLI framework
- **Serilog** team for structured logging
- The .NET community for continuous innovation

---

*Git Oracle Magic - Unveiling the secrets hidden in your Git history* 🔮✨