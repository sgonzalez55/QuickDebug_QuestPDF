# Diseñador visual → QuestPDF (POC)

Esqueleto para un editor drag-and-drop (estilo Crystal Reports) que produce un
JSON de diseño y lo traduce a PDF con QuestPDF. Integrado al proyecto existente
`PdfQuickDebug` **sin tocar `Core/*`**.

## Flujo

```
XML (fuente de datos) ──┐
                        ├──► XmlDataResolver (XPath + namespaces)
design.json ────────────┘            │
                                     ▼
                     DesignDocumentRenderer (IDocument QuestPDF)
                                     │
                                     ▼
                                  PDF final
```

## Editor visual nativo (WinForms, drag & drop)

Proyecto **`DebugPDF/PdfDesigner.WinForms`** (WinForms, `net8.0-windows`),
referencia al motor `PdfQuickDebug`. Es la UI principal del diseñador.

Ejecutar:

```
dotnet run --project DebugPDF/PdfDesigner.WinForms
```

Ventana (modelo ActiveReports):
- **Toolbox (izquierda):** Texto, Campo (XPath), Texto enriquecido, Imagen,
  Línea, Forma, Casilla, Tabla, Código de barras/QR, Salto de página. Se
  **arrastran** al lienzo y aparecen al instante.
- **Secciones (izquierda-abajo):** agregar/quitar/reordenar secciones y elegir
  su tipo (`reportHeader`, `pageHeader`, `groupHeader`, `detail`, `groupFooter`,
  `pageFooter`, `reportFooter`, `custom`). Cada sección tiene **alto fijo**,
  ajustable también **arrastrando el borde inferior** (amplía hacia abajo o
  reduce hacia arriba) o el **borde superior** (mueve el límite con la sección
  anterior). Alto mín. 8 pt, máx. el área de contenido de la página.
- **Lienzo (centro, WYSIWYG GDI+):** muestra las secciones con su alto real y
  los controles dibujados (texto, líneas, formas, tablas, códigos…). Incluye
  reglas, grilla, selección con handles, **arrastrar para mover** y
  **redimensionar**; flechas para mover 1pt (Shift = grilla); Supr elimina.
- **Propiedades (derecha):** X/Y/Ancho/Alto/Z-Orden/Visible, contenido, XPath,
  formato, fuente, colores, bordes, alineación, rotación, etc.
- **Árbol XML (abajo derecha):** clic en un nodo genera su XPath y lo asigna al
  Campo/Casilla/Tabla seleccionado.
- **Toolbar:** `Render PDF`, `👁 Preview` (rasteriza el PDF embebido con
  `GenerateImages`), `Deshacer/Rehacer` (Ctrl+Z/Y), `Copiar/Pegar/Duplicar`
  (Ctrl+C/V/D), `Guardar`, `Exportar/Importar JSON`, subir/bajar y eliminar
  sección, zoom y grilla.
- **Menú contextual** en el lienzo: traer al frente, enviar atrás, duplicar,
  eliminar.

Características extra: `CanGrow` de secciones (crece con el contenido),
`PageBreak` real (fuerza salto de página), rotación, RichText con spans,
códigos de barras/QR reales y subreportes.

Archivos: `PdfDesigner.WinForms/Program.cs`, `DesignerForm.cs`,
`ReportDesignSurface.cs`, `DesignElementFactory.cs`, `ElementDragData.cs`.

> Nota: existe también un editor web opcional (`dotnet run -- --designer-ui`).
> No es necesario; el nativo es el recomendado.

### Panel Datos (arrastrar campos del XML)

El panel izquierdo tiene pestañas **Elementos | Datos | Secciones**. En **Datos**
se desglosa el XML cargado en un árbol jerárquico de campos (hojas y atributos,
con su valor de muestra); los nodos repetidos se marcan `[repetido]` y el ruido
técnico (firma, blobs base64) se excluye.

- **Arrastra un campo** al lienzo → crea un `Campo` enlazado (XPath + Expr) en la
  posición del drop. Si el campo está en el contrato, toma su `Expr`; si es
  `CustomField`, usa `GetCustomField(...)`.
- **Clic** en un campo → lo asigna al elemento seleccionado (Campo/Casilla/Código/Tabla).
- **Arrastra un repetido** (ej. `Lines/Line`) → menú **Tabla** | **Banda Detail** |
  **Campo simple**; la Tabla/Detail se generan con las columnas/campos hijos.

## Exportar a template QuestPDF (.cs)

El diseño visual se traduce a **código C# QuestPDF real** (`: IInvoiceTemplate`),
con la **misma estructura de `FacturaUbl.cs`** y **puro código de la librería
QuestPDF** (llamadas directas, sin capas de abstracción propias):

