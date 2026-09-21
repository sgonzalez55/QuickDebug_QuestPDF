using System.Text;
using System.Text.RegularExpressions;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Genera la entrega dto\DtoUbl.cs de un cliente: un DTO que hereda de InvoiceModel
/// (del servicio) y agrega campos tipados por cliente.
///
/// Merge: si ya existe el archivo (p. ej. el DtoUbl.cs heredado de AJECOLOMBIA), se
/// conservan todos sus miembros —incluidos Factores/Motivos/CodigosBarra y
/// FactorLineInfo— y solo se agregan propiedades nuevas para las claves de CustomFields
/// que el contrato descubra y que aún no estén declaradas.
/// </summary>
public static class DtoToCSharpGenerator
{
    private static readonly (string Name, string Summary)[] CuratedBase =
    {
        ("Zona", "Zona de distribución del cliente"),
        ("Ruta", "Ruta de entrega"),
        ("PlacaVehiculo", "Placa del vehículo de entrega"),
        ("NumCargue", "Número de cargue"),
        ("OrdenCompra", "Orden de compra del cliente"),
        ("NumPedido", "Número de pedido"),
        ("FechaDian", "Fecha de firma DIAN (xades:SigningTime)"),
        ("HoraGeneracion", "Hora de generación del documento"),
        ("ResolucionTexto", "Texto de la resolución DIAN"),
        ("LogoBase64", "Logo en formato Base64 o URL"),
        ("ValorLetras", "Total en letras (calculado)"),
        ("CondicionPago", "Condición de pago (1=CONTADO, 2=CREDITO) según cac:PaymentMeans/cbc:ID del ERP")
    };

    private static readonly string[] InfraMembers =
    {
        "/// <summary>Factores de empaque por línea, resueltos desde CustomFields en MapToCustomDto.",
        "/// Keyed por referencia de LineItemInfo (misma lista que Lines, robusto a paginación).</summary>",
        "public Dictionary<LineItemInfo, FactorLineInfo> Factores { get; set; } = new();",
        "",
        "/// <summary>Motivo de venta (f_motivo, AdditionalItemProperty) por línea: valor crudo 01-08.",
        "/// Keyed por referencia de LineItemInfo, igual que Factores. Solo se usa en FacturaUbl.</summary>",
        "public Dictionary<LineItemInfo, string> Motivos { get; set; } = new();",
        "",
        "/// <summary>Código de barras principal (f_codigo_barra_principal, AdditionalItemProperty) por línea.",
        "/// Keyed por referencia de LineItemInfo, igual que Factores. Solo se usa en FacturaUbl.</summary>",
        "public Dictionary<LineItemInfo, string> CodigosBarra { get; set; } = new();"
    };

    private static readonly string FactorLineInfoBlock =
        """
        /// <summary>
        /// Datos de factor de empaque de una línea, calculados por el template (no viven en Models).
        /// </summary>
        public class FactorLineInfo
        {
            public string UmEmp { get; set; } = string.Empty;
            public decimal FactorEmp { get; set; }
            public decimal FactorEmpaque { get; set; }
        }
        """;

    /// <summary>Genera (o mergea) el DtoUbl.cs del cliente para el servicio y devuelve el código listo para escribir.</summary>
    public static string Generate(DataContract contract, string existingPath, string nit)
    {
        var ns = string.IsNullOrWhiteSpace(nit) ? "_000000" : "_" + SanitizeNs(nit);
        return GenerateCore(contract, existingPath, Header(ns));
    }

    /// <summary>Genera (o mergea) el DtoUbl.cs con namespaces locales (PdfQuickDebug.Templates),
    /// listo para copiar a DebugPDF\PdfQuickDebug\Templates\DtoUbl.cs.</summary>
    public static string GenerateLocal(DataContract contract, string existingPath)
    {
        const string localHeader =
            "namespace PdfQuickDebug.Templates;\n\n" +
            "using PdfQuickDebug.Core;\n\n" +
            "/// <summary>\n" +
            "/// DTO personalizado del cliente generado por el diseñador (merge con el heredado).\n" +
            "/// Hereda de InvoiceModel y agrega campos específicos del cliente.\n" +
            "/// </summary>\n";
        return GenerateCore(contract, existingPath, localHeader);
    }

