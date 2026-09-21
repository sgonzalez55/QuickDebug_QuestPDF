using System.Text.Json;
using System.Text.RegularExpressions;
using PdfQuickDebug.Designer.CodeGen;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>Config opcional por cliente (client.json).</summary>
public sealed class ClientConfig
{
    public string Nit { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string LastDesign { get; set; } = string.Empty;
}

/// <summary>
/// Manejo de clientes. Cada cliente tiene DOS carpetas:
///   data\clients\<Cliente>\   → solo las entregas: ubl.xslt, dto\DtoUbl.cs, templates\<Cliente>.cs
///   data\work\<Cliente>\      → estado de la herramienta: raw.xml, transformed.xml,
///                               binding-map.json, data-contract.json, client.json y el diseño.
/// </summary>
public static class ClientData
{
    public static readonly string RootDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static readonly string DataDir = Path.Combine(RootDir, "data");
    public static readonly string ClientsDir = Path.Combine(DataDir, "clients");
    public static readonly string WorkDirBase = Path.Combine(DataDir, "work");
    public static readonly string FallbackSampleXml = Path.Combine(DataDir, "factura.sample.xml");
    public static readonly string BaseXslt = Path.Combine(DataDir, "FACTURA-UBL.xslt");
    public static readonly string ReferenceXslt = Path.Combine(Dir("AJECOLOMBIA"), "ubl.xslt");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static List<string> ListClients()
    {
        if (!Directory.Exists(ClientsDir)) return new List<string>();
        return Directory.GetDirectories(ClientsDir)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Select(n => n!)
            .ToList();
    }

    public static string Dir(string clientName) => Path.Combine(ClientsDir, clientName);

    public static string WorkDir(string clientName) => Path.Combine(WorkDirBase, clientName);

    // Entregas por cliente (clients\<Cliente>\).
    public static string XsltPath(string clientName) => Path.Combine(Dir(clientName), "ubl.xslt");
    public static string DtoPath(string clientName) => Path.Combine(Dir(clientName), "dto", "DtoUbl.cs");
    public static string TemplatesDir(string clientName) => Path.Combine(Dir(clientName), "templates");
    public static string TemplatePath(string clientName) => Path.Combine(TemplatesDir(clientName), "FacturaUbl.cs");

    // Estado de la herramienta (work\<Cliente>\).
    public static string RawXmlPath(string clientName) => Path.Combine(WorkDir(clientName), "raw.xml");
    public static string TransformedXmlPath(string clientName) => Path.Combine(WorkDir(clientName), "transformed.xml");
    public static string BindingMapPath(string clientName) => Path.Combine(WorkDir(clientName), "binding-map.json");
    public static string DataContractPath(string clientName) => Path.Combine(WorkDir(clientName), "data-contract.json");
    public static string ClientConfigPath(string clientName) => Path.Combine(WorkDir(clientName), "client.json");
    public static string DefaultDesignPath(string clientName) => Path.Combine(WorkDir(clientName), clientName + ".design.json");

    public static bool Exists(string clientName) => !string.IsNullOrWhiteSpace(clientName) && Directory.Exists(Dir(clientName));

