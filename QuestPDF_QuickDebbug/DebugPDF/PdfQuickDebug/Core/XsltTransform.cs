using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace PdfQuickDebug.Core;

/// <summary>
/// Servicio standalone para transformación XSLT
/// </summary>
public class XsltTransform
{
    /// <summary>
    /// Aplica transformación XSLT a un XML UBL
    /// </summary>
    public string Transform(string xmlContent, string xsltContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be empty", nameof(xmlContent));

        if (string.IsNullOrWhiteSpace(xsltContent))
            throw new ArgumentException("XSLT content cannot be empty", nameof(xsltContent));

        try
        {
            // Compilar XSLT
            var xslt = new XslCompiledTransform();
            var settings = new XsltSettings(enableDocumentFunction: true, enableScript: false);

            using var xsltReader = XmlReader.Create(new StringReader(xsltContent));
            xslt.Load(xsltReader, settings, null);

            // Transformar XML
            using var xmlReader = XmlReader.Create(new StringReader(xmlContent));
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                ConformanceLevel = ConformanceLevel.Fragment, // Permite múltiples elementos raíz
                Encoding = Encoding.UTF8
            });

            xslt.Transform(xmlReader, xmlWriter);

            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error transforming XML with XSLT: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Valida que el XML transformado tenga la estructura correcta (CFD y Adicional)
    /// </summary>
    public (bool IsValid, string Message) ValidateTransformedXml(string transformedXml)
    {
        if (string.IsNullOrWhiteSpace(transformedXml))
            return (false, "XML transformado está vacío");

        try
        {
            var wrappedXml = $"<Root>{transformedXml}</Root>";
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(wrappedXml);

            var cfd = doc.SelectSingleNode("//CFD");
            var adicional = doc.SelectSingleNode("//Adicional");

            if (cfd == null)
                return (false, "Elemento <CFD> no encontrado");

            if (adicional == null)
                return (false, "Elemento <Adicional> no encontrado");

            // Verificar CustomFields
            var customFields = adicional.SelectNodes("Campo");
            var fieldCount = customFields?.Count ?? 0;

            return (true, $"XML válido: CFD encontrado, Adicional con {fieldCount} CustomFields");
        }
        catch (Exception ex)
        {
            return (false, $"Error validando XML: {ex.Message}");
        }
    }
}
