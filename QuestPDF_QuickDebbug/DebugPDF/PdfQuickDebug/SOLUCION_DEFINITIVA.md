# 🎯 SOLUCIÓN DEFINITIVA - DEBUGGING EN VS CODE

## ⚡ PROBLEMA

Cuando presionas F5 o el botón Run en VS Code, sale este error:
```
Couldn't find a debug adapter descriptor for debug type 'coreclr'
```

## ✅ SOLUCIÓN (2 MINUTOS)

### Paso 1: Instalar C# Dev Kit

1. Abre VS Code
2. Presiona `Ctrl+Shift+X` (abre panel de extensiones)
3. Busca: **C# Dev Kit**
4. Click en **Install**
5. **REINICIA VS CODE** (cierra y abre de nuevo)

### Paso 2: Abrir el proyecto correcto

1. En VS Code: `File > Open Folder`
2. Selecciona: `F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug`
3. (NO abras `DebugPDF`, abre la carpeta `PdfQuickDebug` dentro)

### Paso 3: Debugear

1. Abre `Program.cs`
2. Click en el margen izquierdo en la **línea 74** (aparece punto rojo 🔴)
3. Presiona **F5** (NO el botón ▶️ Run)
4. El programa se detendrá en línea 74
5. Pasa el mouse sobre variables para ver sus valores
6. Usa **F10** para siguiente línea, **F11** para entrar en función

---

## 🎯 VERIFICAR QUE FUNCIONÓ

Si instalaste correctamente:
1. Cuando presionas F5, debe compilar y ejecutar
2. Se detiene en los breakpoints (puntos rojos 🔴)
3. Puedes ver valores de variables al pasar el mouse
4. Panel lateral izquierdo muestra todas las variables

---

## 🚀 WORKFLOW COMPLETO

### Para Desarrollador:

```bash
# 1. Modificar template
# Edita: Templates/FacturaUbl.cs
# Ejemplo: Height(48) → Height(80)

# 2. Ejecutar y ver resultado
dotnet run

# Automáticamente:
# ✅ Genera PDF: DEBUG_OUTPUT.pdf
# ✅ Exporta template: FacturaUbl_PARA_SERVICIO.cs
# ✅ Copia al servicio con namespaces correctos
# ✅ Crea backup automático

# 3. Si necesitas debugear con breakpoints
# Abre Program.cs
# Click en línea para breakpoint (🔴)
# Presiona F5
```

### Para Soporte (Integrar al servicio):

```bash
# El desarrollador ya ejecutó "dotnet run"
# El template ya está copiado en el servicio con namespaces correctos

# 1. Compilar servicio
cd "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator"
dotnet build

# 2. Commit
cd "E:\DV-JDL\Modelo Masivos\QA_develop"
git add code/src/Services.PdfGenerator/Infrastructure/Templates/_1193122070/FacturaUbl.cs
git commit -m "Update FacturaUbl template"
git push
```

---

## 📋 RUTAS CONFIGURADAS

En `Program.cs` líneas 15-25:

```csharp
const string XML_PATH = @"F:\Pruebas de Formatos Masivos\xml.xml";
const string XSLT_PATH = @"e:\DV-JDL\Modelo Masivos\QA_develop\xslt_final_con_logo.xml";
const string OUTPUT_PDF = @"F:\Pruebas de Formatos Masivos\DEBUG_OUTPUT.pdf";
const string NIT = "1193122070";

// Exportación automática
const bool EXPORTAR_TEMPLATE = true;
const string TEMPLATE_EXPORT_PATH = @"F:\Pruebas de Formatos Masivos\DebugPDF\FacturaUbl_PARA_SERVICIO.cs";
const string SERVICE_TEMPLATE_PATH = @"E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs";
```

Si necesitas cambiar rutas, solo edita estas líneas.

---

## 🐛 ALTERNATIVA SIN EXTENSIÓN

Si NO quieres instalar C# Dev Kit, puedes usar `Console.WriteLine`:

