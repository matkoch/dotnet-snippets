using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using DotNetSnippets;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.MSBuild;

RiderThemeLoader.Load();

var projectFile = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Single();
var csharpFile = Environment.GetCommandLineArgs().ElementAt(1);
var highlightedLines = new int[0];

using var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectFile);
var document = project.Documents.Single(x => x.Name == csharpFile);

var syntaxRoot = await document.GetSyntaxRootAsync();
var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, syntaxRoot.FullSpan);

var sourceText = new StringBuilder(syntaxRoot.GetText().ToString().Replace("<", "\a").Replace(">", "\b"));
var classificationBySpans = classifiedSpans
    .GroupBy(x => x.TextSpan)
    .OrderByDescending(x => x.Key).ToList();

foreach (var classifications in classificationBySpans)
{
    var classes = classifications
        .Select(x => x.ClassificationType.Replace(" ", "-").Replace("---", "-"))
        .Where(x => x != "punctuation").ToHashSet();

    if (!classes.Any())
        continue;

    sourceText.Insert(classifications.Key.End, "</span>");
    sourceText.Insert(classifications.Key.Start, $"<span class=\"{string.Join(" ", classes)}\">");
}

var sourceTextLines = sourceText.ToString().Replace("\a", "&lt;").Replace("\b", "&gt;").Split(Environment.NewLine);
if (highlightedLines.Any())
{
    for (var i = 0; i < sourceTextLines.Length; i++)
    {
        if (!highlightedLines.Contains(i + 1))
            sourceTextLines[i] = "<span class=\"transparent\">" + sourceTextLines[i] + "</span>";
    }
}

Console.WriteLine("<pre><code>");
foreach (var sourceTextLine in sourceTextLines)
    Console.WriteLine(sourceTextLine);
Console.WriteLine("</code></pre>");
// var text = await ClipboardService.GetTextAsync();
// var options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, SourceCodeKind.Script);
// var syntaxTree = CSharpSyntaxTree.ParseText(text, options);
