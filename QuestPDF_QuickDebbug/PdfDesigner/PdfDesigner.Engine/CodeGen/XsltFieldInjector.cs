using System.Text;
using System.Text.RegularExpressions;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Inserta de forma idempotente campos <c>&lt;Campo clave="..." valor="{...}"/&gt;</c>
/// en el XSLT, dentro de la región marcada
/// <c>&lt;!-- DESIGNER-FIELDS:START/END --&gt;</c> de &lt;Adicional&gt;.
/// </summary>
public static class XsltFieldInjector
{
    private const string Start = "<!-- DESIGNER-FIELDS:START -->";
    private const string End = "<!-- DESIGNER-FIELDS:END -->";

    private static readonly Regex CampoRegex =
        new(@"<Campo\s+clave=""([^""]+)""\s+valor=""\{([^}]*)\}""", RegexOptions.Compiled);

    /// <summary>Traduce un XPath del diseñador (/Invoice/cac:.../cbc:X) a expresión XSLT (fe:Invoice/cac:.../cbc:X).</summary>
    public static string Translate(string designerXpath)
    {
        var s = (designerXpath ?? string.Empty).Trim();
        if (s.StartsWith("fe:", StringComparison.Ordinal))
            return s;

        s = s.TrimStart('/');
        var segments = s.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && (segments[0] == "Invoice" || segments[0] == "fe:Invoice"))
            segments.RemoveAt(0);

        return "fe:Invoice/" + string.Join('/', segments);
    }

    /// <summary>Agrega/actualiza campos en el bloque marcado. Devuelve los campos resultantes.</summary>
    public static IReadOnlyList<(string Clave, string Xpath)> Inject(
        string xsltPath, IEnumerable<(string Clave, string Xpath)> fields)
    {
        var xslt = File.ReadAllText(xsltPath);
        var existing = ReadExisting(xslt);

        foreach (var (clave, xpath) in fields)
        {
            if (!string.IsNullOrWhiteSpace(clave))
                existing[clave] = Translate(xpath);
        }

        var block = BuildBlock(existing);
        xslt = ReplaceOrInsertBlock(xslt, block);
        File.WriteAllText(xsltPath, xslt);

        return existing.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    public static IReadOnlyList<(string Clave, string Xpath)> Read(string xsltPath)
    {
        var xslt = File.ReadAllText(xsltPath);
        return ReadExisting(xslt).Select(kv => (kv.Key, kv.Value)).ToList();
    }

    private static Dictionary<string, string> ReadExisting(string xslt)
    {
        var result = new Dictionary<string, string>();
        var startIdx = xslt.IndexOf(Start, StringComparison.Ordinal);
        var endIdx = xslt.IndexOf(End, StringComparison.Ordinal);
        if (startIdx < 0 || endIdx <= startIdx)
            return result;

        var content = xslt[startIdx..endIdx];
        foreach (Match match in CampoRegex.Matches(content))
            result[match.Groups[1].Value] = match.Groups[2].Value;

        return result;
    }

    private static string BuildBlock(Dictionary<string, string> fields)
    {
        var sb = new StringBuilder();
        foreach (var kv in fields.OrderBy(k => k.Key))
            sb.AppendLine($"            <Campo clave=\"{kv.Key}\" valor=\"{{{kv.Value}}}\"/>");
        return sb.ToString();
    }

    private static string ReplaceOrInsertBlock(string xslt, string block)
    {
        var startIdx = xslt.IndexOf(Start, StringComparison.Ordinal);
        var endIdx = xslt.IndexOf(End, StringComparison.Ordinal);

        if (startIdx >= 0 && endIdx > startIdx)
        {
            var before = xslt[..(startIdx + Start.Length)];
            var after = xslt[endIdx..];
            return before + "\n" + block + "            " + after;
        }

        var marker = xslt.IndexOf("<Adicional", StringComparison.Ordinal);
        if (marker < 0)
            throw new InvalidOperationException("No se encontró <Adicional> en el XSLT.");

        var close = xslt.IndexOf('>', marker);
        if (close < 0)
            throw new InvalidOperationException("No se pudo ubicar el cierre de <Adicional>.");

        var insertion = $"\n            {Start}\n{block}            {End}";
        return xslt.Insert(close + 1, insertion);
    }
}