    /// <summary>
    /// Intenta extraer el NIT del proveedor (emisor) desde el XML crudo del cliente.
    /// Orden de patrones: atributo nit_cd, cbc:CompanyID del supplier (independiente
    /// del prefijo), o atributo nit genérico.
    /// </summary>
    public static string TryExtractNit(string rawXml)
    {
        if (string.IsNullOrWhiteSpace(rawXml)) return string.Empty;

        var patterns = new[]
        {
            @"nit_cd\s*=\s*""(\d+)""",
            @"CompanyID[^>]*>\s*(\d+)\s*<",
            @"<nit[^>]*>\s*(\d+)\s*<",
            @"nit\s*=\s*""(\d+)"""
        };
        foreach (var pattern in patterns)
        {
            var m = Regex.Match(rawXml, pattern, RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
        }
        return string.Empty;
    }

    /// <summary>Crea las carpetas del cliente: entregas (xslt + binding defaults) y trabajo.</summary>
    public static void CreateClient(string clientName)
    {
        var dir = Dir(clientName);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(WorkDir(clientName));

        if (!File.Exists(XsltPath(clientName)))
        {
            var source = File.Exists(BaseXslt) ? BaseXslt
                : File.Exists(ReferenceXslt) ? ReferenceXslt
                : null;
            if (source != null)
                File.Copy(source, XsltPath(clientName));
        }

        if (!File.Exists(BindingMapPath(clientName)))
            BindingMap.CreateDefault().Save(BindingMapPath(clientName));
    }

    public static ClientConfig LoadConfig(string clientName)
    {
        if (!Exists(clientName)) return new ClientConfig();
        try
        {
            var p = ClientConfigPath(clientName);
            if (File.Exists(p))
            {
                var result = JsonSerializer.Deserialize<ClientConfig>(File.ReadAllText(p), Options) ?? new ClientConfig();
                result.Nit ??= string.Empty;
                result.ClientName ??= string.Empty;
                result.LastDesign ??= string.Empty;
                return result;
            }
        }
        catch
        {
            // configuración opcional
        }
        return new ClientConfig();
    }

    public static void SaveConfig(string clientName, ClientConfig config)
    {
        try
        {
            Directory.CreateDirectory(WorkDir(clientName));
            config.Nit ??= string.Empty;
            config.ClientName ??= string.Empty;
            config.LastDesign ??= string.Empty;
            File.WriteAllText(ClientConfigPath(clientName), JsonSerializer.Serialize(config, Options));
        }
        catch
        {
            // configuración opcional
        }
    }

    public static void RememberLastDesign(string clientName, string designPath)
    {
        var cfg = LoadConfig(clientName);
        cfg.LastDesign = designPath;
        SaveConfig(clientName, cfg);
    }

    /// <summary>
    /// Migra los datos heredados de data\ al cliente AJECOLOMBIA (una sola vez).
    /// Devuelve la lista de clientes existentes tras la migración.
    /// </summary>
    public static List<string> EnsureMigration()
    {
        var createdNew = false;
        if (!Directory.Exists(ClientsDir))
        {
            Directory.CreateDirectory(Dir("AJECOLOMBIA"));
            createdNew = true;
        }

        var target = Dir("AJECOLOMBIA");
        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
            createdNew = true;
        }

        // Archivos heredados de data\ → cliente AJECOLOMBIA
        var moves = new[]
        {
            ("FACTURA-UBL.xslt", "ubl.xslt"),
            ("NUEVO_XML.xml", "raw.xml"),
            ("factura.transformed.xml", "transformed.xml"),
            ("binding-map.json", "binding-map.json"),
            ("data-contract.json", "data-contract.json")
        };
        foreach (var (src, dst) in moves)
        {
            var from = Path.Combine(DataDir, src);
            var to = Path.Combine(target, dst);
            if (File.Exists(from) && !File.Exists(to))
                File.Move(from, to);
        }

        // Diseños heredados (*.design.json) → también a AJECOLOMBIA (soporta varios por cliente).
        foreach (var from in Directory.GetFiles(DataDir, "*.design.json"))
        {
            var to = Path.Combine(target, Path.GetFileName(from));
            if (!File.Exists(to))
                File.Move(from, to);
        }

        // client.json de referencia con los datos del diseño migrado.
        var cfgPath = Path.Combine(target, "client.json");
        if (!File.Exists(cfgPath))
        {
            var design = Path.Combine(target, "factura.design.json");
            var (nit, cn) = File.Exists(design) ? ReadDesignMeta(design) : default;
            SaveConfig("AJECOLOMBIA", new ClientConfig
            {
                Nit = nit,
                ClientName = string.IsNullOrWhiteSpace(cn) ? "AJECOLOMBIA" : cn
            });
        }

        if (createdNew || !File.Exists(Path.Combine(target, "binding-map.json")))
        {
            if (!File.Exists(Path.Combine(target, "binding-map.json")))
                BindingMap.CreateDefault().Save(Path.Combine(target, "binding-map.json"));
            if (!File.Exists(Path.Combine(target, "ubl.xslt")) && File.Exists(BaseXslt))
                File.Copy(BaseXslt, Path.Combine(target, "ubl.xslt"));
        }

        MigrateToWorkLayout();

        return ListClients();
    }

    /// <summary>
    /// Fase 2: separa las entregas (se quedan en clients\&lt;Cliente&gt;\: ubl.xslt, dto\, templates\)
    /// del estado de la herramienta (raw/transformed/binding/contract/config/diseños → work\&lt;Cliente&gt;\).
    /// Además, si el cliente tiene dto\DtoUbl.cs heredado en output\_&lt;nit&gt;\, lo copia como baseline.
    /// </summary>
    private static void MigrateToWorkLayout()
    {
        foreach (var client in ListClients())
        {
            var src = Dir(client);
            var wd = WorkDir(client);
            Directory.CreateDirectory(wd);

            foreach (var file in Directory.GetFiles(src))
            {
                var name = Path.GetFileName(file);
                if (name == "ubl.xslt") continue; // entrega: queda con el cliente
                var to = Path.Combine(wd, name);
                if (!File.Exists(to))
                    File.Move(file, to);
            }

            // Baseline del DTO desde el output heredado (output\_<nit>\DtoUbl.cs).
            if (!File.Exists(DtoPath(client)))
            {
                var cfg = LoadConfig(client);
                var legacy = Path.Combine(RootDir, "output", $"_{cfg.Nit}", "DtoUbl.cs");
                if (File.Exists(legacy))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DtoPath(client))!);
                    File.Copy(legacy, DtoPath(client));
                }
            }
        }
    }

    private static (string Nit, string ClientName) ReadDesignMeta(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
            if (doc.RootElement.TryGetProperty("metadata", out var meta))
            {
                var nit = meta.TryGetProperty("nit", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var cn = meta.TryGetProperty("clientName", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                return (nit, cn);
            }
        }
        catch
        {
            // metadata opcional
        }
        return (string.Empty, string.Empty);
    }

    /// <summary>Lista los diseños .design.json del cliente (nombre + ruta).</summary>
    public static List<(string Name, string Path)> ListDesigns(string clientName)
    {
        var dir = WorkDir(clientName);
        if (!Directory.Exists(dir)) return new List<(string, string)>();
        return Directory.GetFiles(dir, "*.design.json")
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Select(p => (Path.GetFileNameWithoutExtension(p), Path.GetFullPath(p)))
            .ToList();
    }
}