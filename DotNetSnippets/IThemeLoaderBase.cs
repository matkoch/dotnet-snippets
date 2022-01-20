using TextCopy;

namespace DotNetSnippets;

public interface IThemeLoaderBase
{
    string LoadFromFile(string file);
    string LoadFromResource(string resourceName);
}
