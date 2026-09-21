# Índice del Proyecto — QuestPDF_QuickDebbug_v3

Doc aparte para sesiones de IA: `AGENTS.md`.

---

## 1. Visión general

Espacio de trabajo dedicado al **debugging, diseño y despliegue de templates QuestPDF** para factura electrónica colombiana (modelos UBL 2.1 → DIAN). Contiene **dos sistemas en paralelo** que comparten el mismo modelo de datos (`InvoiceModel`):

| Sistema | Ubicación | Qué hace |
|---|---|---|
| **PdfQuickDebug** (harness) | `QuestPDF_QuickDebbug\DebugPDF\PdfQuickDebug` | Ejecuta el pipeline XML→XSLT→Modelo→PDF con datos reales y **exporta el template al servicio de producción** `Services.PdfGenerator` reescribiendo namespaces automáticamente. |
| **PdfDesigner** (editor visual) | `QuestPDF_QuickDebbug\PdfDesigner` | Diseñador visual (WinForms + editor web) con drag & drop por bandas que genera **templates QuestPDF en C# compilables** desde un `design.json`. |

Más `decode/` (backups versionados) y `BK 12-09-2026/` (zip de respaldo).

---

## 2. Estructura raíz

```
QuestPDF_QuickDebbug_v3/
├── INDICE.md                  ← este documento
├── AGENTS.md                  ← guía breve orientada a agentes/IA
├── BK 12-09-2026/             ← backup zip (12-sep-2026): templates + XSLT de producción
├── decode/                    ← versionado y respaldos de templates/XSLT/DTOs
└── QuestPDF_QuickDebbug/      ← proyecto activo
    ├── DataTest/              ← datos de prueba, XSLT activo, guías, salidas
    ├── DebugPDF/              ← harness PdfQuickDebug (documentación + código)
    ├── output/                ← artefactos generados (GeneratedArchive)
    └── PdfDesigner/           ← diseñador visual (sln Engine+WinForms+Cli)
```

---

## 3. Sistema 1 — PdfQuickDebug (harness de debugging)

**Entrada:** `DataTest\NUEVO_XML.xml` (UBL) + `DataTest\FACTURA-UBL.xslt`
**Config:** constantes al inicio de `DebugPDF\PdfQuickDebug\Program.cs` (líneas 16-31): `XML_PATH`, `XSLT_PATH`, `OUTPUT_PDF`, `NIT`, `EXPORTAR_TEMPLATE`, `SERVICE_TEMPLATE_PATH`, `ENABLE_COMPANION`.

