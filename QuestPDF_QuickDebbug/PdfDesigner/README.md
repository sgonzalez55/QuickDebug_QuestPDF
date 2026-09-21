# PdfDesigner

Diseñador visual de reportes (WinForms) que arrastra campos de la factura (modelo + campos Custom) y genera templates QuestPDF. Aislado del proyecto original en su propia carpeta.

## Estructura

```
PdfDesigner\
├── PdfDesigner.sln              ─ solución (Engine + WinForms + Cli)
├── PdfDesigner.Engine\          ─ motor (antes DebugPDF\PdfQuickDebug\Designer\)
├── PdfDesigner.WinForms\        ─ aplicación visual (drag & drop)
├── PdfDesigner.Cli\             ─ consola de utilidades
├── data\                        ─ muestras y contratos: factura.sample.xml,
│                                  factura.transformed.xml (salida real del XSLT),
│                                  binding-map.json, data-contract.json,
│                                  factura.design.json, NUEVO_XML.xml, FACTURA-UBL.xslt
└── output\                      ─ resultados: DESIGN_OUTPUT.pdf, Generated\,
                                   _800153993\ (exportación para el servicio)
```

## Cómo usar

1. **Abrir el diseñador visual**
   ```
   dotnet run --project PdfDesigner.WinForms\PdfDesigner.WinForms.csproj
   ```
   (o ejecutar `PdfDesigner.WinForms\bin\Debug\net8.0-windows\PdfDesigner.WinForms.exe`)

2. **Utilidades de consola**
   ```
   dotnet run --project PdfDesigner.Cli\PdfDesigner.Cli.csproj -- --save-transformed
   dotnet run --project PdfDesigner.Cli\PdfDesigner.Cli.csproj -- --contract
   dotnet run --project PdfDesigner.Cli\PdfDesigner.Cli.csproj -- --resolve-test
   dotnet run --project PdfDesigner.Cli\PdfDesigner.Cli.csproj -- --design
   dotnet run --project PdfDesigner.Cli\PdfDesigner.Cli.csproj -- --designer-ui
   ```

## Dependencia externa

Por decisión de diseño, `PdfDesigner.Engine` **referencia** el proyecto original
`..\DebugPDF\PdfQuickDebug\PdfQuickDebug.csproj` para reutilizar `XmlParser`, `InvoiceModel`,
`XsltTransform` y `DtoUbl`. No modifiques ese proyecto: el diseñador solo lo lee.
El proyecto original quedó **sin** código del diseñador (extraído aquí).

## Notas

- `data\NUEVO_XML.xml` y `data\FACTURA-UBL.xslt` son copias; los originales viven en `DataTest\`.
- El diseñador lee y escribe únicamente dentro de `PdfDesigner\`; no toca `DataTest\` ni `Templates\`.