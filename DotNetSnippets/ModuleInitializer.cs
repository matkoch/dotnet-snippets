using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace DotNetSnippets;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        MSBuildLocator.RegisterDefaults();
    }
}