### Pipeline de 6 pasos
1. Cargar XML y XSLT.
2. `XsltTransform.Transform` → XML simple (`<CFD>` + `<Adicional>` con `<Campo clave="..." valor="..."/>`).
3. `ValidateTransformedXml` → chequea `<CFD>` y `<Adicional>`.
4. `XmlParser.Parse` → `InvoiceModel` genérico.
5. `template.MapToCustomDto(model)` → `DtoUbl` tipado → `GeneratePdf` (QuestPDF) → `OUTPUT_PDF`.
6. `ExportarTemplateParaServicio()` → reescribe namespaces y exporta a `DataTest\_{NIT}\`.

### Contrato de exportación (namespace rewrite)
- `using PdfQuickDebug.Core;` → `using Services.PdfGenerator.Domain.Models;`
- `namespace PdfQuickDebug.Templates;` → `namespace Services.PdfGenerator.Infrastructure.Templates._{NIT};`

### Reglas de modificación
- **El Core NO se toca NUNCA**: `Core\Models.cs`, `Core\XmlParser.cs`, `Core\XsltTransform.cs` y `Utilities\PDF_Utilities.cs` son de solo lectura.
- **Únicos archivos editables:** el **DTO** (`Templates\DtoUbl.cs`), el **XSLT** (p. ej. `DataTest\FACTURA-UBL.xslt`) y el **template `Templates\FacturaUbl.cs`**. Los demás templates (`NotasCredito.cs`, `FacturaUBL1/2.cs`) solo si se trabaja específicamente en ellos.

### Estructura de archivos
- `Program.cs` (323) — main y exportación automática.
- `Core\Models.cs` (143) — `InvoiceModel`, `IInvoiceTemplate`, `TemplateMetadata`. `InvoiceModel` agrega `Folio`, `Document`, `Issuer`, `Customer`, `Lines`, `Totals`, `QR`, `Additional` (con `CustomFields`), `InformacionAdditional`.
- `Core\XmlParser.cs` (209) — parseo del XML simple a `InvoiceModel` (mapeo por atributos). OJO: hay un `Console.WriteLine` de debug en la línea 106.
- `Core\XsltTransform.cs` (86) — transformación XSLT (`ConformanceLevel.Fragment`).
- `Utilities\PDF_Utilities.cs` (182) — `PDF_Utils.NumeroALetrasCOP` (numeral a letras COP), `SkiaSharpHelpers`, `DrawBarCodeExtensions`, `StringsProcessing.GenerateEan13`.
- `Templates\DtoUbl.cs` (79) — `DtoUbl : InvoiceModel` (Zona, Ruta, PlacaVehiculo, NumCargue, OrdenCompra, NumPedido, FechaDian, HoraGeneracion, ResolucionTexto, LogoBase64, ValorLetras, CondicionPago; diccionarios `Factores`, `Motivos`, `CodigosBarra` por `LineItemInfo`).
- `Templates\FacturaUbl.cs` (1.783) — **template principal activo**. Formato 612×391pt (~media página), logo, QR, 12 ítems/página, tabla de 13 columnas, totales, texto legal, `AgruparPorSku`, factores de empaque, motivos, códigos de barra. `BrandColors` y `InvoiceTableStyles` al final.
- `Templates\NotasCredito.cs` (814) — notacrédito (612×399pt, muestra CUDE+CUFE).
- `Templates\FacturaUBL1.cs` (327) — esqueleto auto-generado por diseñador (no usado en el flujo activo).
- `Templates\FacturaUBL2.cs` (643) — réplica del diseño FRD2 HTML para MIZOOCO S.A.S. (con constantes de muestra pendientes de dato real).
- `Templates\236015FRD2.html` (380) — fixture de referencia (export pdf2htmlEX) para FacturaUBL2.
- `FacturaUbl_PARA_SERVICIO.cs` (raíz DebugPDF, 510) — artefacto exportado antiguo (NIT 1193122070), referencia del namespace rewrite.
- `INTEGRAR_TEMPLATE.ps1` (117) — automatización previa (PowerShell), superada por el paso 6 de Program.cs.

### Dependencias NuGet (PdfQuickDebug.csproj)
QuestPDF 2024.12.3 · QRCoder 1.4.3 · SkiaSharp 3.119.1 · System.Text.Encoding.CodePages 8.0.0 · ZXing.Net 0.16.11 · ZXing.Net.Bindings.SkiaSharp 0.16.22.

### Documentación de ayuda
`DebugPDF\README.md` (principal), `LEEME_PRIMERO.txt`, `INICIO_RAPIDO.txt`, `RESUMEN_FINAL.txt`, `PARA_SOPORTE_INTEGRAR.md`, y dentro de `PdfQuickDebug\`: `COMO_DEBUGEAR.md`, `COMO_USAR_EXPORTACION.md`, `SOLUCION_DEFINITIVA.md`, `DEBUGGING_SIN_EXTENSION.md`, `INSTALAR_EXTENSION.md`, `SOLUCION_ERROR_VSCODE.txt`. Incluyen rutas viejas de otras máquinas (`F:\Pruebas...`, `E:\DV-JDL...`) — ignorar, ya no aplican.

---

## 4. Sistema 2 — PdfDesigner (editor visual de reportes)

Solución `PdfDesigner.sln` con 3 proyectos. Namespace del motor: `PdfQuickDebug.Designer.*`.

### 4.1 PdfDesigner.Engine (motor)
Referencia a QuestPDF y a `..\..\DebugPDF\PdfQuickDebug\PdfQuickDebug.csproj` (reutiliza `XmlParser`, `InvoiceModel`, `XsltTransform`, `DtoUbl`). **No modificar** DebugPDF: el diseñador solo lo lee.

```
PdfDesigner.Engine/
├── DesignerRunner.cs        — acceso rápido: XML + design.json → PDF bytes
├── DesignerTemplate.cs      — adaptador de ReportDesign a IInvoiceTemplate
├── Model/                   — esquema JSON del diseño
│   ├── ReportDesign.cs      — raíz: page, metadata, namespaces, sections, expressions
│   ├── ElementDesign.cs     — 11 tipos de elemento (text, field, richtext, image, line,
│   │                          shape, checkbox, pagebreak, table, barcode, subreport)
│   ├── ExpressionDesign.cs  — expresiones declarativas (concat/add/sub/mul/div/if/upper/lower)
│   ├── DesignerJson.cs      — serialización JSON (camelCase, tolerante)
│   └── PageSizeCatalog.cs   — tamaños de página (Letter/A4/... + orientación + custom)
├── Data/
│   ├── XmlDataResolver.cs   — resuelve XPath contra XML (fallback local-name(), 3 niveles)
│   └── XmlFieldCatalog.cs   — descompone XML en árbol de campos para el picker UI
├── Rendering/               — render en runtime (preview directo a PDF)
│   ├── RenderingAbstractions.cs — RenderContext, IElementRenderer, ElementRendererRegistry
│   ├── ElementRenderers.cs  — 11 renderers (QuestPDF + SkiaSharp + ZXing)
│   ├── DesignDocumentRenderer.cs — compositor IDocument (bandas/sections → páginas)
│   └── RenderingHelpers.cs  — StyleApplicator, Formatting (es-CO), ExpressionEvaluator, ImageSource
├── CodeGen/                 — generación de C# compilable
│   ├── DesignToCSharpGenerator.cs — genera clase IInvoiceTemplate autocontenida desde design.json
│   ├── DtoToCSharpGenerator.cs    — genera/fusiona DtoUbl.cs (merge seguro)
│   ├── DataContract.cs      — catálogo de campos (BindingMap + CustomFields reales del XSLT + DTO)
│   ├── BindingMap.cs        — traduce XPath ↔ expresiones C# (model.*/line.*)
│   ├── ModelSchemaCatalog.cs— "Data Source": campos del InvoiceModel parseado (reflexión)
│   ├── GenerationValidator.cs — reglas de contenido + smoke compile (dotnet build)
│   ├── TemplateExporter.cs  — exporta .cs a local / servicio / cliente
│   └── XsltFieldInjector.cs — inyecta <Campo> en el XSLT entre marcadores DESIGNER-FIELDS
└── UI/                      — editor web drag & drop
    ├── DesignerUiServer.cs  — HttpListener + REST (GET/POST /api/load, /render, /save)
    ├── designer.html/.css/.js — SPA de 4 paneles; render vía /api/render
