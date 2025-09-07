using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using System.Text;
using CodeVision.Core.Entities;
using CodeVision.Core.Interfaces;

namespace CodeVision.Infrastructure.Services;

public class RoslynAnalyzerService : IRoslynAnalyzerService
{
    private readonly ILogger<RoslynAnalyzerService> _logger;

    public RoslynAnalyzerService(ILogger<RoslynAnalyzerService> logger)
    {
        _logger = logger;
    }

    public async Task<List<RoslynFinding>> AnalyzeCodeAsync(string codeContent, string fileName)
    {
        var findings = new List<RoslynFinding>();

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(codeContent, path: fileName);
            var compilation = CSharpCompilation.Create("TempAssembly")
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var diagnostics = compilation.GetDiagnostics();
            
            foreach (var diagnostic in diagnostics)
            {
                var finding = new RoslynFinding
                {
                    RuleId = diagnostic.Id,
                    Title = diagnostic.Descriptor.Title.ToString(),
                    Message = diagnostic.GetMessage(),
                    Severity = ConvertSeverity(diagnostic.Severity),
                    FilePath = fileName,
                    Category = diagnostic.Descriptor.Category
                };

                if (diagnostic.Location != Location.None)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    finding.LineNumber = lineSpan.StartLinePosition.Line + 1;
                    finding.ColumnNumber = lineSpan.StartLinePosition.Character + 1;
                    finding.CodeSnippet = GetCodeSnippet(codeContent, lineSpan.StartLinePosition.Line);
                }

                findings.Add(finding);
            }

            // Ek analizler ekle
            await AddCustomAnalysisAsync(syntaxTree, fileName, findings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Roslyn analysis: {FileName}", fileName);
        }

