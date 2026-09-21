using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace PdfQuickDebug.Designer.Data;

/// <summary>
/// Resuelve expresiones XPath contra un XML, con soporte de namespaces y
/// tolerante a nodos faltantes. Si un XPath con prefijos no registrados falla,
/// reintenta automáticamente por local-name() (namespace-agnóstico).
/// </summary>
public sealed class XmlDataResolver
{
    private static readonly Regex AttrPrefix =
        new(@"@([A-Za-z_][\w.-]*):([A-Za-z_][\w.-]*)", RegexOptions.Compiled);

    private static readonly Regex ElemPrefix =
        new(@"([A-Za-z_][\w.-]*):([A-Za-z_][\w.-]*)", RegexOptions.Compiled);

    private static readonly Regex AttrBare =
        new(@"@([A-Za-z_][\w.-]*)", RegexOptions.Compiled);

    private static readonly Regex ElemBare =
        new(@"(?<![\w.\-:'@\[])([A-Za-z_][\w.\-]*)(?![\w.\-]|::|\s*\()", RegexOptions.Compiled);

    private static readonly Regex Literal =
        new(@"'[^']*'", RegexOptions.Compiled);

    private static readonly Regex LiteralPlaceholder =
        new("\u0001(\\d+)\u0001", RegexOptions.Compiled);

    // Solo operadores sin paréntesis (las funciones ya se excluyen por el lookahead '(?!)').
    private static readonly HashSet<string> XPathKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "mod", "div"
    };

    private readonly XPathNavigator _root;
    private readonly XmlNamespaceManager _ns;
    private readonly Action<string>? _log;

    public XmlDataResolver(string xml, IDictionary<string, string>? namespaces = null, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("El XML no puede estar vacío.", nameof(xml));

        var doc = new XmlDocument { XmlResolver = null };
        doc.LoadXml(xml);

        _root = doc.CreateNavigator()!;
        _ns = new XmlNamespaceManager(doc.NameTable);

        if (namespaces != null)
        {
            foreach (var kv in namespaces)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    _ns.AddNamespace(kv.Key, kv.Value);
            }
        }

        _log = log;
    }

    /// <summary>Resuelve un valor de texto a nivel documento.</summary>
    public string ResolveString(string? xpath, string @default = "")
        => ResolveString(xpath, null, @default);

    /// <summary>Resuelve un valor de texto relativo a un nodo (o documento si current es null).</summary>
    public string ResolveString(string? xpath, XPathNavigator? current, string @default = "")
    {
        if (string.IsNullOrWhiteSpace(xpath))
            return @default;

        var result = TryEvaluate(xpath, current);
        return string.IsNullOrEmpty(result) ? @default : result!;
    }

    public decimal ResolveDecimal(string? xpath, XPathNavigator? current = null, decimal @default = 0m)
    {
        var raw = ResolveString(xpath, current, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return @default;

        raw = raw.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : @default;
    }

    public DateTime ResolveDate(string? xpath, XPathNavigator? current = null)
    {
        var raw = ResolveString(xpath, current, string.Empty);
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : DateTime.MinValue;
    }

    /// <summary>Resuelve una lista de nodos (para secciones/tablas repetidas).</summary>
    public IReadOnlyList<XPathNavigator> ResolveNodes(string? xpath, XPathNavigator? current = null)
    {
        if (string.IsNullOrWhiteSpace(xpath))
            return Array.Empty<XPathNavigator>();

        var nav = current ?? _root;
        var list = new List<XPathNavigator>();

        try
        {
            Collect(nav.Select(xpath, _ns), list);
        }
        catch (Exception ex)
        {
            _log?.Invoke($"[XmlDataResolver] XPath falló ('{xpath}'): {ex.Message}");
        }

        if (list.Count == 0)
        {
            try
            {
                Collect(nav.Select(ToLocalName(xpath), _ns), list);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[XmlDataResolver] XPath local-name falló ('{xpath}'): {ex.Message}");
            }
        }

        return list;
    }

    private static void Collect(XPathNodeIterator iterator, List<XPathNavigator> target)
    {
        while (iterator.MoveNext())
        {
            if (iterator.Current != null)
                target.Add(iterator.Current.Clone());
        }
    }

    private string? TryEvaluate(string xpath, XPathNavigator? current)
    {
        var nav = current ?? _root;

        // 1) Node-set directo
        try
        {
            var node = nav.SelectSingleNode(xpath, _ns);
            if (node != null)
                return node.Value;
        }
        catch
        {
            // prefijo no registrado o expresión no node-set: seguimos.
        }

        // 2) Expresión escalar (o primer nodo si Evaluate devuelve node-set)
        var scalar = EvaluateScalar(nav, xpath);
        if (!string.IsNullOrEmpty(scalar))
            return scalar;

        // 3) Fallback namespace-agnóstico por local-name()
        var local = ToLocalName(xpath);
        try
        {
            var node = nav.SelectSingleNode(local, _ns);
            if (node != null)
                return node.Value;
        }
        catch
        {
            // seguimos
        }

        return EvaluateScalar(nav, local);
    }

    /// <summary>Evalúa sin stringificar un XPathNodeIterator (evita "MS.Internal.Xml.XPath...").</summary>
    private string? EvaluateScalar(XPathNavigator nav, string xpath)
    {
        try
        {
            var result = nav.Evaluate(xpath, _ns);
            return result switch
            {
                null => null,
                XPathNodeIterator iterator => iterator.MoveNext() ? iterator.Current?.Value : null,
                _ => result.ToString()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Convierte nombres (con y sin prefijo) a local-name() para XMLs con namespaces variables.</summary>
    private static string ToLocalName(string xpath)
    {
        // Proteger literales '...' para no tocar sus contenidos.
        var literals = new List<string>();
        var text = Literal.Replace(xpath, match =>
        {
            literals.Add(match.Value);
            return $"\u0001{literals.Count - 1}\u0001";
        });

        text = AttrPrefix.Replace(text, "@*[local-name()='$2']");
        text = AttrBare.Replace(text, "@*[local-name()='$1']");
        text = ElemPrefix.Replace(text, "*[local-name()='$2']");
        text = ElemBare.Replace(text, match =>
            XPathKeywords.Contains(match.Groups[1].Value)
                ? match.Value
                : $"*[local-name()='{match.Groups[1].Value}']");

        return LiteralPlaceholder.Replace(text, match => literals[int.Parse(match.Groups[1].Value)]);
    }
}
