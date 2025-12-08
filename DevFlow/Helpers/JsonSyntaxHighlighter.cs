using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DevFlow.Helpers;

public static class JsonSyntaxHighlighter
{
    // Catppuccin Mocha colors for syntax highlighting
    private static readonly SolidColorBrush KeyBrush = new(Color.FromArgb(255, 137, 180, 250));       // Blue - keys
    private static readonly SolidColorBrush StringBrush = new(Color.FromArgb(255, 166, 227, 161));    // Green - string values
    private static readonly SolidColorBrush NumberBrush = new(Color.FromArgb(255, 250, 179, 135));    // Peach - numbers
    private static readonly SolidColorBrush BoolNullBrush = new(Color.FromArgb(255, 203, 166, 247));  // Mauve - bool/null
    private static readonly SolidColorBrush BracketBrush = new(Color.FromArgb(255, 205, 214, 244));   // Text - brackets
    private static readonly SolidColorBrush PunctuationBrush = new(Color.FromArgb(255, 147, 153, 178)); // Overlay2 - : ,
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 205, 214, 244));   // Default text

    public static void ApplyHighlighting(TextBlock textBlock, string text)
    {
        if (textBlock == null) return;
        
        textBlock.Inlines.Clear();
        
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Try to parse as JSON, if not valid just show plain text
        if (!IsLikelyJson(text))
        {
            textBlock.Inlines.Add(new Run { Text = text, Foreground = DefaultBrush });
            return;
        }

        int i = 0;
        bool expectingKey = true;

        while (i < text.Length)
        {
            char c = text[i];

            // Whitespace - preserve it
            if (char.IsWhiteSpace(c))
            {
                int start = i;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                textBlock.Inlines.Add(new Run { Text = text[start..i], Foreground = DefaultBrush });
                continue;
            }

            // String (key or value)
            if (c == '"')
            {
                int start = i;
                i++; // skip opening quote
                while (i < text.Length)
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        i += 2; // skip escaped char
                        continue;
                    }
                    if (text[i] == '"')
                    {
                        i++; // skip closing quote
                        break;
                    }
                    i++;
                }
                
                var tokenText = text[start..i];
                var brush = expectingKey ? KeyBrush : StringBrush;
                textBlock.Inlines.Add(new Run { Text = tokenText, Foreground = brush });
                continue;
            }

            // Number
            if (char.IsDigit(c) || (c == '-' && i + 1 < text.Length && (char.IsDigit(text[i + 1]) || text[i + 1] == '.')))
            {
                int start = i;
                if (c == '-') i++;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.' || text[i] == 'e' || text[i] == 'E' || text[i] == '+' || text[i] == '-'))
                {
                    if ((text[i] == '+' || text[i] == '-') && i > start && text[i-1] != 'e' && text[i-1] != 'E')
                        break;
                    i++;
                }
                textBlock.Inlines.Add(new Run { Text = text[start..i], Foreground = NumberBrush });
                continue;
            }

            // true
            if (i + 4 <= text.Length && text.Substring(i, 4) == "true")
            {
                textBlock.Inlines.Add(new Run { Text = "true", Foreground = BoolNullBrush });
                i += 4;
                continue;
            }

            // false
            if (i + 5 <= text.Length && text.Substring(i, 5) == "false")
            {
                textBlock.Inlines.Add(new Run { Text = "false", Foreground = BoolNullBrush });
                i += 5;
                continue;
            }

            // null
            if (i + 4 <= text.Length && text.Substring(i, 4) == "null")
            {
                textBlock.Inlines.Add(new Run { Text = "null", Foreground = BoolNullBrush });
                i += 4;
                continue;
            }

            // Object braces
            if (c == '{' || c == '}')
            {
                textBlock.Inlines.Add(new Run { Text = c.ToString(), Foreground = BracketBrush });
                if (c == '{') expectingKey = true;
                i++;
                continue;
            }

            // Array brackets
            if (c == '[' || c == ']')
            {
                textBlock.Inlines.Add(new Run { Text = c.ToString(), Foreground = BracketBrush });
                i++;
                continue;
            }

            // Colon - next value is not a key
            if (c == ':')
            {
                textBlock.Inlines.Add(new Run { Text = ":", Foreground = PunctuationBrush });
                expectingKey = false;
                i++;
                continue;
            }

            // Comma - reset for next key/value
            if (c == ',')
            {
                textBlock.Inlines.Add(new Run { Text = ",", Foreground = PunctuationBrush });
                expectingKey = true;
                i++;
                continue;
            }

            // Any other character
            textBlock.Inlines.Add(new Run { Text = c.ToString(), Foreground = DefaultBrush });
            i++;
        }
    }

    private static bool IsLikelyJson(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }
}
