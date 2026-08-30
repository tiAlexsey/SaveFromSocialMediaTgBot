using System.Text;
using System.Text.Json;

namespace SaveFromSocialMediaTgBot.Data.Extensions;

internal static class JsonCorrection
{
    public static bool TryCorrectJson(string source, out string json)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new StringBuilder(source.Length);
        var containers = new Stack<char>();
        var inString = false;
        var changed = false;

        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];

            if (!inString)
            {
                switch (current)
                {
                    case '\"':
                        inString = true;
                        break;
                    case '{':
                    case '[':
                        containers.Push(current);
                        break;
                    case '}' when !TryCloseContainer(containers, '{'):
                    case ']' when !TryCloseContainer(containers, '['):
                        json = source;
                        return false;
                }

                result.Append(current);
                continue;
            }

            switch (current)
            {
                case '<' when TryFindXmlEnd(source, index, out var xmlEnd):
                {
                    index = xmlEnd - 1;
                    changed = true;
                    continue;
                }
                case '\\':
                {
                    if (index + 1 < source.Length && IsValidEscapeCharacter(source[index + 1]))
                    {
                        result.Append(current).Append(source[++index]);
                    }
                    else
                    {
                        result.Append("\\\\");
                        changed = true;
                    }

                    continue;
                }
                case '\"':
                {
                    if (IsClosingQuote(source, index))
                    {
                        inString = false;
                        result.Append(current);
                    }
                    else
                    {
                        result.Append("\\\"");
                        changed = true;
                    }

                    continue;
                }
                default:
                    switch (current)
                    {
                        case '\r':
                            result.Append("\\r");
                            changed = true;
                            break;
                        case '\n':
                            result.Append("\\n");
                            changed = true;
                            break;
                        case '\t':
                            result.Append("\\t");
                            changed = true;
                            break;
                        case < ' ':
                            result.Append($"\\u{(int)current:x4}");
                            changed = true;
                            break;
                        default:
                            result.Append(current);
                            break;
                    }

                    break;
            }
        }

        if (inString || containers.Count != 0)
        {
            json = source;
            return false;
        }

        json = changed ? result.ToString() : source;

        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            json = source;
            return false;
        }
    }

    private static bool IsClosingQuote(string source, int quoteIndex)
    {
        for (var index = quoteIndex + 1; index < source.Length; index++)
        {
            if (char.IsWhiteSpace(source[index]))
                continue;

            return source[index] is ':' or ',' or '}' or ']';
        }

        return true;
    }

    private static bool IsValidEscapeCharacter(char value) =>
        value is '\"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't' or 'u';

    private static bool TryCloseContainer(Stack<char> containers, char expectedOpening)
    {
        if (!containers.TryPeek(out var opening) || opening != expectedOpening)
            return false;

        containers.Pop();

        return true;
    }

    private static bool TryFindXmlEnd(string source, int xmlStart, out int xmlEnd)
    {
        var index = xmlStart;

        if (source.AsSpan(index).StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var declarationEnd = source.IndexOf("?>", index, StringComparison.Ordinal);
            if (declarationEnd < 0)
            {
                xmlEnd = 0;
                return false;
            }

            index = source.IndexOf('<', declarationEnd + 2);
            if (index < 0)
            {
                xmlEnd = 0;
                return false;
            }
        }

        if (!TryReadXmlTag(source, index, out var rootName, out var rootTagEnd, out var isClosing,
                out var isSelfClosing)
            || isClosing)
        {
            xmlEnd = 0;
            return false;
        }

        if (isSelfClosing)
        {
            xmlEnd = rootTagEnd + 1;
            return true;
        }

        var elements = new Stack<string>();
        elements.Push(rootName);
        index = rootTagEnd + 1;

        while (index < source.Length)
        {
            index = source.IndexOf('<', index);
            if (index < 0)
                break;

            if (TrySkipXmlSpecialTag(source, index, out var specialTagEnd))
            {
                index = specialTagEnd;
                continue;
            }

            if (!TryReadXmlTag(source, index, out var name, out var tagEnd, out isClosing, out isSelfClosing))
            {
                xmlEnd = 0;
                return false;
            }

            if (isClosing)
            {
                if (!elements.TryPeek(out var opening) ||
                    !string.Equals(opening, name, StringComparison.OrdinalIgnoreCase))
                {
                    xmlEnd = 0;
                    return false;
                }

                elements.Pop();
                if (elements.Count == 0)
                {
                    xmlEnd = tagEnd + 1;
                    return true;
                }
            }
            else if (!isSelfClosing)
            {
                elements.Push(name);
            }

            index = tagEnd + 1;
        }

        xmlEnd = 0;
        return false;
    }

    private static bool TrySkipXmlSpecialTag(string source, int tagStart, out int tagEnd)
    {
        var endMarker = source.AsSpan(tagStart) switch
        {
            var value when value.StartsWith("<!--", StringComparison.Ordinal) => "-->",
            var value when value.StartsWith("<![CDATA[", StringComparison.Ordinal) => "]]>",
            var value when value.StartsWith("<?", StringComparison.Ordinal) => "?>",
            _ => null
        };

        if (endMarker == null)
        {
            tagEnd = 0;
            return false;
        }

        var markerIndex = source.IndexOf(endMarker, tagStart + 2, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            tagEnd = 0;
            return false;
        }

        tagEnd = markerIndex + endMarker.Length;
        return true;
    }

    private static bool TryReadXmlTag(
        string source,
        int tagStart,
        out string name,
        out int tagEnd,
        out bool isClosing,
        out bool isSelfClosing)
    {
        name = string.Empty;
        tagEnd = 0;
        isClosing = false;
        isSelfClosing = false;

        if (tagStart + 1 >= source.Length || source[tagStart] != '<')
            return false;

        var index = tagStart + 1;
        if (source[index] is '!' or '?')
            return false;

        isClosing = source[index] == '/';
        if (isClosing)
            index++;

        var nameStart = index;
        while (index < source.Length &&
               (char.IsLetterOrDigit(source[index]) || source[index] is '_' or ':' or '-' or '.'))
            index++;

        if (nameStart == index)
            return false;

        name = source[nameStart..index];
        char quote = '\0';

        for (; index < source.Length; index++)
        {
            var current = source[index];
            if (quote != '\0')
            {
                if (current == quote)
                    quote = '\0';

                continue;
            }

            if (current is '\"' or '\'')
            {
                quote = current;
                continue;
            }

            if (current != '>')
                continue;

            tagEnd = index;
            var previous = index - 1;
            while (previous >= tagStart && char.IsWhiteSpace(source[previous]))
                previous--;

            isSelfClosing = !isClosing && previous >= tagStart && source[previous] == '/';
            return true;
        }

        return false;
    }
}