- `public class <Nombre> : IInvoiceTemplate`
- ctor estático + `RegisterEncodingProvider`
- `GeneratePdf(model)` / `PreviewOnQuestPDFCompanion(model)` → `Compose(container, model)`
- `ComposeHeader` / `ComposeContent` / `ComposeFooter`
- `GetMetadata()` (block) y `MapToCustomDto`
- Helpers autocontenidos: `SafeGetValue`, `GetCustomField`, `Format`,
  `SafeLoadImage`, `DrawShape`, `DrawBarcode`, `Canvas`

Botones:
- **`🧩 Exportar .cs`** → `Templates/Generated/<Nombre>.cs` (namespaces locales).
- **`🚀 Al servicio`** → `DataTest/_<NIT>/` con los namespaces de
  `Services.PdfGenerator` (reescritura `PdfQuickDebug.Core →
  Services.PdfGenerator.Domain.Models`).

El render usa el **layout absoluto** con `Layers` + `TranslateX/Y` (API de
QuestPDF) y enlaza los campos contra **`InvoiceModel`** (no requiere el XML en
runtime). Ejemplo generado:

```csharp
layers.Layer()
    .TranslateX(0f, Unit.Point).TranslateY(26f, Unit.Point)
    .Width(220f).Height(14f)
    .Element(cell =>
    {
        cell.Text(Format(model.Issuer.TaxId, "text")).FontSize(9f).FontColor("#000000");
    });
```

**Mapeo de datos:** `DataTest/DesignTest/binding-map.json` traduce cada XPath a
una propiedad del modelo (editable sin recompilar). Defaults para UBL común;
lo no mapeado se emite como literal + `// TODO`.

Archivos: `Designer/CodeGen/DesignToCSharpGenerator.cs`, `BindingMap.cs`,
`TemplateExporter.cs`. El bloque `metadata` del diseño (nit, templateName,
clientName, version) alimenta `GetMetadata()`.

## Campos, XSLT y DTO

El diseñador conoce los campos disponibles vía `data-contract.json`, generado con
`dotnet run -- --contract` o el botón **🔎 Campos**. El contrato combina:
- **Modelo**: entradas de `binding-map.json` (props de `InvoiceModel`).
- **Custom**: claves reales de `Additional.CustomFields` que produce el XSLT.
- **DTO**: props de `DtoUbl`.

### Enlazar un campo
1. Selecciona un `Campo` en el lienzo.
2. En **Campo del contrato** elige el campo (o escribe el XPath a mano).
3. Al elegir, se setean `XPath` (preview) y `Expr` (expresión C# para el código
   generado). `Expr` tiene prioridad sobre el `binding-map`.

### Campos nuevos
- **Automático:** si el XML trae `<CustomField Name="f_x" Value="..."/>`, el XSLT
  ya lo emite (passthrough) → aparece en el contrato. Cero edición.
- **Asistido:** si es un nodo UBL no mapeado, selecciona el `Campo` con su XPath
  y pulsa **➕ XSLT**. Se inserta `<Campo clave="f_designer_X" valor="{...}"/>`
  en el bloque `<!-- DESIGNER-FIELDS:START/END -->` de `<Adicional>`
  (idempotente) y se re-descubre el contrato.

> El DTO es opcional: el template generado enlaza a `InvoiceModel`
> (`GetCustomField`), así que no hace falta tocar `DtoUbl` salvo para acceso
> tipado en `FacturaUbl`.

### Preview y XPaths
- El resolver es **namespace-agnóstico**: `//Customer/Name` matchea
  `inv:Customer/cbc:Name` (convierte nombres con y sin prefijo a `local-name()`),
  y un XPath sin match devuelve vacío (nunca el tipo del iterator).
- Los campos **Custom** se previsualizan cargando un XML de muestra con
  `<CustomFieldExtension>` usando el botón **📂 Muestra XML** (ej.
  `NUEVO_XML.xml`). Con el sample demo no aparecen, pero sí funcionan en el
  `.cs` generado.

## Modo sin UI

`dotnet run -- --design` genera el PDF desde `factura.design.json` +
`factura.sample.xml` sin abrir ninguna interfaz.

## Componentes

