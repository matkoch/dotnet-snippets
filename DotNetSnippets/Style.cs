namespace DotNetSnippets;

record Style(string Foreground, string Background, FontType FontType, string EffectColor, EffectTypes EffectType);

enum EffectTypes
{
    None,
    Underscored,
    BoldUnderscored,
    Underwaved,
    Bordered,
    Strikeout,
    DottedLine
}

enum FontType
{
    Regular,
    Bold,
    Italic,
    BoldItalic
}
