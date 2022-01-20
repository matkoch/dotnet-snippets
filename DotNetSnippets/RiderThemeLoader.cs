using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.Classification;

namespace DotNetSnippets;

public class RiderThemeLoader : IThemeLoaderBase
{
    private readonly Dictionary<string, string[]> _classificationMappings = new()
    {
        // Keywords
        [ClassificationTypeNames.Keyword] = new[] { "DEFAULT_KEYWORD" },
        [ClassificationTypeNames.ControlKeyword] = new[] { "DEFAULT_KEYWORD" },
        [ClassificationTypeNames.PreprocessorKeyword] = new[] { "DEFAULT_KEYWORD" },

        // Types
        [ClassificationTypeNames.NamespaceName] = new[] { "ReSharper.NAMESPACE_IDENTIFIER" },
        [ClassificationTypeNames.ClassName] = new[] { "ReSharper.CLASS_IDENTIFIER", "DEFAULT_CLASS_NAME" },
        [ClassificationTypeNames.StructName] = new[] { "ReSharper.STRUCT_IDENTIFIER", "DEFAULT_STRUCT_NAME" },
        [ClassificationTypeNames.RecordClassName] = new[] { "ReSharper.RECORD_IDENTIFIER", "ReSharper.CLASS_IDENTIFIER", "DEFAULT_CLASS_NAME" },
        [ClassificationTypeNames.RecordStructName] = new[] { "ReSharper.RECORD_STRUCT_IDENTIFIER", "ReSharper.STRUCT_IDENTIFIER", "DEFAULT_STRUCT_NAME" },
        [ClassificationTypeNames.InterfaceName] = new[] { "ReSharper.INTERFACE_IDENTIFIER", "DEFAULT_INTERFACE_NAME" },
        [ClassificationTypeNames.TypeParameterName] = new[] { "ReSharper.TYPE_PARAMETER_IDENTIFIER" },
        [ClassificationTypeNames.DelegateName] = new[] { "ReSharper.DELEGATE_IDENTIFIER", "DEFAULT_DELEGATE_NAME" },
        [ClassificationTypeNames.EnumName] = new[] { "ReSharper.ENUM_IDENTIFIER" },
        [ClassificationTypeNames.EventName] = new[] { "ReSharper.EVENT_IDENTIFIER", "DEFAULT_INSTANCE_FIELD" },

        // Properties & variables
        [ClassificationTypeNames.FieldName] = new[] { "DEFAULT_INSTANCE_FIELD" },
        [ClassificationTypeNames.PropertyName] = new[] { "DEFAULT_INSTANCE_FIELD" },
        [ClassificationTypeNames.EnumMemberName] = new[] { "ReSharper.ENUM_IDENTIFIER" },
        [ClassificationTypeNames.ConstantName] = new[] { "DEFAULT_CONSTANT" },
        [ClassificationTypeNames.ParameterName] = new[] { "DEFAULT_PARAMETER" },
        [ClassificationTypeNames.LocalName] = new[] { "DEFAULT_LOCAL_VARIABLE" },
        ["reassigned variable"] = new[] { "DEFAULT_REASSIGNED_LOCAL_VARIABLE" },

        // Methods
        [ClassificationTypeNames.MethodName] = new[] { "DEFAULT_INSTANCE_METHOD" },
        [ClassificationTypeNames.ExtensionMethodName] = new[] { "ReSharper.EXTENSION_METHOD_IDENTIFIER" },

        // Numbers
        [ClassificationTypeNames.NumericLiteral] = new[] { "DEFAULT_NUMBER" },

        // Strings
        [ClassificationTypeNames.StringLiteral] = new[] { "DEFAULT_STRING" },
        [ClassificationTypeNames.VerbatimStringLiteral] = new[] { "DEFAULT_STRING" },
        [ClassificationTypeNames.StringEscapeCharacter] = new[] { "DEFAULT_VALID_STRING_ESCAPE" },
        [ClassificationTypeNames.StringEscapeCharacter + 1] = new[] { "DEFAULT_VALID_STRING_ESCAPE" },
        [ClassificationTypeNames.StringEscapeCharacter + 2] = new[] { "ReSharper.STRING_ESCAPE_CHARACTER_2" },

        // Comments
        [ClassificationTypeNames.Comment] = new[] { "DEFAULT_LINE_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentText] = new[] { "DEFAULT_DOC_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentDelimiter] = new[] { "DEFAULT_DOC_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentName] = new[] { "DEFAULT_DOC_COMMENT_TAG", "DEFAULT_DOC_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentAttributeName] = new[] { "DEFAULT_DOC_COMMENT_TAG", "DEFAULT_DOC_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = new[] { "DEFAULT_DOC_COMMENT_TAG_VALUE", "DEFAULT_DOC_COMMENT" },
        [ClassificationTypeNames.XmlDocCommentAttributeValue] = new[] { "DEFAULT_DOC_COMMENT_TAG_VALUE", "DEFAULT_DOC_COMMENT" },
    };

    public string LoadFromResource(string resourceName)
    {
        var assembly = typeof(Program).Assembly;
        var resourceStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}");
        Trace.Assert(resourceStream != null);
        var reader = new StreamReader(resourceStream);
        return Load(reader.ReadToEnd());
    }

    public string LoadFromFile(string file)
    {
        return Load(File.ReadAllText(file));
    }

    private string Load(string content)
    {
        var theme = XDocument.Parse(content).XPathSelectElements("//scheme/attributes/option").ToList();

        Style GetStyle(string styleName)
        {
            var style = theme.FirstOrDefault(x => x.Attribute("name")?.Value == styleName);
            if (style == null)
                return null;

            var options = style.Element("value")?.Elements("option").ToList() ?? new List<XElement>();

            string GetOption(string optionName)
                => options.SingleOrDefault(x => x.Attribute("name")?.Value == optionName)?.Attribute("value")?.Value;

            return new Style(
                GetOption("FOREGROUND"),
                GetOption("BACKGROUND"),
                GetOption("FONT_TYPE") is {} fontType ? (FontType) int.Parse(fontType) : FontType.Regular,
                GetOption("EFFECT_COLOR"),
                GetOption("EFFECT_TYPE") is { } effectType ? (EffectTypes)int.Parse(effectType) : EffectTypes.None);
        }

        var builder = new StringBuilder();
        foreach (var (classification, styles) in _classificationMappings)
        {
            var style = styles.Select(GetStyle).First(x => x != null);
            builder.Append(".");
            builder.Append(classification.Replace(" ", "-").Replace("---", "-"));
            builder.Append(" { ");

            if (style.Foreground != null)
                builder.Append("color: #" + style.Foreground + "; ");

            if (style.Background != null)
                builder.Append("background-color: #" + style.Background + "; ");

            if (style.FontType is FontType.Italic or FontType.BoldItalic)
                builder.Append("font-style: italic; ");

            if (style.FontType is FontType.Bold or FontType.BoldItalic)
                builder.Append("font-weight: bold; ");

            if (style.EffectColor != null)
                builder.Append("border-color: #" + style.EffectColor + "; ");

            if (style.EffectType is EffectTypes.Underscored)
                builder.Append("text-decoration: underline; ");

            if (style.EffectType is EffectTypes.Strikeout)
                builder.Append("text-decoration: line-through; ");

            if (style.EffectType is EffectTypes.Bordered)
                builder.Append("border-style: solid; ");

            builder.Append("}");
            builder.AppendLine();
        }

        return builder.ToString();
    }
    
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
}
