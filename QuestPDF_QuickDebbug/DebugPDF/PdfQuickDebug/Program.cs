using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfQuickDebug.Core;
using PdfQuickDebug.Templates;
using QuestPDF.Drawing;

namespace PdfQuickDebug;

class Program
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // 📌 CONFIGURACIÓN - SOLO EDITA ESTA SECCIÓN
    // ═══════════════════════════════════════════════════════════════════════════════

    // Rutas de archivos (COPIAR/PEGAR aquí tus rutas)
    const string XML_PATH = @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\DataTest\NUEVO_XML.xml";
    const string XSLT_PATH = @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\DataTest\FACTURA-UBL.xslt";
    const string OUTPUT_PDF = @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\DataTest\DEBUG_OUTPUT.pdf";

    // NIT del cliente (para seleccionar template)
    const string NIT = "800153993";
        
    // ⚙️ EXPORTAR TEMPLATE PARA EL SERVICIO (AUTOMÁTICO)
    const bool EXPORTAR_TEMPLATE = true;  // true = exporta template con namespaces correctos
    const string EXPORT_BASE_PATH = @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\DataTest";  // Carpeta base de exportación
    const string SERVICE_TEMPLATE_PATH = @"E:\DV-SCM\QuestPDF_QuickDebbug\DataTest\FacturaUbl.cs"; //ojo cambiar por el de aje o el que este trabajando FacturaUbl

    // QuestPDF Companion (Hot Reload visual en navegador)
    const bool ENABLE_COMPANION = true;  // true = abre http://localhost:12500
    const int COMPANION_PORT = 12500;

    // ═══════════════════════════════════════════════════════════════════════════════
    // 🚀 MAIN - NO TOCAR (a menos que sepas lo que haces)
    // ═══════════════════════════════════════════════════════════════════════════════

    static void Main(string[] args)
    {
        // Configurar QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFont(File.OpenRead("Assets/Fonts/LiberationSans-Regular.ttf"));

        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           PDF QUICK DEBUG - DEBUGGING TOOL                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // PASO 1: CARGAR ARCHIVOS
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine("📂 [1/5] Cargando archivos...");
            Console.WriteLine($"   XML:  {XML_PATH}");
            Console.WriteLine($"   XSLT: {XSLT_PATH}");

            if (!File.Exists(XML_PATH))
            {
                Console.WriteLine($"❌ ERROR: XML no encontrado en: {XML_PATH}");
                Console.WriteLine("   Verifica la ruta en la línea 13 del código");
                return;
            }

            if (!File.Exists(XSLT_PATH))
            {
                Console.WriteLine($"❌ ERROR: XSLT no encontrado en: {XSLT_PATH}");
                Console.WriteLine("   Verifica la ruta en la línea 14 del código");
                return;
            }

            var xml = File.ReadAllText(XML_PATH);
            var xslt = File.ReadAllText(XSLT_PATH);
            Console.WriteLine($"   ✅ XML cargado ({xml.Length / 1024} KB)");
            Console.WriteLine($"   ✅ XSLT cargado ({xslt.Length / 1024} KB)");

            // ═══════════════════════════════════════════════════════════════════
            // PASO 2: TRANSFORMAR XML CON XSLT
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine("\n⚙️  [2/5] Transformando XML con XSLT...");
            var transformer = new XsltTransform();
            var transformedXml = transformer.Transform(xml, xslt);
            Console.WriteLine($"   ✅ Transformación exitosa ({transformedXml.Length} caracteres)");

            // Validar estructura
            var (isValid, message) = transformer.ValidateTransformedXml(transformedXml);
            Console.WriteLine($"   {(isValid ? "✅" : "❌")} {message}");

            if (!isValid)
            {
                Console.WriteLine("\n❌ El XML transformado no es válido. No se puede continuar.");
                return;
            }

            // ═══════════════════════════════════════════════════════════════════
            // PASO 3: PARSEAR A MODELO
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine("\n📊 [3/5] Parseando XML a InvoiceModel...");
            var model = XmlParser.Parse(transformedXml);

            Console.WriteLine($"   ✅ Documento: {model.Document.Prefix}{model.Document.Number}");
            Console.WriteLine($"   ✅ Cliente: {model.Customer.Name}");
            Console.WriteLine($"   ✅ NIT Cliente: {model.Customer.TaxId}");
            Console.WriteLine($"   ✅ Total: ${model.Totals.Total:N0}");
            Console.WriteLine($"   ✅ Líneas: {model.Lines.Count}");
            Console.WriteLine($"   ✅ CustomFields: {model.Additional.CustomFields.Count}");

            // Mostrar CustomFields importantes
            if (model.Additional.CustomFields.Any())
            {
                Console.WriteLine("\n   📋 CustomFields encontrados:");
                foreach (var field in model.Additional.CustomFields.Take(5))
                {
                    var displayValue = field.Value.Length > 50
                        ? field.Value.Substring(0, 50) + "..."
                        : field.Value;
                    Console.WriteLine($"      • {field.Key}: {displayValue}");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // PASO 4: MAPEAR A DTO PERSONALIZADO Y GENERAR PDF
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine("\n📄 [4/5] Generando PDF...");
            var template = new FacturaUbl();
            var templateNotaCredito = new NotasCredito();

            // NUEVO FLUJO: Mapear InvoiceModel genérico a DtoUbl personalizado
            // Esto extrae los CustomFields y los mapea a propiedades tipadas
            var customModel = template.MapToCustomDto(model);
            Console.WriteLine($"   ✅ Modelo mapeado a: {customModel.GetType().Name}");

            // Si es DtoUbl, mostrar campos personalizados mapeados
            if (customModel is DtoUbl dto)
            {
                Console.WriteLine($"   📋 Campos personalizados del DTO:");
                Console.WriteLine($"      • Zona: {dto.Zona}");
                Console.WriteLine($"      • Ruta: {dto.Ruta}");
                Console.WriteLine($"      • NumPedido: {dto.NumPedido}");
                Console.WriteLine($"      • NumCargue: {dto.NumCargue}");
                Console.WriteLine($"      • PlacaVehiculo: {dto.PlacaVehiculo}");
                Console.WriteLine($"      • OrdenCompra: {dto.OrdenCompra}");
                Console.WriteLine($"      • HoraGeneracion: {dto.HoraGeneracion}");
                Console.WriteLine($"      • ValorLetras: {(dto.ValorLetras.Length > 40 ? dto.ValorLetras.Substring(0, 40) + "..." : dto.ValorLetras)}");
            }

            // template.PreviewOnQuestPDFCompanion(customModel); //Descomentar para debbuguear en QuestPDF Companion
            // templateNotaCredito.PreviewOnQuestPDFCompanion(customModel); //Descomentar para debbuguear en QuestPDF Companion
            var pdfBytes = template.GeneratePdf(customModel);

            File.WriteAllBytes(OUTPUT_PDF, pdfBytes);
            Console.WriteLine($"   ✅ PDF generado exitosamente");
            Console.WriteLine($"   📁 Ubicación: {OUTPUT_PDF}");
            Console.WriteLine($"   📦 Tamaño: {pdfBytes.Length / 1024} KB");

            // ═══════════════════════════════════════════════════════════════════
            // PASO 5: DEBUGGING CON VS CODE
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine("\n✅ [5/5] TODO LISTO - PDF generado exitosamente");
            Console.WriteLine();
            Console.WriteLine("🐛 DEBUGGING CON VS CODE:");
            Console.WriteLine("   1. Abre este proyecto en VS Code");
            Console.WriteLine("   2. Pon breakpoints haciendo click en el número de línea");
            Console.WriteLine("   3. Presiona F5 para ejecutar con debugging");
            Console.WriteLine("   4. Inspecciona variables: model, transformedXml, pdfBytes, etc.");
            Console.WriteLine();
            Console.WriteLine("💡 MODIFICAR TEMPLATE:");
            Console.WriteLine("   1. Edita Templates/FacturaUbl.cs");
            Console.WriteLine("   2. Guarda (Ctrl+S)");
            Console.WriteLine("   3. Ejecuta de nuevo: dotnet run");
            Console.WriteLine("   4. Verás los cambios en el nuevo PDF");
            Console.WriteLine();
            Console.WriteLine("📁 Breakpoints útiles:");
            Console.WriteLine($"   • Línea 74: Ver XML transformado");
            Console.WriteLine($"   • Línea 91: Ver modelo parseado");
            Console.WriteLine($"   • Línea 118: Ver PDF generado");
            Console.WriteLine($"   • Templates/FacturaUbl.cs línea 66: Ver logo cargado");

            // ═══════════════════════════════════════════════════════════════════
            // PASO 6: EXPORTAR TEMPLATE PARA EL SERVICIO (AUTOMÁTICO)
            // ═══════════════════════════════════════════════════════════════════
            if (EXPORTAR_TEMPLATE)
            {
                ExportarTemplateParaServicio();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ERROR CRÍTICO: {ex.Message}");
            #if DEBUG
            Console.WriteLine($"\n📋 STACK TRACE:");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\n📋 INNER EXCEPTION: {ex.InnerException.Message}");
                Console.WriteLine(ex.InnerException.StackTrace);
            }
            #endif
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // 📤 EXPORTAR TEMPLATE Y DTO PARA EL SERVICIO
    // ═══════════════════════════════════════════════════════════════════════════════
    static void ExportarTemplateParaServicio()
    {
        try
        {
            Console.WriteLine("\n📤 [6/6] Exportando template y DTO para el servicio...");

            // ═══════════════════════════════════════════════════════════════════
            // CREAR ESTRUCTURA DE CARPETAS: ExportTemplates/_{NIT}/
            // ═══════════════════════════════════════════════════════════════════
            var exportFolder = Path.Combine(EXPORT_BASE_PATH, $"_{NIT}");

            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
                Console.WriteLine($"   📁 Carpeta creada: {exportFolder}");
            }
            else
            {
                Console.WriteLine($"   📁 Carpeta existente: {exportFolder}");
            }

            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Templates");
            basePath = Path.GetFullPath(basePath);

            // ═══════════════════════════════════════════════════════════════════
            // EXPORTAR FacturaUbl.cs
            // ═══════════════════════════════════════════════════════════════════
            var templatePath = Path.Combine(basePath, "FacturaUbl.cs");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"   ⚠️  Template no encontrado en: {templatePath}");
                return;
            }

            // Leer y transformar template
            var contenidoTemplate = File.ReadAllText(templatePath);
            var contenidoTemplateServicio = contenidoTemplate
                .Replace("using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;")
                .Replace("namespace PdfQuickDebug.Templates;", $"namespace Services.PdfGenerator.Infrastructure.Templates._{NIT};");

            // Guardar FacturaUbl en la carpeta del NIT
            var facturaExportPath = Path.Combine(exportFolder, "FacturaUbl.cs");
            File.WriteAllText(facturaExportPath, contenidoTemplateServicio);
            Console.WriteLine($"   ✅ FacturaUbl.cs exportado");

            // ═══════════════════════════════════════════════════════════════════
            // EXPORTAR NotasCredito.cs
            // ═══════════════════════════════════════════════════════════════════
            var templatePathNC = Path.Combine(basePath, "NotasCredito.cs");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"   ⚠️  Template no encontrado en: {templatePath}");
                return;
            }

            // Leer y transformar template
            var contenidoTemplateNC = File.ReadAllText(templatePathNC);
            var contenidoTemplateServicioNC = contenidoTemplateNC
                .Replace("using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;")
                .Replace("namespace PdfQuickDebug.Templates;", $"namespace Services.PdfGenerator.Infrastructure.Templates._{NIT};");

            // Guardar FacturaUbl en la carpeta del NIT
            var NotasCreditoExportPath = Path.Combine(exportFolder, "NotasCredito.cs");
            File.WriteAllText(NotasCreditoExportPath, contenidoTemplateServicioNC);
            Console.WriteLine($"   ✅ FacturaUbl.cs exportado");

            // ═══════════════════════════════════════════════════════════════════
            // EXPORTAR DtoUbl.cs
            // ═══════════════════════════════════════════════════════════════════
            var dtoPath = Path.Combine(basePath, "DtoUbl.cs");
            var dtoExportPath = Path.Combine(exportFolder, "DtoUbl.cs");

            if (File.Exists(dtoPath))
            {
                var contenidoDto = File.ReadAllText(dtoPath);
                var contenidoDtoServicio = contenidoDto
                    .Replace("using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;")
                    .Replace("namespace PdfQuickDebug.Templates;", $"namespace Services.PdfGenerator.Infrastructure.Templates._{NIT};");

                File.WriteAllText(dtoExportPath, contenidoDtoServicio);
                Console.WriteLine($"   ✅ DtoUbl.cs exportado");
            }
            else
            {
                Console.WriteLine($"   ⚠️  DtoUbl.cs no encontrado en: {dtoPath}");
            }

            // ═══════════════════════════════════════════════════════════════════
            // MOSTRAR RESUMEN
            // ═══════════════════════════════════════════════════════════════════
            Console.WriteLine();
            Console.WriteLine("   ╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("   ║  ESTRUCTURA EXPORTADA PARA EL SERVICIO:                   ║");
            Console.WriteLine("   ╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"   📂 {exportFolder}");
            Console.WriteLine($"      ├── FacturaUbl.cs  (Template con MapToCustomDto)");
            Console.WriteLine($"      ├── NotaCredito.cs  (Template con MapToCustomDto)");
            Console.WriteLine($"      └── DtoUbl.cs      (DTO personalizado)");
            Console.WriteLine();
            Console.WriteLine($"   Namespace: Services.PdfGenerator.Infrastructure.Templates._{NIT}");
            Console.WriteLine($"   Using: Services.PdfGenerator.Domain.Models");
            Console.WriteLine();
            Console.WriteLine("   🎯 PARA INTEGRAR AL SERVICIO:");
            Console.WriteLine($"   1. Copiar carpeta _{NIT} completa a:");
            Console.WriteLine($"      Services.PdfGenerator/Infrastructure/Templates/_{NIT}/");
            Console.WriteLine($"   2. Compilar: dotnet build");
            Console.WriteLine($"   3. Commit y push");
            Console.WriteLine();
            Console.WriteLine("   ✅ Listo para usar en el servicio");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Error al exportar template: {ex.Message}");
            Console.WriteLine("   El PDF se generó correctamente, solo falló la exportación.");
        }
    }

}
