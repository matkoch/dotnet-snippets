using System;
using System.IO;
using System.Linq;
using DotNetSnippets;
using TextCopy;

var theme = new RiderThemeLoader().Load("RiderLight.xml");
ClipboardService.SetText(theme);

var projectFile = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Single();
var csharpFile = Environment.GetCommandLineArgs().ElementAt(1);
var html = await new HtmlClassifier().Load(projectFile, csharpFile);
ClipboardService.SetText(html);
// Console.WriteLine(html);

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
