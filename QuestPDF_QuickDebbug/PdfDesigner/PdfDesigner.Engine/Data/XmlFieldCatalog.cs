using System.Text.RegularExpressions;
using System.Xml;

namespace PdfQuickDebug.Designer.Data;

/// <summary>
/// Desglosa un XML en un árbol de campos arrastrables. Incluye hojas y
/// atributos, marca nodos repetidos y excluye ruido técnico (firma, blobs).
/// </summary>
public static class XmlFieldCatalog
{
    public sealed class FieldNode
    {
        public string Name { get; set; } = string.Empty;
        public string XPath { get; set; } = string.Empty;

        /// <summary>XPath relativo al ancestro repetido más cercano (para tablas/detail).</summary>
        public string RelativePath { get; set; } = string.Empty;

        public string? Value { get; set; }
        public bool IsField { get; set; }
        public bool IsRepeated { get; set; }
        public List<FieldNode> Children { get; set; } = new();
    }

    private const int MaxValueLength = 200;

    private static readonly HashSet<string> NoiseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Signature", "SignedInfo", "SignatureValue", "KeyInfo", "X509Data",
        "X509Certificate", "X509SubjectName", "X509IssuerSerial", "X509IssuerName",
        "X509SerialNumber", "QualifyingProperties", "SignedProperties",
        "SignedSignatureProperties", "SigningTime", "SignerRole", "ClaimedRoles",
        "ClaimedRole", "SignaturePolicyIdentifier", "SignaturePolicyId", "SigPolicyHash",
        "SigPolicyQualifiers", "SigPolicyQualifier", "Reference", "Transforms", "Transform",
        "DigestMethod", "DigestValue", "CanonicalizationMethod", "SignatureMethod",
        "RSAKeyValue", "Modulus", "Exponent", "Object", "Algorithm"
    };

    public static FieldNode Build(string xml)
    {
        var doc = new XmlDocument { XmlResolver = null };
        doc.LoadXml(xml);
        var root = doc.DocumentElement ?? throw new InvalidOperationException("XML sin raíz.");
        return BuildNode(root, "/" + root.Name, null);
    }

    private static FieldNode BuildNode(XmlElement element, string xpath, string? repeatedAncestor)
    {
        var node = new FieldNode
        {
            Name = element.Name,
            XPath = xpath,
            RelativePath = Relative(xpath, repeatedAncestor)
        };

        // Atributos (excepto namespaces)
        foreach (XmlAttribute attr in element.Attributes)
        {
            if (attr.Name.StartsWith("xmlns", StringComparison.OrdinalIgnoreCase)) continue;

            var value = Trim(attr.Value);
            node.Children.Add(new FieldNode
            {
                Name = "@" + attr.Name,
                XPath = xpath + "/@" + attr.Name,
                RelativePath = Relative(xpath + "/@" + attr.Name, repeatedAncestor),
                Value = value,
                IsField = true
            });
        }

        var childElements = element.ChildNodes.OfType<XmlElement>().ToList();
        var counts = childElements.GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.Count());
        var seen = new Dictionary<string, int>();

        foreach (var child in childElements)
        {
            if (NoiseNames.Contains(LocalName(child.Name))) continue;
            if (IsLongBlob(child)) continue;

            var index = seen.GetValueOrDefault(child.Name) + 1;
            seen[child.Name] = index;

            var repeated = counts[child.Name] > 1;
            if (repeated && index > 1) continue; // solo la primera ocurrencia como representante

            var childXpath = xpath + "/" + child.Name;
            var childNode = BuildNode(child, childXpath, repeated ? childXpath : repeatedAncestor);
            childNode.IsRepeated = repeated;
            node.Children.Add(childNode);
        }

        // Hoja con texto
        if (childElements.Count == 0)
        {
            var text = element.InnerText?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                node.Value = Trim(text);
                node.IsField = true;
            }
        }

        // CustomField: nombre = @Name, valor = @Value
        if (LocalName(element.Name) == "CustomField")
        {
            var nameAttr = element.GetAttribute("Name");
            if (!string.IsNullOrEmpty(nameAttr))
            {
                var valueAttr = element.GetAttribute("Value");
                node.Name = nameAttr;
                node.XPath = $"//CustomField[@Name='{nameAttr}']/@Value";
                node.RelativePath = node.XPath;
                node.Value = Trim(valueAttr);
                node.IsField = true;
                node.Children.Clear();
            }
        }

        // Campo (forma del XSLT en producción): nombre = @clave, valor = @valor
        if (LocalName(element.Name) == "Campo")
        {
            var key = element.GetAttribute("clave");
            if (!string.IsNullOrEmpty(key))
            {
                var valueAttr = element.GetAttribute("valor");
                node.Name = key;
                node.XPath = $"//Campo[@clave='{key}']/@valor";
                node.RelativePath = node.XPath;
                node.Value = Trim(valueAttr);
                node.IsField = true;
                node.IsRepeated = false;
                node.Children.Clear();
            }
        }

        return node;
    }

    private static string Relative(string xpath, string? repeatedAncestor)
    {
        if (string.IsNullOrEmpty(repeatedAncestor))
            return xpath;
        if (!xpath.StartsWith(repeatedAncestor, StringComparison.Ordinal))
            return xpath;

        var rel = xpath[repeatedAncestor.Length..];
        rel = Regex.Replace(rel, @"\[\d+\]", string.Empty);
        return rel.TrimStart('/');
    }

    private static bool IsLongBlob(XmlElement element)
    {
        var text = element.InnerText;
        return !string.IsNullOrEmpty(text) && text.Length > MaxValueLength && !text.Contains(' ');
    }

    private static string LocalName(string name)
        => name.Contains(':') ? name[(name.IndexOf(':') + 1)..] : name;

    private static string? Trim(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        value = value.Trim();
        return value.Length > MaxValueLength ? value[..MaxValueLength] + "…" : value;
    }
}