    private static string GenerateCore(DataContract contract, string existingPath, string header)
    {
        // Claves custom descubiertas por el contrato.
        var discovered = contract.Fields
            .Where(f => f.Group == "Custom")
            .Select(TryExtractKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = SafeRead(existingPath);
        if (!string.IsNullOrWhiteSpace(existing))
            return Merge(existing, header, discovered);

        return FromScratch(header, discovered);
    }

    private static string Merge(string existing, string header, List<string> discovered)
    {
        var open = FindClassStart(existing, "public class DtoUbl");
        var closeIndex = FindClassClose(existing, "public class DtoUbl");
        if (open < 0 || closeIndex < 0)
        {
            // El archivo existente no contiene la clase — regenera desde cero.
            return FromScratch(header, discovered);
        }

        var prefix = existing[open..closeIndex];
        var suffix = existing[closeIndex..];  // incluye el '}' y todo lo que sigue (FactorLineInfo)

        var known = CollectMemberNames(prefix);
        var additions = NewProps(discovered, known);
        var body = additions.Length == 0
            ? string.Empty
            : "\n\n    // ═══════════════════════════════════════════════════════════════════════════════\n" +
              "    // CAMPOS NUEVOS DESCUBIERTOS POR EL CONTRATO\n" +
              "    // ═══════════════════════════════════════════════════════════════════════════════\n" +
              "\n" + additions;

        // El archivo heredado ya viene completo; solo insertamos las propiedades nuevas antes del cierre.
        return header + prefix + body + "\n" + suffix;
    }

    private static string FromScratch(string header, List<string> discovered)
    {
        var known = new HashSet<string>(CuratedBase.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        var additions = NewProps(discovered, known);

        var members = new StringBuilder();
        foreach (var (name, summary) in CuratedBase)
            members.AppendLine($"    /// <summary>{summary}</summary>")
                   .AppendLine($"    public string {name} {{ get; set; }} = string.Empty;")
                   .AppendLine();
        if (additions.Length > 0)
            members.AppendLine(additions.TrimEnd()).AppendLine();
        foreach (var line in InfraMembers)
            members.AppendLine("    " + line);

        return header +
               "\npublic class DtoUbl : InvoiceModel\n{\n" +
               members +
               "}\n\n" +
               FactorLineInfoBlock + "\n";
    }

    private static string NewProps(List<string> discovered, ISet<string> known)
    {
        var builder = new StringBuilder();
        foreach (var key in discovered)
        {
            var prop = ToPropName(key);
            if (known.Contains(prop)) continue;
            known.Add(prop);
            builder.AppendLine($"    /// <summary>Campo <c>{key}</c> (CustomField descubierto)</summary>");
            builder.AppendLine($"    public string {prop} {{ get; set; }} = string.Empty;");
            builder.AppendLine();
        }
        return builder.ToString().TrimEnd('\n');
    }

    private static string Header(string nsSuffix)
        => $"namespace Services.PdfGenerator.Infrastructure.Templates.{nsSuffix};\n\n" +
           "using Services.PdfGenerator.Domain.Models;\n\n" +
           "/// <summary>\n" +
           "/// DTO personalizado del cliente generado por el diseñador (merge con el heredado).\n" +
"/// Hereda de InvoiceModel y agrega campos específicos del cliente.\n" +
       "/// </summary>\n";

    private static string? TryExtractKey(DataContract.Field field)
    {
        var m = Regex.Match(field.Expr ?? string.Empty, @"GetCustomField\(model,\s*""([^""]+)""\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string ToPropName(string key)
    {
        var sb = new StringBuilder();
        foreach (var segment in Regex.Split(key, "[^A-Za-z0-9]+"))
        {
            if (segment.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(segment[0]));
            if (segment.Length > 1) sb.Append(segment[1..]);
        }
        var name = sb.Length == 0 ? "Campo" : sb.ToString();
        if (char.IsDigit(name[0])) name = "Campo" + name;
        return name;
    }

    private static HashSet<string> CollectMemberNames(string code)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(code, @"public\s+(?:[A-Za-z_][\w<>,\[\]\. ]*?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{"))
            names.Add(m.Groups["name"].Value);
        return names;
    }

    /// <summary>Índice donde empieza la clase que coincide con <paramref name="classMarker"/> (o -1).</summary>
    private static int FindClassStart(string code, string classMarker)
        => code.IndexOf(classMarker, StringComparison.Ordinal);

    /// <summary>Índice del '}' que cierra la clase que empieza con <paramref name="classMarker"/> (o -1).</summary>
    private static int FindClassClose(string code, string classMarker)
    {
        var start = code.IndexOf(classMarker, StringComparison.Ordinal);
        if (start < 0) return -1;
        var open = code.IndexOf('{', start);
        if (open < 0) return -1;

        var depth = 0;
        for (var i = open; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static string SanitizeNs(string nit)
    {
        var sb = new StringBuilder();
        foreach (var ch in nit.Trim())
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return sb.Length == 0 ? "000000" : sb.ToString();
    }

    private static string SafeRead(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}