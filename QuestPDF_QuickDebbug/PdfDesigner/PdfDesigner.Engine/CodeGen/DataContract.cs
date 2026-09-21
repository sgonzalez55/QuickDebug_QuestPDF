using System.Reflection;
using System.Text.Json;
using PdfQuickDebug.Core;
using PdfQuickDebug.Designer.Model;
using PdfQuickDebug.Templates;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Catálogo de campos disponibles para enlazar en el diseñador. Se construye
/// combinando el binding-map (modelo), las claves reales de CustomFields que
/// produce el XSLT, y las propiedades del DtoUbl.
/// </summary>
public sealed class DataContract
{
    public string Version { get; set; } = "1.0";
    public List<Field> Fields { get; set; } = new();

    public sealed class Field
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty; // Modelo | Custom | DTO
        public string Xpath { get; set; } = string.Empty;
        public string Expr { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static DataContract? Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<DataContract>(File.ReadAllText(path), Options);
        }
        catch
        {
            // ignorar
        }
        return null;
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));

    /// <summary>Construye el contrato corriendo el pipeline (XML + XSLT) sobre el XML de producción.</summary>
    public static DataContract Build(BindingMap map, string pipelineXmlPath, string xsltPath)
    {
        var contract = new DataContract();

        // 1) Modelo (desde binding-map)
        foreach (var entry in map.Mappings)
        {
            contract.Fields.Add(new Field
            {
                Name = FriendlyName(entry.Expr),
                Group = "Modelo",
                Xpath = entry.Match,
                Expr = entry.Expr
            });
        }

        // 2) Custom (claves reales que produce el XSLT)
        try
        {
            var xml = File.ReadAllText(pipelineXmlPath);
            var xslt = File.ReadAllText(xsltPath);
            var transformed = new XsltTransform().Transform(xml, xslt);
            var model = XmlParser.Parse(transformed);

            foreach (var key in model.Additional.CustomFields.Keys.OrderBy(k => k))
            {
                contract.Fields.Add(new Field
                {
                    Name = key,
                    Group = "Custom",
                    Xpath = $"//Adicional/Campo[@clave='{key}']/@valor",
                    Expr = $"GetCustomField(model, \"{key}\")"
                });
            }
        }
        catch
        {
            // sin pipeline disponible: solo modelo + DTO
        }

        // 3) DTO (propiedades propias de DtoUbl)
        foreach (var property in typeof(DtoUbl).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.DeclaringType != typeof(DtoUbl)) continue;
            contract.Fields.Add(new Field
            {
                Name = property.Name,
                Group = "DTO",
                Xpath = string.Empty,
                Expr = $"dto.{property.Name}"
            });
        }

        contract.Fields = contract.Fields
            .GroupBy(f => f.Expr)
            .Select(g => g.First())
            .OrderBy(f => f.Group)
            .ThenBy(f => f.Name)
            .ToList();

        return contract;
    }

    private static string FriendlyName(string expr)
    {
        if (expr.StartsWith("model.", StringComparison.Ordinal))
            return expr["model.".Length..];
        if (expr.StartsWith("line.", StringComparison.Ordinal))
            return expr;
        if (expr.StartsWith("dto.", StringComparison.Ordinal))
            return expr["dto.".Length..];
        return expr;
    }
}
