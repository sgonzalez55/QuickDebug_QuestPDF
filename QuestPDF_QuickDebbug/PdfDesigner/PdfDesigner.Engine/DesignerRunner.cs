using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using PdfQuickDebug.Designer.Rendering;
using QuestPDF.Fluent;

namespace PdfQuickDebug.Designer;

/// <summary>Punto de entrada del POC: XML + design.json -> PDF.</summary>
public static class DesignerRunner
{
    public static byte[] Render(string xmlPath, string designJsonPath)
    {
        var design = DesignerJson.Load(designJsonPath);
        var xml = File.ReadAllText(xmlPath);

        var resolver = new XmlDataResolver(xml, design.Namespaces, msg => Console.WriteLine($"   ⚠️  {msg}"));
        var renderer = new DesignDocumentRenderer(design, resolver, log: msg => Console.WriteLine($"   ⚠️  {msg}"));

        return renderer.GeneratePdf();
    }

    public static void Run(string xmlPath, string designJsonPath, string outputPdf)
    {
        Console.WriteLine("\n🎨 [DESIGNER] Generando PDF desde diseño JSON...");
        Console.WriteLine($"   XML:    {xmlPath}");
        Console.WriteLine($"   Diseño: {designJsonPath}");

        if (!File.Exists(xmlPath))
        {
            Console.WriteLine($"   ❌ XML no encontrado: {xmlPath}");
            return;
        }

        if (!File.Exists(designJsonPath))
        {
            Console.WriteLine($"   ❌ Diseño JSON no encontrado: {designJsonPath}");
            return;
        }

        var bytes = Render(xmlPath, designJsonPath);
        File.WriteAllBytes(outputPdf, bytes);

        Console.WriteLine($"   ✅ PDF generado: {outputPdf} ({bytes.Length / 1024} KB)");
    }
}
