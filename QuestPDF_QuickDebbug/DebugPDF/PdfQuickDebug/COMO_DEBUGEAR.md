# 🐛 CÓMO DEBUGEAR - GUÍA SIMPLE

## ❌ PROBLEMA: VS Code pide extensión "coreclr"

Si ves el error "Couldn't find a debug adapter descriptor for debug type 'coreclr'", es porque VS Code necesita una extensión de C#.

---

## ✅ SOLUCIÓN: 3 MÉTODOS DE DEBUGGING (del más simple al más avanzado)

---

## 🥇 MÉTODO 1: Console.WriteLine (MÁS SIMPLE)

### Ventajas:
- ✅ No necesita extensiones
- ✅ Funciona en cualquier editor
- ✅ Super rápido

### Cómo usar:

1. **Abre Program.cs**

2. **Agrega Console.WriteLine donde quieras ver datos:**

```csharp
// Después de línea 74 (transformación):
var transformedXml = transformer.Transform(xml, xslt);
Console.WriteLine("\n🔍 DEBUG - XML Transformado (primeros 500 chars):");
Console.WriteLine(transformedXml.Substring(0, Math.Min(500, transformedXml.Length)));

// Después de línea 91 (modelo):
var model = XmlParser.Parse(transformedXml);
Console.WriteLine("\n🔍 DEBUG - Modelo Parseado:");
Console.WriteLine($"   Documento: {model.Document.Number}");
Console.WriteLine($"   Cliente: {model.Customer.Name}");
Console.WriteLine($"   Total: ${model.Totals.Total:N0}");
Console.WriteLine($"   CustomFields Count: {model.Additional.CustomFields.Count}");

// Ver todos los CustomFields:
foreach (var field in model.Additional.CustomFields)
{
    var value = field.Value.Length > 50 ? field.Value.Substring(0, 50) + "..." : field.Value;
    Console.WriteLine($"   • {field.Key}: {value}");
}
```

3. **Ejecutar:**

```bash
dotnet run
```

4. **Ver output en consola** con todos los datos

### Ejemplo completo:

```csharp
// Línea 91 - Después de parsear
var model = XmlParser.Parse(transformedXml);

// 🐛 DEBUGGING: Ver todo el modelo
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("🔍 DEBUGGING - DATOS COMPLETOS");
Console.WriteLine(new string('=', 60));

Console.WriteLine("\n📄 DOCUMENTO:");
Console.WriteLine($"   Número: {model.Document.Number}");
Console.WriteLine($"   Prefijo: {model.Document.Prefix}");
Console.WriteLine($"   Fecha: {model.Document.IssueDate:yyyy-MM-dd}");

Console.WriteLine("\n👤 CLIENTE:");
Console.WriteLine($"   Nombre: {model.Customer.Name}");
Console.WriteLine($"   NIT: {model.Customer.TaxId}");
Console.WriteLine($"   Dirección: {model.Customer.Address}");

Console.WriteLine("\n💰 TOTALES:");
Console.WriteLine($"   Subtotal: ${model.Totals.Subtotal:N2}");
Console.WriteLine($"   Impuestos: ${model.Totals.TotalTax:N2}");
Console.WriteLine($"   TOTAL: ${model.Totals.Total:N2}");

Console.WriteLine("\n📦 LÍNEAS:");
foreach (var line in model.Lines.Take(3))
{
    Console.WriteLine($"   {line.LineNumber}. {line.Description} - ${line.LineTotal:N2}");
}

Console.WriteLine("\n🔖 CUSTOMFIELDS:");
foreach (var field in model.Additional.CustomFields)
{
    var val = field.Value.Length > 80 ? field.Value.Substring(0, 80) + "..." : field.Value;
    Console.WriteLine($"   • {field.Key}: {val}");
}

Console.WriteLine("\n" + new string('=', 60));
```

---

## 🥈 MÉTODO 2: Instalar Extensión C# DevKit (Recomendado)

Si quieres debugging REAL con breakpoints:

### Paso 1: Instalar extensión

1. Abre VS Code
2. Ve a Extensions (Ctrl+Shift+X)
3. Busca: **"C# Dev Kit"**
4. Click en "Install"
5. Reinicia VS Code

### Paso 2: Configurar debugging

Una vez instalada la extensión, el archivo `launch.json` funcionará automáticamente.

### Paso 3: Usar breakpoints

