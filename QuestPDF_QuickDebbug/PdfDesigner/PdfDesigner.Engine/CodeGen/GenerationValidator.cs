using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfQuickDebug.Designer.CodeGen;

/// <summary>
/// Valida los entregables generados del diseñador antes de darlos por buenos:
///   1) Reglas de contenido sobre el .cs (sin SkiaSharp/ZXing.SkiaSharp, con
///      BrandColors/InvoiceTableStyles al final).
///   2) Smoke-compile: compila un template generado contra el proyecto
///      PdfQuickDebug (QuestPDF + core) para detectar CS0103/CS0246 al momento.
/// </summary>
public static class GenerationValidator
{
    private static readonly Regex ForbiddenSymbol = new(
        @"\b(SkiaSharp|SKSvgCanvas|BarcodeWriter|SKPaint|SKCanvas|SKSvg|ZXing\.SkiaSharp)\b",
        RegexOptions.Compiled);

    /// <summary>Patrones que NO deben aparecer en el template entregable.</summary>
    public static IReadOnlyList<string> Forbidden { get; } = new[]
    {
        "SkiaSharp", "ZXing.SkiaSharp", "SKSvgCanvas", "BarcodeWriter", "SKPaint", "SKCanvas", "SKSvg"
    };

    /// <summary>Clases compartidas que el template debe emitir (usadas por NotasCredito/FacturaUBL2).</summary>
    public static IReadOnlyList<string> Required { get; } = new[]
    {
        "static class BrandColors", "static class InvoiceTableStyles"
    };

    /// <summary>Devuelve los problemas de contenido del código; vacío si está OK.</summary>
    public static IReadOnlyList<string> CheckContent(string code)
    {
        var issues = new List<string>();
        foreach (var forbidden in Forbidden)
            if (code.Contains(forbidden, StringComparison.Ordinal))
                issues.Add($"Contiene referencia a '{forbidden}' (se eliminó SkiaSharp de la salida).");
        foreach (var required in Required)
            if (!code.Contains(required, StringComparison.Ordinal))
                issues.Add($"Falta '{required}' al final del template (lo usa NotasCredito.cs / FacturaUBL2.cs).");
        return issues;
    }

    /// <summary>Devuelve los errores de compilación (CS/MSB) al compilar el código generado contra PdfQuickDebug.</summary>
    public static IReadOnlyList<string> SmokeCompileTemplate(string? scratchDir, string className, string code)
    {
        if (string.IsNullOrWhiteSpace(scratchDir)) throw new ArgumentNullException(nameof(scratchDir));
        Directory.CreateDirectory(scratchDir);
        foreach (var f in Directory.GetFiles(scratchDir, "*.cs"))
            File.Delete(f);
        File.WriteAllText(Path.Combine(scratchDir, className + ".cs"), code);

        var csproj = Path.Combine(scratchDir, "ValidationScratch.csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <LangVersion>latest</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\..\..\DebugPDF\PdfQuickDebug\PdfQuickDebug.csproj" />
              </ItemGroup>
            </Project>
            """);

        var psi = new ProcessStartInfo("dotnet", $"build \"{csproj}\" -v q --nologo")
        {
            WorkingDirectory = scratchDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi);
        var sb = new StringBuilder();
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        if (!proc.WaitForExit(120000))
        {
            proc.Kill();
            return new[] { "La compilación expiró (120s)." };
        }

        var errors = sb.ToString()
            .Split('\n')
            .Where(l => l.Contains("error CS") || l.Contains("error MSB"))
            .Take(25)
            .ToList();
        if (errors.Count == 0 && proc.ExitCode == 0)
            return Array.Empty<string>();
        return errors.Count > 0 ? errors : new[] { $"dotnet build salió con código {proc.ExitCode}." };
    }
}