Agrega en `Program.cs` después de línea 91:

```csharp
// Después de: var model = XmlParser.Parse(transformedXml);

Console.WriteLine("\n🔍 DEBUG - VALORES DEL MODELO:");
Console.WriteLine($"   Número: {model.Document.Number}");
Console.WriteLine($"   Prefijo: {model.Document.Prefix}");
Console.WriteLine($"   Emisor: {model.Issuer.Name}");
Console.WriteLine($"   Cliente: {model.Customer.Name}");
Console.WriteLine($"   Total: ${model.Totals.Total:N2}");
Console.WriteLine($"   Líneas: {model.Lines.Count}");

// Custom Fields
Console.WriteLine($"\n🔖 CUSTOM FIELDS ({model.Additional.CustomFields.Count}):");
foreach (var field in model.Additional.CustomFields)
{
    var value = field.Value;
    if (value.Length > 100)
    {
        Console.WriteLine($"   • {field.Key}: {value.Substring(0, 100)}... ({value.Length} chars)");
    }
    else
    {
        Console.WriteLine($"   • {field.Key}: {value}");
    }
}
```

Luego ejecuta:
```bash
dotnet run
```

Verás TODO el output en consola.

---

## 🆘 TROUBLESHOOTING

### "coreclr error" sigue apareciendo
→ Instalaste C# Dev Kit pero NO reiniciaste VS Code
→ **Solución**: Cierra VS Code completamente y ábrelo de nuevo

### "No para en breakpoints"
→ Estás usando el botón ▶️ Run en lugar de F5
→ **Solución**: Usa **F5** en lugar del botón

### "Template no se exporta"
→ El PDF se genera correctamente de todos modos
→ **Solución**: Usa el archivo `FacturaUbl_PARA_SERVICIO.cs` y cópialo manualmente

### "Namespace error al compilar servicio"
→ No ejecutaste `dotnet run` en PdfQuickDebug
→ **Solución**: Ejecuta `dotnet run` primero (esto exporta con namespaces correctos)

---

## ✅ CHECKLIST

- [ ] C# Dev Kit instalado
- [ ] VS Code reiniciado
- [ ] Carpeta correcta abierta (`PdfQuickDebug`, no `DebugPDF`)
- [ ] Rutas configuradas en líneas 15-25
- [ ] Ejecutar: `dotnet run` → genera PDF
- [ ] Presionar F5 → se detiene en breakpoints
- [ ] Modificar template → `dotnet run` → PDF actualizado
- [ ] Template exportado automáticamente al servicio
- [ ] Compilar servicio: `dotnet build`
- [ ] Commit y push

---

## 🎉 RESULTADO FINAL

**ANTES (complicado):**
1. Modificar template en servicio
2. Compilar servicio completo
3. Esperar 5 minutos
4. Ver resultado
5. Repetir...

**AHORA (simple):**
1. Modificar template en PdfQuickDebug
2. `dotnet run` (10 segundos)
3. Ver PDF generado
4. Template automáticamente exportado al servicio
5. Soporte compila y hace commit
6. Listo

**Tiempo ahorrado: 5 minutos → 30 segundos por iteración**

---

## 📞 OTROS ARCHIVOS DE AYUDA

- `INSTALAR_EXTENSION.md` - Guía detallada de instalación
- `COMO_DEBUGEAR.md` - 3 métodos de debugging
- `DEBUGGING_SIN_EXTENSION.md` - Debugging con Console.WriteLine
- `COMO_USAR_EXPORTACION.md` - Exportación automática
- `PARA_SOPORTE_INTEGRAR.md` - Guía para soporte
- `LEEME_PRIMERO.txt` - Resumen ejecutivo
- `RESUMEN_FINAL.txt` - Resumen completo de funcionalidades

---

🚀 **¡TODO LISTO PARA USAR!**

Instala C# Dev Kit, reinicia VS Code, presiona F5, y ya puedes debugear con breakpoints.

Si tienes dudas, revisa: `INSTALAR_EXTENSION.md`
