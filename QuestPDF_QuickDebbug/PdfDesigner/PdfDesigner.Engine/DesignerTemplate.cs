using PdfQuickDebug.Core;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using PdfQuickDebug.Designer.Rendering;
using QuestPDF.Fluent;

namespace PdfQuickDebug.Designer;

/// <summary>
/// Adapter del motor de diseño al contrato IInvoiceTemplate existente.
/// A diferencia de los templates tipados (FacturaUbl), este enlaza por XPath
/// contra el XML, por lo que requiere el XML de origen.
/// </summary>
public sealed class DesignerTemplate : IInvoiceTemplate
{
    private readonly ReportDesign _design;
    private readonly string? _xml;

    public DesignerTemplate(ReportDesign design, string? xml = null)
    {
        _design = design ?? throw new ArgumentNullException(nameof(design));
        _xml = xml;
    }

    public static DesignerTemplate FromFiles(string designJsonPath, string? xmlPath = null)
        => new(
            DesignerJson.Load(designJsonPath),
            xmlPath != null ? File.ReadAllText(xmlPath) : null);

    /// <summary>Genera el PDF a partir del XML crudo enlazado por el diseño.</summary>
    public byte[] GeneratePdfFromXml(string xml)
    {
        var resolver = new XmlDataResolver(xml, _design.Namespaces);
        var renderer = new DesignDocumentRenderer(_design, resolver);
        return renderer.GeneratePdf();
    }

    public byte[] GeneratePdf(InvoiceModel model)
    {
        if (string.IsNullOrEmpty(_xml))
        {
            throw new InvalidOperationException(
                "DesignerTemplate enlaza por XPath: construye con XML o usa GeneratePdfFromXml(xml).");
        }

        return GeneratePdfFromXml(_xml);
    }

    public TemplateMetadata GetMetadata() => new()
    {
        Nit = _design.Namespaces.TryGetValue("nit", out var nit) ? nit : string.Empty,
        TemplateName = "designer-template",
        ClientName = "Designer",
        Version = _design.Version,
        LastUpdated = DateTime.UtcNow
    };

    public InvoiceModel MapToCustomDto(InvoiceModel baseModel) => baseModel;
}
