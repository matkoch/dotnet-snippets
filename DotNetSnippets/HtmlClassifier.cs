using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

public class HtmlClassifier
{
    private readonly string[] _ignoredClassifications =
    {
        "punctuation"
    };

    public async Task<string> Load(string sourceCode, int[] highlightedLines = null)
    {
        var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        var workspace = new AdhocWorkspace(host);

        var sourceText = SourceText.From(sourceCode);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var compilation = CSharpCompilation
            .Create(nameof(HtmlClassifier))
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                    "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                    "System.Runtime.dll")))
            .AddSyntaxTrees(syntaxTree);

        var syntaxRoot = await syntaxTree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var classifiedSpans =
            Classifier.GetClassifiedSpans(semanticModel, new TextSpan(0, sourceText.Length), workspace);
        return Load(syntaxRoot, classifiedSpans, highlightedLines);
        
        // var projectInfo = ProjectInfo.Create(
        //         ProjectId.CreateNewId(),
        //         VersionStamp.Create(),
        //         nameof(HtmlClassifier),
        //         nameof(HtmlClassifier),
        //         LanguageNames.CSharp)
        //     .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        //         .WithNullableContextOptions(NullableContextOptions.Enable))
        //     .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
        //     .WithMetadataReferences(new[]
        //     {
        //         MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        //         MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!,
        //             "netstandard.dll")),
        //         MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!,
        //             "System.Runtime.dll"))
        //     });
        //
        // var project = new AdhocWorkspace(host)
        //     .CurrentSolution
        //     .AddProject(projectInfo)
        //     .AddDocument(DocumentId.CreateNewId(
        //             projectInfo.Id,
        //             nameof(HtmlClassifier) + ".cs"),
        //         nameof(HtmlClassifier) + ".cs",
        //         SourceText.From(csharpCode, Encoding.UTF8))
        //     .GetProject(projectInfo.Id);
        //
        // var document = project.Documents.Single(x => x.Name == nameof(HtmlClassifier) + ".cs");
        // return await Load(document, highlightedLines);
    }

    public async Task<string> Load(string projectFile, string sourceFile, int[] highlightedLines = null)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectFile);
        var document = project.Documents.Single(x => x.Name == sourceFile);

        var syntaxRoot = await document.GetSyntaxRootAsync();
        Trace.Assert(syntaxRoot != null);
        var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, syntaxRoot.FullSpan);
        return Load(syntaxRoot, classifiedSpans, highlightedLines);
    }

    private string Load(
        SyntaxNode syntaxRoot,
        IEnumerable<ClassifiedSpan> classifiedSpans,
        int[] highlightedLines)
    {
        var alternateEscape = true;
        var sourceText = new StringBuilder(syntaxRoot.GetText().ToString()
            .Replace("<", "\a")
            .Replace(">", "\b"));

        IEnumerable<(int Position, string Tag)> GetSpanTags(
            TextSpan textSpan,
            IEnumerable<string> classifications)
        {
            var classes = classifications
                .Where(x => !_ignoredClassifications.Contains(x))
                .Select(x => x.Replace(" ", "-").Replace("---", "-")).ToHashSet();

            // TODO: manipulate classifications
            if (classes.Contains("string-escape-character"))
            {
                classes.Add("string-escape-character" + (alternateEscape ? 1 : 2));
                alternateEscape = !alternateEscape;
            }

            if (classes.Count == 0)
                yield break;

            yield return (textSpan.Start, "<span class=\"" + string.Join(" ", classes) + "\">");
            yield return (textSpan.End, "</span>");
        }

        var spanTags = classifiedSpans
            .GroupBy(x => x.TextSpan)
            .SelectMany(x => GetSpanTags(x.Key, x.Select(x => x.ClassificationType)))
            .OrderByDescending(x => x.Position)
            .ThenBy(x => x.Tag.StartsWith("</span>")).ToList();

        foreach (var (position, tag) in spanTags)
            sourceText.Insert(position, tag);

        var sourceTextLines = sourceText.ToString()
            .Replace("\a", "&lt;")
            .Replace("\b", "&gt;")
            .Split(Environment.NewLine);
        if (highlightedLines != null)
        {
            for (var i = 0; i < sourceTextLines.Length; i++)
            {
                if (!highlightedLines.Contains(i + 1))
                    sourceTextLines[i] = "<span class=\"transparent\">" + sourceTextLines[i] + "</span>";
            }
        }

        var builder = new StringBuilder();

        builder.Append("<pre><code>");
        foreach (var (sourceTextLine, lineNumber) in sourceTextLines.Select((x, i) => (x, i)))
        {
            if (lineNumber + 1 < sourceTextLines.Length)
                builder.AppendLine(sourceTextLine);
            else
                builder.Append(sourceTextLine);
        }

        builder.Append("</code></pre>");

        return builder.ToString();
    }
}
