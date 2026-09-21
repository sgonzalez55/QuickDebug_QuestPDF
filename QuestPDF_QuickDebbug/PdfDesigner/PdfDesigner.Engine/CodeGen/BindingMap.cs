using System.Text.Json;
using System.Text.RegularExpressions;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Traduce los XPath del diseño a expresiones C# sobre InvoiceModel/DtoUbl.
/// Se carga desde un binding-map.json editable; si no existe, usa los defaults.
/// El matching es agnóstico a namespaces (normaliza a local-name).
/// </summary>
public sealed class BindingMap
{
    public string Version { get; set; } = "1.0";
    public string CustomFieldPattern { get; set; } = "^f_";
    public string CustomFieldTemplate { get; set; } = "GetCustomField(model, \"{name}\")";
    public List<Entry> Mappings { get; set; } = new();

    public sealed class Entry
    {
        public string Match { get; set; } = string.Empty;
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

    public static BindingMap Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<BindingMap>(File.ReadAllText(path), Options) ?? CreateDefault();
        }
        catch
        {
            // cae a defaults
        }

        return CreateDefault();
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));

    /// <summary>Resuelve un XPath a una expresión C#. Devuelve literal + TODO si no hay mapeo.</summary>
    public string Resolve(string? xpath, bool inDetail, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(xpath))
            return Quote(fallback ?? string.Empty);

        var normalized = Normalize(xpath);

        var candidates = Mappings.Where(m => Normalize(m.Match) == normalized).ToList();
        if (candidates.Count == 0)
            candidates = Mappings.Where(m => normalized.EndsWith(Normalize(m.Match), StringComparison.Ordinal)).ToList();
        if (candidates.Count == 0)
        {
            var last = normalized.Split('/').Last();
            candidates = Mappings.Where(m => Normalize(m.Match).Split('/').Last() == last).ToList();
        }

        var pick = inDetail
            ? candidates.FirstOrDefault(c => c.Expr.StartsWith("line.", StringComparison.Ordinal)) ?? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(c => c.Expr.StartsWith("model.", StringComparison.Ordinal)) ?? candidates.FirstOrDefault();

        if (pick != null)
            return pick.Expr;

        var name = normalized.Split('/').Last();
        if (Regex.IsMatch(name, CustomFieldPattern))
            return CustomFieldTemplate.Replace("{name}", name);

        return $"{Quote(fallback ?? string.Empty)} /* TODO: mapear '{xpath}' */";
    }

    private static string Normalize(string xpath)
    {
        var s = Regex.Replace(xpath, @"[A-Za-z_][\w.-]*:", string.Empty);
        s = s.Replace("//", "/").Trim('/');
        s = Regex.Replace(s, @"\[\d+\]", string.Empty);
        return s;
    }

    private static string Quote(string value)
        => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static BindingMap CreateDefault() => new()
    {
        Mappings = new List<Entry>
        {
            new() { Match = "//cbc:Number", Expr = "model.Document.Number" },
            new() { Match = "//cbc:UUID", Expr = "model.Document.CUFE" },
            new() { Match = "//cbc:IssueDate", Expr = "model.Document.IssueDate" },
            new() { Match = "//cbc:DueDate", Expr = "model.Document.DueDate" },
            new() { Match = "//cbc:Note", Expr = "model.Additional.Notes" },

            new() { Match = "//Supplier/Name", Expr = "model.Issuer.Name" },
            new() { Match = "//Supplier/TaxId", Expr = "model.Issuer.TaxId" },
            new() { Match = "//Supplier/Address", Expr = "model.Issuer.Address" },
            new() { Match = "//Supplier/City", Expr = "model.Issuer.City" },
            new() { Match = "//Supplier/Phone", Expr = "model.Issuer.Phone" },
            new() { Match = "//Supplier/Email", Expr = "model.Issuer.Email" },

            new() { Match = "//Customer/Name", Expr = "model.Customer.Name" },
            new() { Match = "//Customer/TaxId", Expr = "model.Customer.TaxId" },
            new() { Match = "//Customer/Address", Expr = "model.Customer.Address" },
            new() { Match = "//Customer/City", Expr = "model.Customer.City" },
            new() { Match = "//Customer/Phone", Expr = "model.Customer.Phone" },
            new() { Match = "//Customer/Email", Expr = "model.Customer.Email" },

            new() { Match = "//Totals/Total", Expr = "model.Totals.Total" },
            new() { Match = "//Totals/Subtotal", Expr = "model.Totals.Subtotal" },
            new() { Match = "//Totals/TotalTax", Expr = "model.Totals.TotalTax" },
            new() { Match = "//Totals/TotalDiscount", Expr = "model.Totals.TotalDiscount" },

            new() { Match = "//Line/Description", Expr = "line.Description" },
            new() { Match = "//Line/Quantity", Expr = "line.Quantity" },
            new() { Match = "//Line/UnitPrice", Expr = "line.UnitPrice" },
            new() { Match = "//Line/LineTotal", Expr = "line.LineTotal" },
            new() { Match = "//Line/TaxRate", Expr = "line.TaxRate" },
            new() { Match = "//Line/TaxValor", Expr = "line.TaxValor" },
            new() { Match = "//Line/Unit", Expr = "line.Unit" },
            new() { Match = "//Line/Codigo_cd", Expr = "line.Codigo_cd" },
            new() { Match = "//Line/Discount", Expr = "line.Discount" },
            new() { Match = "//Line/ValorDescuento", Expr = "line.ValorDescuento" },
            new() { Match = "//Line/PrecioAntesIVA", Expr = "line.PrecioAntesIVA" },
            new() { Match = "//Line/Ibua", Expr = "line.Ibua" }
        }
    };
}
