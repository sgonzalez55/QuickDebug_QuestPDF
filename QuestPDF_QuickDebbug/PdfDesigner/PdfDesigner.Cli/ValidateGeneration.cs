using System.Text.Json;
using System.Text.RegularExpressions;
using PdfQuickDebug.Designer.CodeGen;
using PdfQuickDebug.Designer.Model;

namespace PdfDesigner.Cli;

/// <summary>
/// Valida la generación de los entregables contra el proyecto PdfQuickDebug:
///   1) DTO — regenera dto\DtoUbl.cs sin duplicar namespaces (merge seguro).
///   2) Template .cs — genera el archivo local y lo compila contra PdfQuickDebug (QuestPDF 2024.12.3).
/// </summary>
internal static class ValidateGeneration
{
    public static void Run(string root)
    {
        var clientsDir = Path.Combine(root, "data", "clients");
        var workDir = Path.Combine(root, "data", "work");
        var clients = DiscoverClients(workDir, clientsDir);
        var failed = false;

        Console.WriteLine("== 1) DTO (merge) ==");
        foreach (var client in clients)
            failed |= RegenerateDto(workDir, clientsDir, client);

        Console.WriteLine("\n== 2) Template .cs (compilación vs PdfQuickDebug) ==");
        failed |= SmokeCompileTemplates(root, workDir, clients);

        Console.WriteLine(failed
            ? "\nVALIDACIÓN: se detectaron problemas. Revisa la salida arriba."
            : "\nVALIDACIÓN: TODO OK.");
        Environment.ExitCode = failed ? 1 : 0;
    }

    private static string[] DiscoverClients(string workDir, string clientsDir)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(workDir))
            foreach (var d in Directory.GetDirectories(workDir)) names.Add(Path.GetFileName(d));
        if (Directory.Exists(clientsDir))
            foreach (var d in Directory.GetDirectories(clientsDir)) names.Add(Path.GetFileName(d));
        if (names.Count == 0) names.Add("CORRAL");
        return names.ToArray();
    }

    /// <summary>Regenera el DTO con el merge corregido y verifica un único namespace y clase.</summary>
    private static bool RegenerateDto(string workDir, string clientsDir, string client)
    {
        var work = Path.Combine(workDir, client);
        var contractPath = Path.Combine(work, "data-contract.json");
        var clientJsonPath = Path.Combine(work, "client.json");
        var dtoPath = Path.Combine(clientsDir, client, "dto", "DtoUbl.cs");

        if (!File.Exists(contractPath))
        {
            Console.WriteLine($"  {client}: SIN contrato (skipped)");
            return false;
        }

        var contract = DataContract.Load(contractPath);
        if (contract == null)
        {
            Console.WriteLine($"  {client}: contrato no parseado (skipped)");
            return false;
        }

        var nit = ReadNit(clientJsonPath);
        if (string.IsNullOrWhiteSpace(nit)) nit = ReadDesignNit(work, client);
        if (string.IsNullOrWhiteSpace(nit))
        {
            Console.WriteLine($"  {client}: sin NIT (client.json o diseño) — no se regenera");
            return true;
        }
        var before = File.Exists(dtoPath) ? File.ReadAllText(dtoPath) : string.Empty;
        var code = DtoToCSharpGenerator.Generate(contract, dtoPath, nit);
        File.WriteAllText(dtoPath, code);

        var nsCount = Regex.Matches(code, @"\bnamespace\b").Count;
        var classCount = Regex.Matches(code, @"\bclass\s+DtoUbl\b").Count;
        var ok = nsCount == 1 && classCount == 1 && !string.IsNullOrWhiteSpace(nit);

        Console.WriteLine($"  {client}: namespace={nsCount}  class={classCount}  nit='{nit}'  {(ok ? "OK" : "!! ERROR")}");
        if (before != code)
            Console.WriteLine($"    → regenerado: {dtoPath} ({code.Length} chars)");
        return !ok;
    }

    /// <summary>Genera el template local (.cs) por cliente y lo compila contra el proyecto debug.</summary>
    private static bool SmokeCompileTemplates(string root, string workDir, string[] clients)
    {
        var scratch = Path.Combine(root, "output", "ValidationScratch");
        var generatedAny = false;
        var hadErrors = false;

        foreach (var client in clients)
        {
            var work = Path.Combine(workDir, client);
            var mapPath = Path.Combine(work, "binding-map.json");
            if (!File.Exists(mapPath))
            {
                Console.WriteLine($"  {client}: sin binding-map (skipped)");
                continue;
            }

            var designPath = ResolveDesign(work, client);
            if (designPath == null)
            {
                Console.WriteLine($"  {client}: sin diseño .design.json (skipped)");
                continue;
            }

            var design = DesignerJson.Load(designPath);
            var map = BindingMap.Load(mapPath);
            var className = DesignToCSharpGenerator.GenerateClassName(design);
            var code = DesignToCSharpGenerator.Generate(design, map, className);

            // — Validación de contenido (A2) —
            var contentIssues = GenerationValidator.CheckContent(code);
            if (contentIssues.Count > 0)
            {
                Console.WriteLine($"  {client}: problemas de contenido:");
                foreach (var issue in contentIssues) Console.WriteLine("    " + issue);
                hadErrors = true;
            }

            generatedAny = true;
            Console.WriteLine($"  Generado {client} → {className}.cs ({code.Length} chars)");

            // — Compilación aislada (A1) —
            var compileErrors = GenerationValidator.SmokeCompileTemplate(scratch, className, code);
            if (compileErrors.Count > 0)
            {
                hadErrors = true;
                Console.WriteLine($"  Compilación {client}: FALLOS →");
                foreach (var e in compileErrors) Console.WriteLine("    " + e.Trim());
            }
            else
                Console.WriteLine($"  Compilación {client}: OK (el .cs generado compila contra PdfQuickDebug + QuestPDF 2024.12.3)");
        }

        if (!generatedAny)
        {
            Console.WriteLine("  No hay diseños válidos para compilar.");
            return false;
        }

        return hadErrors;
    }

    /// <summary>Elige el diseño activo del cliente: factura.design.json, &lt;client&gt;.design.json o el primero.</summary>
    private static string? ResolveDesign(string work, string client)
    {
        var byClient = Path.Combine(work, client + ".design.json");
        var byFactura = Path.Combine(work, "factura.design.json");
        if (File.Exists(byFactura)) return byFactura;
        if (!File.Exists(byClient)) return Directory.Exists(work) ? Directory.GetFiles(work, "*.design.json").FirstOrDefault() : null;
        return byClient;
    }

    private static string ReadDesignNit(string work, string client)
    {
        var design = ResolveDesign(work, client);
        if (design == null) return string.Empty;
        try { return DesignerJson.Load(design).Metadata?.Nit ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static string ReadNit(string clientJsonPath)
    {
        if (!File.Exists(clientJsonPath)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(clientJsonPath));
            return doc.RootElement.TryGetProperty("nit", out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString() ?? string.Empty
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}