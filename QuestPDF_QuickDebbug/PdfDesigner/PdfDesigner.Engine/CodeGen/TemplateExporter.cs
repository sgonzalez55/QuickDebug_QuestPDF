using PdfQuickDebug.Designer.Model;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Escribe el template generado a disco, listo para compilar (local) o para
/// copiar al servicio Services.PdfGenerator (con reescritura de namespaces).
/// </summary>
public static class TemplateExporter
{
    /// <summary>Genera el .cs con namespaces locales (PdfQuickDebug.*).</summary>
    public static string Export(ReportDesign design, BindingMap? map, string outputDir)
    {
        map ??= BindingMap.CreateDefault();
        var className = DesignToCSharpGenerator.GenerateClassName(design);
        var code = DesignToCSharpGenerator.Generate(design, map, className);

        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, className + ".cs");
        File.WriteAllText(path, code);
        return path;
    }

    /// <summary>Genera el .cs y lo escribe en la ruta exacta indicada.</summary>
    public static string ExportToFile(ReportDesign design, BindingMap? map, string outputPath)
    {
        map ??= BindingMap.CreateDefault();
        var className = DesignToCSharpGenerator.GenerateClassName(design);
        var code = DesignToCSharpGenerator.Generate(design, map, className);

        File.WriteAllText(outputPath, code);
        return outputPath;
    }

    /// <summary>Genera el .cs con namespaces de Services.PdfGenerator en carpeta _&lt;NIT&gt;.</summary>
    public static string ExportToService(ReportDesign design, BindingMap? map, string outputDir)
    {
        map ??= BindingMap.CreateDefault();
        var className = DesignToCSharpGenerator.GenerateClassName(design);
        var nit = string.IsNullOrWhiteSpace(design.Metadata.Nit) ? "000000" : design.Metadata.Nit;
        var ns = $"Services.PdfGenerator.Infrastructure.Templates._{nit}";

        var code = DesignToCSharpGenerator.Generate(design, map, className, ns)
            .Replace("using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;");

        var folder = Path.Combine(outputDir, $"_{nit}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, className + ".cs");
        File.WriteAllText(path, code);
        return path;
    }

    /// <summary>Genera el .cs de la entrega por cliente (templates\FacturaUbl.cs), con
    /// clase siempre FacturaUbl (igual que instancia Program.cs de PdfQuickDebug) y
    /// namespace del servicio Services.PdfGenerator.Infrastructure.Templates._&lt;NIT&gt;.</summary>
    public static string ExportToClient(ReportDesign design, BindingMap? map, string outputPath)
    {
        map ??= BindingMap.CreateDefault();
        const string className = "FacturaUbl";
        var nit = string.IsNullOrWhiteSpace(design.Metadata.Nit) ? "000000" : design.Metadata.Nit;
        var ns = $"Services.PdfGenerator.Infrastructure.Templates._{nit}";

        var code = DesignToCSharpGenerator.Generate(design, map, className, ns)
            .Replace("using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, code);
        return outputPath;
    }
}
