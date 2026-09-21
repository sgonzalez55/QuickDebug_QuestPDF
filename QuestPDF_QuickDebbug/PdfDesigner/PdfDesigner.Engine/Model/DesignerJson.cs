using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfQuickDebug.Designer.Model;

/// <summary>Configuración central de serialización del diseño (camelCase, comentarios, trailing commas).</summary>
public static class DesignerJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ReportDesign Load(string path) => Deserialize(File.ReadAllText(path));

    public static ReportDesign Deserialize(string json)
    {
        var design = JsonSerializer.Deserialize<ReportDesign>(json, Options)
            ?? throw new InvalidOperationException("No se pudo deserializar el diseño JSON.");
        design.Header ??= new LayoutNode { Type = "column" };
        design.Content ??= new LayoutNode { Type = "column" };
        design.Footer ??= new LayoutNode { Type = "column" };
        design.Page ??= new PageDesign();
        design.Metadata ??= new DesignMetadata();
        design.Namespaces ??= new Dictionary<string, string>();
        design.Expressions ??= new Dictionary<string, ExpressionDesign>();
        return design;
    }

    public static string Serialize(ReportDesign design) => JsonSerializer.Serialize(design, Options);
}
