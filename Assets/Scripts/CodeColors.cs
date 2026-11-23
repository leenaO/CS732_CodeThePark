using System.Text;

// Code the Park - written by us.
// Colours C++ code like a real code editor (the colours of the popular dark theme in VS Code):
//   keywords blue, types teal, text in quotes orange, numbers light green,
//   comments green, function names yellow, cout / endl light blue.
// Returns TextMeshPro rich text. "___" (the blank) is kept as it is.
public static class CodeColors
{
    const string Keyword = "#569CD6", Type = "#4EC9B0", Str = "#CE9178", Num = "#B5CEA8",
                 Comment = "#6A9955", Func = "#DCDCAA", Stream = "#9CDCFE", Plain = "#D4D4D4";
    static readonly string[] Keywords = { "if", "else", "for", "while", "return", "true", "false", "void" };
    static readonly string[] Types = { "int", "double", "string", "bool", "char" };
    static readonly string[] Streams = { "cout", "cin", "endl" };

    public static string Color(string code)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < code.Length)
        {
            char c = code[i];
            if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')            // comment to the end
            { Span(sb, Comment, code.Substring(i)); break; }
            if (c == '"' || c == '\'')                                              // "text" or 'x'
            {
                int j = code.IndexOf(c, i + 1); if (j < 0) j = code.Length - 1;
                Span(sb, Str, code.Substring(i, j - i + 1)); i = j + 1; continue;
            }
            if (char.IsDigit(c))                                                    // 12, 2.5
            {
                int j = i; while (j < code.Length && (char.IsDigit(code[j]) || code[j] == '.')) j++;
                Span(sb, Num, code.Substring(i, j - i)); i = j; continue;
            }
            if (char.IsLetter(c) || c == '_')                                       // words
            {
                int j = i; while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] == '_')) j++;
                string w = code.Substring(i, j - i);
                int k = j; while (k < code.Length && code[k] == ' ') k++;
                if (w.StartsWith("___")) sb.Append(w);                               // the blank stays plain
                else if (System.Array.IndexOf(Keywords, w) >= 0) Span(sb, Keyword, w);
                else if (System.Array.IndexOf(Types, w) >= 0) Span(sb, Type, w);
                else if (System.Array.IndexOf(Streams, w) >= 0) Span(sb, Stream, w);
                else if (k < code.Length && code[k] == '(') Span(sb, Func, w);
                else Span(sb, Plain, w);
                i = j; continue;
            }
            Span(sb, Plain, c.ToString()); i++;
        }
        return sb.ToString();
    }

    static void Span(StringBuilder sb, string col, string s)
    {
        sb.Append("<color=").Append(col).Append('>').Append(s.Replace("<", "<noparse><</noparse>")).Append("</color>");
    }
}
