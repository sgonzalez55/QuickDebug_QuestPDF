using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PdfQuickDebug.Core;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Un campo de la fuente de datos (Data Source). Representa un campo que el runtime
/// (Core) SÍ puede resolver en el invoice generado: model.&lt;ruta&gt;, line.&lt;prop&gt;,
/// GetCustomField(...) o dto.&lt;prop&gt;.
/// </summary>
public sealed class SchemaField
{
    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Expr { get; set; } = string.Empty;
    public string? XPath { get; set; }
    public string? SampleValue { get; set; }
    public bool IsLine { get; set; }
    public bool IsRepeated { get; set; }
    public bool RequiresDtoMap { get; set; }
    public bool HasValue { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Catálogo del "Data Source" estilo Crystal Reports. A diferencia del árbol XML crudo
/// (que en runtime no resuelve porque no sigue el vocabulario del parser de Core), esta
/// lista sale del InvoiceModel PARSEADO: cada campo tiene una expresión que el template
/// generado emite tal cual y que Core siempre puede leer.
///
/// Fuentes: (1) reflexión sobre InvoiceModel (model.* / line.*), (2) claves reales de
/// CustomFields que el XSLT emite en &lt;Adicional&gt; (GetCustomField), (3) propiedades
/// del DTO entregable del cliente (dto\DtoUbl.cs), que requieren MapToCustomDto.
/// </summary>
public static class ModelSchemaCatalog
{
    private static readonly (string Prop, string Group)[] RootOrder =
    {
        ("Document", "Documento"),
        ("Folio", "Folio"),
        ("Issuer", "Emisor"),
        ("Customer", "Receptor"),
        ("Lines", "Detalle (Líneas)"),
        ("Totals", "Totales"),
        ("QR", "QR"),
        ("InformacionAdditional", "Info. Adicional"),
        ("Additional", "Adicional (Custom)")
    };

    private static readonly string[] RootGroupOrder =
    {
        "Documento", "Folio", "Emisor", "Receptor", "Detalle (Líneas)",
        "Totales", "QR", "Info. Adicional", "Adicional", "Adicional (Custom)", "DTO"
    };

    /// <summary>Corre el pipeline (XML + XSLT) y devuelve el modelo parseado, igual que Core.</summary>
    public static (InvoiceModel? Model, string? TransformedXml) LoadPipeline(string xmlPath, string xsltPath)
    {
        try
        {
            var xml = File.ReadAllText(xmlPath);
            var xslt = File.ReadAllText(xsltPath);
            var transformed = new XsltTransform().Transform(xml, xslt);
            return (XmlParser.Parse(transformed), transformed);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Parsea un transformed.xml ya persistido (el archivo se guarda envuelto en
    /// "&lt;Root&gt;…&lt;/Root&gt;") sin volver a ejecutar el XSLT. Se usa como respaldo
    /// cuando el cliente no tiene raw.xml o el pipeline falló en tiempo de diseño.
    /// </summary>
    public static (InvoiceModel? Model, string? TransformedXml) LoadTransformed(string transformedPath)
    {
        try
        {
            if (!File.Exists(transformedPath)) return (null, null);
            var text = File.ReadAllText(transformedPath).Trim();
            // XmlParser vuelve a envolver en <Root>; quitar el wrapper persistido para no anidarlo.
            if (text.StartsWith("<Root>", StringComparison.Ordinal) &&
                text.EndsWith("</Root>", StringComparison.Ordinal))
                text = text[6..^7];
            return (XmlParser.Parse(text), text);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>Catálogo completo: modelo parseado + claves custom + DTO del cliente.</summary>
    public static List<SchemaField> BuildAll(string xmlPath, string xsltPath, string? dtoCsPath)
    {
        var (model, _) = LoadPipeline(xmlPath, xsltPath);
        var fields = new List<SchemaField>();
        fields.AddRange(BuildModel(model));
        fields.AddRange(BuildDto(dtoCsPath));
        return fields;
    }

    /// <summary>Campos del modelo parseado (reflexión) + CustomFields reales del XSLT.</summary>
    public static List<SchemaField> BuildModel(InvoiceModel? model)
    {
        var fields = new List<SchemaField>();
        if (model == null)
            return fields;

        foreach (var (prop, group) in RootOrder)
        {
            if (prop == "Lines")
            {
                var first = model.Lines.Count == 0 ? null : model.Lines[0];
                foreach (var pr in typeof(LineItemInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!pr.CanRead || pr.GetIndexParameters().Length > 0 || AreInfra(pr.Name))
                        continue;
                    var value = first == null ? null : SafeFormat(pr, first);
                    fields.Add(new SchemaField
                    {
                        Group = group,
                        Name = pr.Name,
                        Expr = "line." + pr.Name,
                        XPath = LineAttrByProp.TryGetValue(pr.Name, out var attr) ? attr : null,
                        SampleValue = value,
                        IsLine = true,
                        IsRepeated = false,
                        HasValue = !string.IsNullOrEmpty(value),
                        Note = value == null ? "sin líneas en la muestra" : null
                    });
                }
                continue;
            }

            if (prop == "Additional")
            {
                AddScalar(fields, "Adicional", "PaymentMethod", model.Additional.PaymentMethod, "model.Additional.PaymentMethod");
                AddScalar(fields, "Adicional", "PaymentCondition", model.Additional.PaymentCondition, "model.Additional.PaymentCondition");
                AddScalar(fields, "Adicional", "Notes", model.Additional.Notes, "model.Additional.Notes");

                foreach (var key in model.Additional.CustomFields.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    var value = model.Additional.CustomFields[key];
                    fields.Add(new SchemaField
                    {
                        Group = "Adicional (Custom)",
                        Name = key,
                        Expr = $"GetCustomField(model, \"{key}\")",
                        XPath = $"//Adicional/Campo[@clave='{key}']/@valor",
                        SampleValue = Has(value) ? value : null,
                        HasValue = Has(value)
                    });
                }
                continue;
            }

            var rootProp = typeof(InvoiceModel).GetProperty(prop);
            var rootValue = rootProp?.GetValue(model);
            if (rootValue == null)
                continue;

            foreach (var pr in rootValue.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pr.CanRead || pr.GetIndexParameters().Length > 0 || AreInfra(pr.Name))
                    continue;
                var value = SafeFormat(pr, rootValue);
                fields.Add(new SchemaField
                {
                    Group = group,
                    Name = $"{prop}.{pr.Name}",
                    Expr = $"model.{prop}.{pr.Name}",
                    SampleValue = value,
                    HasValue = !string.IsNullOrEmpty(value)
                });
            }
        }

        return fields;
    }

    /// <summary>Propiedades declaradas en el DTO entregable del cliente (dto\DtoUbl.cs).</summary>
    public static List<SchemaField> BuildDto(string? dtoCsPath)
    {
        var fields = new List<SchemaField>();
        if (string.IsNullOrWhiteSpace(dtoCsPath))
            return fields;

        try
        {
            if (!File.Exists(dtoCsPath))
                return fields;

            var code = DtoClassBody(File.ReadAllText(dtoCsPath));
            if (string.IsNullOrWhiteSpace(code))
                return fields;

            foreach (Match m in Regex.Matches(code,
                @"public\s+[\w<>,\[\]\. ]*?(?:\w+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get\s*;"))
            {
                var name = m.Groups["name"].Value;
                if (AreInfra(name))
                    continue;
                fields.Add(new SchemaField
                {
                    Group = "DTO",
                    Name = name,
                    Expr = "dto." + name,
                    RequiresDtoMap = true,
                    Note = "requiere MapToCustomDto en el servicio"
                });
            }
        }
        catch
        {
            // sin DTO entregable: se omite la tabla
        }

        return fields;
    }

    public static IEnumerable<string> RootGroups() => RootGroupOrder;

    private static void AddScalar(List<SchemaField> fields, string group, string name, object? value, string expr)
    {
        fields.Add(new SchemaField
        {
            Group = group,
            Name = name,
            Expr = expr,
            SampleValue = FormatValue(value),
            HasValue = Has(value)
        });
    }

    private static string? SafeFormat(PropertyInfo pr, object target)
    {
        try
        {
            return FormatValue(pr.GetValue(target));
        }
        catch
        {
            return null;
        }
    }

    private static string? FormatValue(object? value)
    {
        if (!Has(value)) return null;
        var text = value switch
        {
            DateTime dt => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("N2", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("N2", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            string s => s.Trim(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length > 60 ? text[..60] + "…" : text;
    }

    private static bool Has(object? value) => value switch
    {
        null => false,
        string s => !string.IsNullOrWhiteSpace(s),
        DateTime dt => dt != default,
        decimal dec => dec != 0,
        double dbl => dbl != 0,
        int i => i != 0,
        _ => true
    };

    /// <summary>Miembros de infraestructura que no son "campos" para el Data Source.</summary>
    private static bool AreInfra(string prop) => prop switch
    {
        "Factores" or "Motivos" or "CodigosBarra" or "QrImageBytes" or "CustomFields" => true,
        _ => false
    };

    /* ── Líneas del detalle (XmlParser.ParseLine, DebugPDF\Core) ─
       Atributos sobre el elemento <Detalle> del XML transformado. Impiden
       que el preview caiga a la primera línea: cada fila resuelve su nodo. */
    private static readonly Dictionary<string, string> LineAttrByProp = new()
    {
        ["LineNumber"] = "@linea_nu",
        ["Cant_Und"] = "@cantidad_unit",
        ["Description"] = "@descripcion_tx",
        ["Codigo_cd"] = "@codigo_cd",
        ["Tiquete"] = "@tiquete",
        ["TVenta"] = "@tventa",
        ["Quantity"] = "@cantidad_nu",
        ["Unit"] = "@unidad_cd",
        ["Unit_Medition"] = "@unidad_medida_cd",
        ["UnitPrice"] = "@precio_unitario_am",
        ["PrecioAntesIVA"] = "@item_subtotal_am",
        ["Discount"] = "@descuento_am",
        ["TaxRate"] = "@impuesto_tasa_nu",
        ["TaxValor"] = "@item_iva_am",
        ["LineTotal"] = "@total_linea_am",
        ["Ibua"] = "@ibua",
        ["ValorDescuento"] = "@vlr_descuento"
    };

    /// <summary>Solo el cuerpo de la clase DtoUbl (no FactorLineInfo ni otras clases del archivo).</summary>
    private static string DtoClassBody(string code)
    {
        var start = code.IndexOf("public class DtoUbl", StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        var open = code.IndexOf('{', start);
        if (open < 0) return string.Empty;

        var depth = 0;
        for (var i = open; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return code[start..i];
            }
        }
        return code[start..];
    }
}