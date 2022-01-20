using System;
using System.IO;
using System.Linq;
using DotNetSnippets;
using TextCopy;

var theme = new RiderThemeLoader().LoadFromResource("RiderDark.xml");
ClipboardService.SetText(theme);

var projectFile = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Single();
var sourceFile = Environment.GetCommandLineArgs().ElementAt(1);
var html = await new HtmlClassifier().Load(projectFile, sourceFile);
ClipboardService.SetText(html);

var html2 = await new HtmlClassifier().Load(@"
using System;
using System.IO;
using System.Linq;

class Program
{
    public static void Main()
    {
        Console.WriteLine(new []{1, 2, 3}.Select(x => x.ToString()));
        Console.WriteLine(""Hello World!"");
    }
}");
Console.WriteLine(html2);

// var text = await ClipboardService.GetTextAsync();
// var options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None, SourceCodeKind.Script);
// var syntaxTree = CSharpSyntaxTree.ParseText(text, options);