        return findings;
    }

    public async Task<List<RoslynFinding>> AnalyzePullRequestDiffAsync(string diffContent)
    {
        var findings = new List<RoslynFinding>();

        try
        {
            var changedFiles = ParseDiffContent(diffContent);

            foreach (var file in changedFiles)
            {
                if (IsCSharpFile(file.FileName))
                {
                    var fileFindings = await AnalyzeCodeAsync(file.Content, file.FileName);
                    findings.AddRange(fileFindings);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during diff analysis");
        }

        return findings;
    }

    public async Task<int> CalculateQualityScoreAsync(List<RoslynFinding> findings)
    {
        const int baseScore = 100;
        var penalty = 0;

        foreach (var finding in findings)
        {
            penalty += finding.Severity switch
            {
                Severity.Error => 10,
                Severity.Warning => 5,
                Severity.Info => 1,
                _ => 0
            };
        }

        var score = Math.Max(0, baseScore - penalty);
        return await Task.FromResult(score);
    }

    public async Task<RiskLevel> DetermineRiskLevelAsync(List<RoslynFinding> findings)
    {
        var errorCount = findings.Count(f => f.Severity == Severity.Error);
        var warningCount = findings.Count(f => f.Severity == Severity.Warning);

        var riskLevel = (errorCount, warningCount) switch
        {
            (>= 5, _) => RiskLevel.High,
            (>= 1, >= 10) => RiskLevel.High,
            (>= 1, _) => RiskLevel.Medium,
            (0, >= 15) => RiskLevel.Medium,
            (0, >= 5) => RiskLevel.Low,
            _ => RiskLevel.Low
        };

        return await Task.FromResult(riskLevel);
    }

    private static Severity ConvertSeverity(DiagnosticSeverity diagnosticSeverity)
    {
        return diagnosticSeverity switch
        {
            DiagnosticSeverity.Error => Severity.Error,
            DiagnosticSeverity.Warning => Severity.Warning,
            DiagnosticSeverity.Info => Severity.Info,
            DiagnosticSeverity.Hidden => Severity.Info,
            _ => Severity.Info
        };
    }

    private static string GetCodeSnippet(string codeContent, int lineNumber)
    {
        var lines = codeContent.Split('\n');
        if (lineNumber >= 0 && lineNumber < lines.Length)
        {
            return lines[lineNumber].Trim();
        }
        return string.Empty;
    }

    private async Task AddCustomAnalysisAsync(SyntaxTree syntaxTree, string fileName, List<RoslynFinding> findings)
    {
        try
        {
            var root = await syntaxTree.GetRootAsync();
            
            // Async/await yanlış kullanımları kontrol et
            CheckAsyncAwaitPatterns(root, fileName, findings);
            
            // Magic numbers kontrol et
            CheckMagicNumbers(root, fileName, findings);
            
            // Çok uzun metotları kontrol et
            CheckMethodLength(root, fileName, findings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during custom analyses: {FileName}", fileName);
        }
    }

    private static void CheckAsyncAwaitPatterns(SyntaxNode root, string fileName, List<RoslynFinding> findings)
    {
        // Async void metotları bul
        var asyncVoidMethods = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)) &&
                       m.ReturnType.ToString() == "void");

        foreach (var method in asyncVoidMethods)
        {
            var location = method.GetLocation().GetLineSpan();
            findings.Add(new RoslynFinding
            {
                RuleId = "CV0001",
                Title = "Async void usage",
                Message = "Use async Task instead of async void methods",
                Severity = Severity.Warning,
                FilePath = fileName,
                LineNumber = location.StartLinePosition.Line + 1,
                ColumnNumber = location.StartLinePosition.Character + 1,
                Category = "AsyncPatterns",
                SuggestedFix = $"async Task {method.Identifier.Text}(...)"
            });
        }
    }

    private static void CheckMagicNumbers(SyntaxNode root, string fileName, List<RoslynFinding> findings)
    {
        var literals = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>()
            .Where(l => l.Token.IsKind(SyntaxKind.NumericLiteralToken))
            .Where(l => !IsMagicNumberException(l.Token.ValueText));

        foreach (var literal in literals.Take(10)) // Çok fazla olmasın diye sınırla
        {
            var location = literal.GetLocation().GetLineSpan();
            findings.Add(new RoslynFinding
            {
                RuleId = "CV0002",
                Title = "Magic number",
                Message = $"Use a named constant instead of magic number '{literal.Token.ValueText}'",
                Severity = Severity.Info,
                FilePath = fileName,
                LineNumber = location.StartLinePosition.Line + 1,
                ColumnNumber = location.StartLinePosition.Character + 1,
                Category = "CodeQuality",
                SuggestedFix = $"const int MEANINGFUL_NAME = {literal.Token.ValueText};"
            });
        }
    }

    private static void CheckMethodLength(SyntaxNode root, string fileName, List<RoslynFinding> findings)
    {
        var methods = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            if (method.Body != null)
            {
                var lineCount = method.Body.Statements.Count;
                if (lineCount > 50) // 50 satırdan uzun metotlar
                {
                    var location = method.GetLocation().GetLineSpan();
                    findings.Add(new RoslynFinding
                    {
                        RuleId = "CV0003",
                        Title = "Çok uzun metot",
                        Message = $"Metot çok uzun ({lineCount} satır). Daha küçük parçalara bölün.",
                        Severity = Severity.Warning,
                        FilePath = fileName,
                        LineNumber = location.StartLinePosition.Line + 1,
                        ColumnNumber = location.StartLinePosition.Character + 1,
                        Category = "Maintainability"
                    });
                }
            }
        }
    }

    private static bool IsMagicNumberException(string value)
    {
        // Yaygın kullanılan sayıları magic number sayma
        return value is "0" or "1" or "-1" or "2" or "10" or "100";
    }

    private static bool IsCSharpFile(string fileName)
    {
        return fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    private List<ChangedFile> ParseDiffContent(string diffContent)
    {
        var files = new List<ChangedFile>();
        
        try
        {
            // Bu basit bir diff parser. Production'da daha kapsamlı olmalı
            var lines = diffContent.Split('\n');
            string? currentFile = null;
            var currentContent = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("diff --git"))
                {
                    // Önceki dosyayı kaydet
                    if (currentFile != null && currentContent.Length > 0)
                    {
                        files.Add(new ChangedFile { FileName = currentFile, Content = currentContent.ToString() });
                    }

                    // Yeni dosya
                    var parts = line.Split(' ');
                    if (parts.Length >= 4)
                    {
                        currentFile = parts[3].TrimStart('b', '/');
                        currentContent.Clear();
                    }
                }
                else if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    // Eklenen satırlar
                    currentContent.AppendLine(line[1..]);
                }
                else if (!line.StartsWith("-") && !line.StartsWith("@@") && !line.StartsWith("index"))
                {
                    // Değişmemiş satırlar
                    currentContent.AppendLine(line);
                }
            }

            // Son dosyayı kaydet
            if (currentFile != null && currentContent.Length > 0)
            {
                files.Add(new ChangedFile { FileName = currentFile, Content = currentContent.ToString() });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diff parsing hatası");
        }

        return files;
    }

    private class ChangedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
