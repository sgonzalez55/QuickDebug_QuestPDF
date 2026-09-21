# AGENTS.md — QuestPDF_QuickDebbug_v3

Guía breve para agentes/IA. Índice completo y detallado: ver `INDICE.md`.

## Propósito
Espacio de trabajo para diseñar, debugear y desplegar templates **QuestPDF** de factura electrónica colombiana (UBL 2.1). Tiene dos sistemas que comparten el modelo `InvoiceModel`:

- **DebugPDF/PdfQuickDebug** — harness: XML→XSLT→`InvoiceModel`→`DtoUbl`→PDF, y exporta el template al servicio `Services.PdfGenerator` reescribiendo namespaces.
- **PdfDesigner** — editor visual (WinForms + web) que genera templates C# compilables desde `design.json`.

Más `decode/` (backups versionados) y `BK 12-09-2026/` (backup zip). Carpeta de trabajo: `QuestPDF_QuickDebbug\PdfDesigner`.

## Estructura de alto nivel
```
QuestPDF_QuickDebbug/          ← proyecto activo
├── DataTest/                  ← XML UBL de prueba, FACTURA-UBL.xslt (activo), guías en .md/.pdf
├── DebugPDF/PdfQuickDebug/    ← harness (Program.cs: config líneas 16-31, Templates/, Core/)
├── output/                    ← artefactos generados (GeneratedArchive)
└── PdfDesigner/               ← sln: PdfDesigner.Engine + .WinForms + .Cli
    ├── data/clients/<Cliente>/  ← entregables: ubl.xslt, dto/DtoUbl.cs, templates/*.cs
    ├── data/work/<Cliente>/     ← estado: binding-map.json, data-contract.json, *.design.json, raw/transformed.xml, client.json
    └── output/                  ← resultados (LocalDebug/, ValidationScratch/, ZIPs)
```

## Comandos útiles
```bash
# Debug de template (configura rutas en Program.cs líneas 16-31)
dotnet run --project "QuestPDF_QuickDebbug/DebugPDF/PdfQuickDebug/PdfQuickDebug.csproj"

# Herramientas del diseñador (rutas hardcodeadas a ...\PdfDesigner)
dotnet run --project "QuestPDF_QuickDebbug/PdfDesigner/PdfDesigner.Cli/PdfDesigner.Cli.csproj" -- --design
dotnet run --project "QuestPDF_QuickDebbug/PdfDesigner/PdfDesigner.Cli/PdfDesigner.Cli.csproj" -- --validate
dotnet run --project "QuestPDF_QuickDebbug/PdfDesigner/PdfDesigner.Cli/PdfDesigner.Cli.csproj" -- --designer-ui  # editor web :12500

# App WinForms
dotnet run --project "QuestPDF_QuickDebbug/PdfDesigner/PdfDesigner.WinForms/PdfDesigner.WinForms.csproj"
```

## Reglas y convenciones clave
- **`PdfDesigner.Engine` referencia `DebugPDF\PdfQuickDebug\PdfQuickDebug.csproj`** (reusa `XmlParser`, `InvoiceModel`, `XsltTransform`, `DtoUbl`). **No modificar** DebugPDF por cambios del diseñador; el diseñador solo lo lee.
- **El Core de DebugPDF/PdfQuickDebug NUNCA se toca**: `Core\Models.cs`, `Core\XmlParser.cs`, `Core\XsltTransform.cs` y `Utilities\PDF_Utilities.cs` son de solo lectura. Los únicos archivos editables son el **DTO** (`Templates\DtoUbl.cs`), el **XSLT** (p. ej. `DataTest\FACTURA-UBL.xslt`) y el **template `Templates\FacturaUbl.cs`**.
- El NIT del cliente decide namespace y carpeta de export: `Services.PdfGenerator.Infrastructure.Templates._{NIT}` con `using Services.PdfGenerator.Domain.Models;`.
- Catálogo de campos = `DataContract` (BindingMap + CustomFields reales del XSLT + propiedades del DTO). Binding: `BindingMap.Resolve()` traduce XPath→C# (`model.*`/`line.*`/`GetCustomField`).
- NITs en juego según contexto: 830081407, 800153993, 900111222, 900123456, 1193122070. No asumir uno solo.
- Los templates generados NO deben usar SkiaSharp/ZXing directamente en entrega (`GenerationValidator` lo prohíbe).
- `XmlDataResolver` resuelve XPath con fallback `local-name()` (3 niveles) → robusto ante namespaces.
- `XsltFieldInjector` inyecta `<Campo clave="..."/>` en el XSLT entre marcadores `<!-- DESIGNER-FIELDS:START/END -->` (idempotente).
- CSV/formatos es-CO; numeral a letras vía `PDF_Utils.NumeroALetrasCOP`.
- Fuente registrada: `Assets/Fonts/LiberationSans-Regular.ttf` (obligatoria para PDF).

## Datos por defecto
- XML de prueba principal: `DataTest\NUEVO_XML.xml` · XSLT activo: `DataTest\FACTURA-UBL.xslt`.
- Clientes con template generado: REAL, EXPORTACIONES SAS, CORRAL. Maduro a mano: AJECOLOMBIA. Esqueleto: COLANTA, GRUPO BIOS.

## Documentación
- `INDICE.md` (esta raíz) — índice completo del proyecto.
- `QuestPDF_QuickDebbug\DataTest\GUIA_TECNICA_CAPACITACION.md` — guía técnica del pipeline.
- `QuestPDF_QuickDebbug\DataTest\GUIA_DISENADOR.md` — POC del diseñador visual.
- `DebugPDF\README.md` + docs en `DebugPDF\PdfQuickDebug\*.md` — uso del harness. (Algunas rutas citadas son de otras máquinas: **no aplican**.)