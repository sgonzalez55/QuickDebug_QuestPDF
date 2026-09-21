# 🎯 PDF Quick Debug - Herramienta de Debugging

## 📋 ¿Qué es esto?

Un proyecto **super simple** en C# para debugear y modificar PDFs generados con datos reales.

**Características:**
- ✅ **Un solo archivo principal** (`Program.cs`) - cambias 3 líneas y listo
- ✅ **Código real del servicio** - mismo template que producción
- ✅ **Debugging completo** con VS Code - breakpoints, variables, step debugging
- ✅ **Sin dependencias externas** - no necesita GCP, MongoDB, ni nada
- ✅ **Hot reload manual** - modificas template, ejecutas, ves cambios

---

## 🚀 INICIO RÁPIDO (2 MINUTOS)

### 1. Abrir en VS Code

```bash
cd "F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug"
code .
```

### 2. Configurar Rutas

Abre `Program.cs` y **edita líneas 16-18**:

```csharp
// 📌 CONFIGURACIÓN - SOLO EDITA ESTA SECCIÓN
const string XML_PATH = @"F:\Pruebas de Formatos Masivos\xml.xml";
const string XSLT_PATH = @"e:\DV-JDL\Modelo Masivos\QA_develop\xslt_final_con_logo.xml";
const string OUTPUT_PDF = @"F:\Pruebas de Formatos Masivos\DEBUG_OUTPUT.pdf";
```

**Copia/Pega tus rutas aquí** ☝️

### 3. Ejecutar

```bash
dotnet run
```

**¡Listo!** Tu PDF estará en la ruta `OUTPUT_PDF`.

---

## 🐛 DEBUGGING CON VS CODE

### Método 1: Ejecutar con Debugging (F5)

1. Abre `Program.cs`
2. Haz click en el número de línea para poner **breakpoint** (aparece punto rojo)
3. Presiona **F5** (o Debug > Start Debugging)
4. El código se detiene en el breakpoint
5. Inspecciona variables haciendo hover o viendo panel VARIABLES

### Método 2: Breakpoints Útiles

**Líneas importantes para debugear:**

| Línea | Qué ver | Variable clave |
|-------|---------|----------------|
| 74 | XML transformado | `transformedXml` |
| 91 | Modelo parseado | `model` |
| 118 | PDF generado | `pdfBytes` |
| `Templates/FacturaUbl.cs:66` | Logo cargado | `logoBytes` |

### Método 3: Ver Variables en Consola

Agrega `Console.WriteLine` en cualquier lugar:

```csharp
// Ejemplo: Ver CustomFields
foreach (var field in model.Additional.CustomFields)
{
    Console.WriteLine($"{field.Key}: {field.Value}");
}
```

---

## 💡 MODIFICAR EL TEMPLATE (Diseño Visual)

### Cambios Comunes:

#### 1. Logo más Grande

**Archivo:** `Templates/FacturaUbl.cs`
**Buscar:** `c.Height(48).Image(logoBytes)`
**Cambiar a:** `c.Height(80).Image(logoBytes)` (o 100, 120, etc.)

#### 2. Título más Grande

**Buscar:** `.FontSize(9.5f).Bold()`
**Cambiar a:** `.FontSize(12f).Bold()` (o 14f, 16f, etc.)

#### 3. Cambiar Color del Título

**Buscar:** `.FontColor(BrandColors.TextBlack)`
**Cambiar a:** `.FontColor("#FF0000")` (rojo) o `"#0066CC"` (azul)

#### 4. Más Espacio entre Secciones

**Buscar:** `.PaddingTop(3.5f)`
**Cambiar a:** `.PaddingTop(5f)` o más

### Workflow después de cambiar:

```bash
# 1. Guardar cambios (Ctrl+S)
# 2. Ejecutar de nuevo
dotnet run

# 3. Ver nuevo PDF en OUTPUT_PDF
```

---

## 📁 ESTRUCTURA DEL PROYECTO

```
PdfQuickDebug/
│
├── Program.cs                    # ← ARCHIVO PRINCIPAL (configuras aquí)
│   └── Líneas 16-18: RUTAS      # Copia/pega tus rutas
│
├── Core/                         # Código auxiliar (NO tocar)
│   ├── Models.cs                # InvoiceModel, DocumentInfo, etc.
│   ├── XmlParser.cs             # Parser de XML transformado
│   └── XsltTransform.cs         # Transformación XSLT
│
└── Templates/                    # Templates de diseño
    └── FacturaUbl.cs            # ← EDITA AQUÍ para cambios visuales
```