| Ruta | Responsabilidad |
|---|---|
| `Designer/Model/ReportDesign.cs` | Raíz, página, secciones, columnas, repetición |
| `Designer/Model/ElementDesign.cs` | Elementos polimórficos por `"type"` + estilos/layout |
| `Designer/Model/ExpressionDesign.cs` | Operandos y expresiones calculadas |
| `Designer/Model/DesignerJson.cs` | Opciones de System.Text.Json |
| `Designer/Data/XmlDataResolver.cs` | Resolución XPath, namespaces, tolerante |
| `Designer/Rendering/DesignDocumentRenderer.cs` | `IDocument`: secciones absolutas (Layers + TranslateX/Y) |
| `Designer/Rendering/ElementRenderers.cs` | Renderers text/field/richtext/image/line/shape/checkbox/pagebreak/table/barcode |
| `Designer/Rendering/RenderingAbstractions.cs` | `IElementRenderer` + registry extensible |
| `Designer/Rendering/RenderingHelpers.cs` | Estilos, formatos, expresiones |
| `Designer/DesignerTemplate.cs` | Adapter `: IInvoiceTemplate` |
| `Designer/CodeGen/DesignToCSharpGenerator.cs` | Emisor opcional de código C# (Fase 2) |

## Tipos de elemento soportados

`text`, `field`, `richtext`, `image`, `line`, `shape` (rect/elipse/redondeado),
`checkbox`, `pagebreak`, `table`, `barcode` (CODE_128, CODE_39, EAN_13, QR) y
`subreport` (incrusta otro `.design.json`).
Agregar uno nuevo = nueva subclase de `ElementDesign` + `IElementRenderer` +
registrarlo en `ElementRendererRegistry.CreateDefault()`.
No hay que tocar el recorrido del documento.

## Formatos y expresiones

- `format`: `text`, `currency`, `currency2`, `number`, `number2`, `upper`,
  `lower`, `date:<patrón>` (ej: `date:dd/MM/yyyy`).
- `expressions[].op`: `concat`, `add`, `sub`, `mul`, `div`, `if`, `upper`, `lower`.

---

## Notas de arquitectura

### 1. Limitaciones del enfoque
- **Layout por flujo, no absoluto**: QuestPDF no posiciona por (x, y). Replicar
  diseños de layout fijo (como `236015FRD2.html`) requerirá capa `Canvas`/`Layers`
  o aceptar aproximaciones.
- **Paginación avanzada**: `keep-together`, subreportes, saltos condicionales y
  numeración por grupo probablemente requieran tocar el JSON o el generador.
- **Autofit de texto**, rotación y texto enriquecido mixto: limitado.
- **Condicionales**: hoy `if` a nivel de expresión; visibilidad condicional de
  elementos requerirá un `visibleWhen` en el modelo.

### 2. Namespaces distintos entre documentos
El diseño declara `namespaces` por reporte. Si un XPath con prefijos no
registrados falla, `XmlDataResolver` reintenta automáticamente por
`local-name()`, así que un mismo diseño puede apuntar a XML plano o tipo UBL
sin reescribir el resolver. Para migrar de plano → UBL basta agregar los
prefijos `cac/cbc` al `namespaces` y ajustar los XPath del diseño.

### 3. Versionado y pruebas
- Campo `version` en `ReportDesign`.
- Validación contra JSON Schema antes de renderizar (recomendado en Fase 2).
- Tests unitarios de `XmlDataResolver` y `ExpressionEvaluator` (casos: nodo
  faltante, namespace no registrado, conversión de decimal).
- Pruebas "golden": extraer el texto del PDF (pymupdf) y compararlo contra un
  snapshot esperado. Cubrir: saltos de página, secciones vacías, 0 y 1 filas,
  valores negativos y subreportes.

### 4. Requerimientos del frontend (drag-and-drop)
- Paleta de elementos y panel de propiedades que emitan exactamente el JSON del
  modelo (`type`, `binding`, `style`, `layout`).
- Panel de árbol XML: cargar un XML de muestra y, al hacer clic en un nodo,
  generar su XPath (con selector de namespace). Es la pieza clave para que el
  usuario no escriba XPath a mano.
- Canvas de diseño con drag-drop; guardar/abrir `.design.json`.
- Preview en vivo vía QuestPDF Companion o render del servidor.
- Validación: impedir soltar elementos en secciones incompatibles (ej: detalle
  solo dentro de `kind: detail`).

### 5. Refactor del proyecto actual
- **Ninguno obligatorio**: `Core/*` queda intacto y los templates existentes
  (`FacturaUbl`, `NotasCredito`, `FacturaUBL2`) siguen funcionando.
- Se **agrega** la carpeta `Designer/` y `Program.cs` gana un `if (args.Contains("--design"))`.
- Si se quiere publicar al servicio `Services.PdfGenerator`, el pipeline de
  export puede incluir `DesignerTemplate.cs` + `Designer/` con el mismo reemplazo
  de namespace (`Services.PdfGenerator.Infrastructure.Templates._{NIT}`).