```

**Flujo de render (runtime):** `design.json → ReportDesign` + XML → `XmlDataResolver` → `DesignDocumentRenderer` (QuestPDF) → PDF.
**Flujo de generación:** `design.json + binding-map.json → DesignToCSharpGenerator → FacturaUbl.cs` (+ `DtoToCSharpGenerator → DtoUbl.cs`) → `TemplateExporter` → carpeta local/service/cliente.
**Pipeline completo de exportación:** descubrir XML fields → `DataContract.Build` → inyectar al XSLT (si aplica) → generar C# → validar (`CheckContent` + smoke compile) → empaquetar ZIP `{Cliente}_{NIT}.zip`.

### 4.2 PdfDesigner.WinForms (aplicación visual)
- `Program.cs` (50) — entry point, excepción → `designer-error.log`, migración, abre `StartForm`.
- `StartForm.cs` (236) — selector de cliente + Nuevo/Cargar diseño.
- `DesignerForm.cs` (2.367) — **corazón de la app**: toolbar, paneles, propiedades, secciones, undo/redo (Ctrl+Z/Y), copy/paste, alinear/distribuir, render/preview, exportar zip/.cs/.json, "Todo en 1", onboarding de cliente (transforma XML, descubre contrato, genera DTO).
- `ReportDesignSurface.cs` (1.510) — canvas WYSIWYG: pintado por bandas, reglas, grid, hit-testing, drag-drop (toolbox + campos), resize de sección, `ValidateLayout()`.
- `PageSetupDialog.cs` (214) — tamaño/orientación/márgenes de página.
- `DataSourceExplorerPanel.cs` (274) — árbol jerárquico "Data Source" (estilo Crystal Reports).
- `FieldExplorerPanel.cs` (355) — lista plana de campos con estado (OK/NoData/NoMapping).
- `ClientPickerForm.cs` (142) — cambiar/crear cliente.
- `ClientData.cs` (298) — gestión de carpetas por cliente + migración + `TryExtractNit`.
- `DesignElementFactory.cs` (102) — paleta y creación de elementos por defecto.
- `ElementDragData.cs`, `FieldDragData.cs`, `DragPreview.cs` — infraestructura de drag & drop.

### 4.3 PdfDesigner.Cli (consola de utilidades)
Path root hardcodeado: `...\PdfDesigner`. Comandos:
- `--save-transformed`· — guarda el XML transformado por el XSLT.
- `--resolve-test` — prueba cuántos campos del contrato resuelven contra la muestra.
- `--contract` — (re)genera `data-contract.json` desde `binding-map.json` + pipeline.
- `--design` — renderiza `factura.design.json` → `output\DESIGN_OUTPUT.pdf`.
- `--designer-ui | --ui` — editor web en `http://localhost:12500`.
- `--validate` — regenera DTOs y smoke-compila los templates generados (`ValidateGeneration.cs`).
- `--cs-local <json> [--binding <json>]` — genera variante local (`PdfQuickDebug.Templates`) en `output\LocalDebug\`.

### 4.4 Modelo de datos por cliente
- `data\clients\<Cliente>\` — **entregables**: `ubl.xslt`, `dto\DtoUbl.cs`, `templates\*.cs`.
- `data\work\<Cliente>\` — **estado de trabajo**: `raw.xml`, `transformed.xml`, `binding-map.json`, `data-contract.json`, `client.json`, `*.design.json`.

| Cliente | NIT | Estado |
|---|---|---|
| AJECOLOMBIA | 800153993 | Maduro (diseños + contrato; el más completo) |
| REAL | 900111222 | Template auto-generado |
| EXPORTACIONES SAS | 900111222 | Template auto-generado (FacturaUbl + propio) |
| CORRAL | 900123456 | Template auto-generado |
| COLANTA | — | Esqueleto (solo XSLT) |
| GRUPO BIOS | — | Esqueleto (solo XSLT + diseño preliminar) |
| (legacy en `data/work/REAL`) | 900111222 | Diseño de prueba REAL |

### 4.5 Routera de datos ejemplo (AJECOLOMBIA)
- `data\work\AJECOLOMBIA\factura.design.json` — diseño base (Letter, 30pt márgenes).
- `data\work\AJECOLOMBIA\binding-map.json` — mapeos `//CFD/Documento/@numero_cd` → `model.Document.Number`, etc.
- `data\clients\AJECOLOMBIA\dto\DtoUbl.cs` — DTO con Factores/Motivos/CodigosBarra.