---

## 🎯 CASOS DE USO

### Caso 1: Verificar que Logo Aparece

```csharp
// Pon breakpoint en Templates/FacturaUbl.cs línea 66
var logoBytes = SafeLoadLogo(GetCustomField(model, "LogoBase64", ""));

// Inspeccionar:
// - logoBytes == null? → Logo NO se cargó
// - logoBytes != null? → Logo OK (ver tamaño: logoBytes.Length)
```

### Caso 2: Ver CustomFields del XML

```csharp
// Pon breakpoint en Program.cs línea 104
foreach (var field in model.Additional.CustomFields.Take(5))
{
    // Inspeccionar field.Key y field.Value
}

// Ver si LogoBase64, ResolucionTexto, ValorLetras existen
```

### Caso 3: Debug de Transformación XSLT

```csharp
// Pon breakpoint en Program.cs línea 74
var transformedXml = transformer.Transform(xml, xslt);

// Inspeccionar transformedXml (ver contenido completo)
// Ver si tiene <Adicional><Campo clave="LogoBase64"...
```

---

## 🛠️ DEPENDENCIAS (Ya Instaladas)

El proyecto usa estos paquetes NuGet:

- **QuestPDF** (2024.12.3) - Generación de PDFs
- **QRCoder** (1.4.3) - Códigos QR
- **System.Text.Encoding.CodePages** (8.0.0) - Fix de encoding UTF-8

Se instalan automáticamente con `dotnet restore`.

---

## ⚙️ CONFIGURACIÓN AVANZADA

### Deshabilitar Companion (ya deshabilitado)

En `Program.cs` línea 24:

```csharp
const bool ENABLE_COMPANION = false;  // Ya está en false
```

### Cambiar NIT (para otro template)

```csharp
const string NIT = "1193122070";  // Cambiar por otro NIT
```

### Cambiar Puerto Companion (si lo activas)

```csharp
const int COMPANION_PORT = 12500;  // Cambiar puerto
```

---

## 🆘 TROUBLESHOOTING

### "XML no encontrado"

Verifica que la ruta en línea 16 sea correcta:

```csharp
const string XML_PATH = @"F:\tu\ruta\xml.xml";  // Ruta absoluta
```

### "XSLT no encontrado"

Verifica que la ruta en línea 17 sea correcta:

```csharp
const string XSLT_PATH = @"e:\tu\ruta\xslt.xml";  // Ruta absoluta
```

### "Logo no aparece"

1. Pon breakpoint en `Templates/FacturaUbl.cs:66`
2. Ver si `logoBytes == null`
3. Si es null, revisar:
   - CustomField "LogoBase64" existe?
   - Tiene prefijo `data:image/png;base64,`?

### "Error de compilación"

```bash
# Limpiar y recompilar
dotnet clean
dotnet build
```

---

## 📖 DOCUMENTACIÓN ADICIONAL

- **QuestPDF Docs**: https://www.questpdf.com/
- **QRCoder Docs**: https://github.com/codebude/QRCoder

---

## ✅ CHECKLIST PRIMERA VEZ

- [ ] Abrir proyecto en VS Code
- [ ] Editar rutas en `Program.cs` líneas 16-18
- [ ] Ejecutar `dotnet run`
- [ ] Verificar que PDF se genera en `OUTPUT_PDF`
- [ ] Poner breakpoint en línea 74
- [ ] Presionar F5 para debugging
- [ ] Inspeccionar variable `transformedXml`
- [ ] Continuar (F5) hasta línea 91
- [ ] Inspeccionar variable `model`
- [ ] Ver `model.Additional.CustomFields`

**¡Listo para debugear!** 🎉

---

## 🎓 TIPS FINALES

1. **Siempre compila antes de ejecutar:**
   ```bash
   dotnet build && dotnet run
   ```

2. **Ver stack trace completo** en errores (modo DEBUG)

3. **Usar `Console.WriteLine`** para debugging rápido

4. **Breakpoints condicionales** en VS Code:
   - Click derecho en breakpoint → Edit Breakpoint → Condición

5. **Watch expressions** en panel DEBUG:
   - Agregar expresiones personalizadas para monitorear

---

**¿Dudas?** Revisa el código en `Program.cs` - está todo comentado y explicado. 📚