1. Abre `Program.cs`
2. Click en el número de línea 74 (aparece punto rojo 🔴)
3. Presiona **F5**
4. El programa se detiene en el breakpoint
5. Inspecciona variables:
   - Pasa mouse sobre variables
   - Ve panel "VARIABLES" (lado izquierdo)
   - Ve panel "WATCH" para expresiones personalizadas

### Paso 4: Controles de debugging

- **F5** = Continue (continuar hasta próximo breakpoint)
- **F10** = Step Over (siguiente línea)
- **F11** = Step Into (entrar a función)
- **Shift+F11** = Step Out (salir de función)
- **Shift+F5** = Stop debugging

---

## 🥉 MÉTODO 3: Visual Studio 2022 (Más Potente)

Si tienes Visual Studio 2022 instalado:

1. Abre: `PdfQuickDebug.sln` (crear primero):
   ```bash
   dotnet new sln
   dotnet sln add PdfQuickDebug.csproj
   ```

2. Doble click en `PdfQuickDebug.sln`

3. Debugging avanzado con todas las herramientas

---

## 🎯 COMPARACIÓN DE MÉTODOS

| Característica | Console.WriteLine | C# Dev Kit | Visual Studio |
|----------------|-------------------|------------|---------------|
| **Setup** | 0 min | 2 min | Requiere VS |
| **Facilidad** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Breakpoints** | ❌ | ✅ | ✅ |
| **Inspeccionar variables** | Manual | ✅ Auto | ✅ Auto |
| **Step debugging** | ❌ | ✅ | ✅ |
| **Performance** | Rápido | Medio | Lento |

---

## 💡 MI RECOMENDACIÓN

### Para empezar HOY:
**→ Método 1 (Console.WriteLine)**
- Copia el código de ejemplo
- Pégalo en Program.cs después de línea 91
- Ejecuta: `dotnet run`
- Ve TODO el modelo en consola

### Para debugging profesional:
**→ Método 2 (C# Dev Kit)**
- Instala la extensión (2 minutos)
- Usa breakpoints
- Inspecciona variables visualmente

---

## 📋 EJEMPLO PRÁCTICO: DEBUGEAR LOGO

### Problema: Logo no aparece

**Con Console.WriteLine:**

```csharp
// En Templates/FacturaUbl.cs línea 66, ANTES de SafeLoadLogo:
var logoUrl = GetCustomField(model, "LogoBase64", "");
Console.WriteLine($"\n🔍 DEBUG LOGO:");
Console.WriteLine($"   URL Length: {logoUrl.Length}");
Console.WriteLine($"   Starts with 'data:': {logoUrl.StartsWith("data:")}");
Console.WriteLine($"   First 100 chars: {logoUrl.Substring(0, Math.Min(100, logoUrl.Length))}");

var logoBytes = SafeLoadLogo(logoUrl);
Console.WriteLine($"   Logo Bytes: {(logoBytes == null ? "NULL ❌" : $"{logoBytes.Length} bytes ✅")}");
```

**Ejecutar y ver:**
```bash
dotnet run
```

**Output esperado:**
```
🔍 DEBUG LOGO:
   URL Length: 21778
   Starts with 'data:': True
   First 100 chars: data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...
   Logo Bytes: 16000 bytes ✅
```

Si ves `NULL ❌`, sabes que el problema está en SafeLoadLogo.

---

## 🆘 TROUBLESHOOTING

### "No veo el output de Console.WriteLine"

Asegúrate de ejecutar:
```bash
dotnet run
```

No uses F5 en VS Code sin la extensión instalada.

### "Quiero ver variables complejas"

Usa JSON:
```csharp
using System.Text.Json;

// Ver objeto completo como JSON
var modelJson = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(modelJson);
```

### "Quiero guardar output en archivo"

```csharp
// Al final de Program.cs Main():
File.WriteAllText("debug_output.txt", $@"
Documento: {model.Document.Number}
Cliente: {model.Customer.Name}
Total: {model.Totals.Total}
CustomFields: {string.Join(", ", model.Additional.CustomFields.Keys)}
");
```

---

## ✅ CONCLUSIÓN

**Usa Método 1 (Console.WriteLine) ahora mismo** - es super efectivo y no necesita nada.

Cuando tengas tiempo, instala C# Dev Kit para debugging visual.

**¡Listo para debugear!** 🎉