---

## 5. Datos de prueba — `DataTest/`

- **XML UBL de prueba (15+):** `NUEVO_XML.xml` (principal, CBPA, 7 líneas, NIT 830081407), `10.xml`, `101F0111510.xml`, `102f024795899*.xml`, `CBPA1624947.xml`, `CFUA88896.xml`, `CMEA543903.xml`, `SETP993902812/13.xml`, `xml_aje.xml`, `xml_dian_hoy.xml`, `XML-HOY-JENNY.xml`, `xml-masivo.xml`, `factura-aje-masivo.xml`, `direccion_xml.xml`.
- **XSLT activo:** `FACTURA-UBL.xslt` (1.027 líneas).
- **Guías:** `GUIA_DISENADOR.md` (240 líneas, POC del diseñador visual) y `GUIA_TECNICA_CAPACITACION.md` (772 líneas, guía técnica completa del pipeline) + versión PDF.
- **Salidas:** `DEBUG_OUTPUT.pdf` + `DEBUG_OUTPUT_preview.png`.
- **Exportación para servicio:** `_800153993\` (DtoUbl.cs, FacturaUbl.cs, NotasCredito.cs reescritos a `Services.PdfGenerator.Infrastructure.Templates._800153993`).

---

## 6. `output/` (artefactos generados)

- `GeneratedArchive\Test.cs.bak` — template generado temprano (con marcadores TODO de XPath sin mapear).
- `ValidationScratch\` — sandbox de compilación (REAL.cs + csproj) usado por el validador.
- `LocalDebug\` — variantes locales `PdfQuickDebug.Templates` (DtoUbl.cs, FacturaUbl.cs, FACTURA-UBL.xslt).
- `EXPORTACIONES SAS_900111222.zip` + carpeta extraída (dto/templates/ubl.xslt de producción).

---

## 7. `decode/` — versionado y backups

Raíz: `templates.json` (descriptor JSON con ZIP embebido en base64: DtoUbl, DummyUbl, FacturaUbl, NotasCredito para NIT 830081407) y `extraer-zip.js` (utilidad Node que lo extrae).

- `templates_830081407/` — zip extraído (4 .cs + FACTURA-UBL.xslt).
- `VERSION 2 … VERSION 6\` — evolución de `DtoUbl.cs`, `FacturaUbl.cs`, `FACTURA-UBL.xslt` (+ `FACTURA-DESC.txt` en 3-6, detalle de factura para debug). Clave: V3 añade `FechaDian`, `Motivos`, `CodigosBarra` y cambia `valorLetras` a `Document.Total_am`; V4-V6 son refinamientos.
- `test\` — copia de trabajo intermedia.
- Backups por fecha: `BK TEMPLATE\`, `BK XSLT\`, `PROD BACKUP AJE 9-9-2026\`, `QA BACKUP AJE 9-9-2026\`, `BACKUP AJE V4\`, `VERSION 5\BK ANTES DE 4\`.
  - Crecimiento de FacturaUbl.cs: 45.5KB (7-sep) → 55.4KB (9-sep) → 62.7KB (10-sep) → 63.0KB (12-sep).

---

## 8. Glosario

- **UBL 2.1** — estándar DIAN de factura electrónica (XML).
- **XSLT** — transforma UBL → XML simple (`<CFD>` + `<Adicional><Campo clave="..."/>`).
- **XML simple** — formato intermedio plano que consume `XmlParser`.
- **`InvoiceModel`** — modelo genérico parseado (¿por todos los templates).
- **Bloqueo de edición** — el Core de DebugPDF (`Core\*`, `Utilities\PDF_Utilities.cs`) es intocable; solo se editan DTO, XSLT y `FacturaUbl.cs`.
- **`DtoUbl`** — DTO por cliente que extiende `InvoiceModel` con props tipadas extraídas de `CustomFields`.
- **`CustomFields`** — diccionario `clave → valor` del `<Adicional>`, fuente de campos opcionales.
- **NIT** — identificación fiscal del cliente emisor; selecciona el namespace/la carpeta de exportación (830081407, 800153993, 900111222, 900123456, 1193122070 aparecen según contexto).
- **`design.json`** — diseño del reporte (bandas + elementos + binding) en JSON.
- **`binding-map.json`** — mapeo XPath ↔ expresión C# (`model.*` / `line.*` / `GetCustomField(...)`).
- **`data-contract.json`** — catálogo completo de campos disponibles para binding.
- **Banda/`SectionDesign`** — región horizontal (reportHeader, pageHeader, groupHeader, detail, groupFooter, pageFooter, reportFooter) con elementos posicionados absoluta.
- **QuestPDF Companion** — hot reload visual en navegador (puerto 12500, deshabilitado por defecto en el flujo principal).