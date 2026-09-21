# 📤 EXPORTACIÓN AUTOMÁTICA AL SERVICIO

## 🎯 ¿Qué hace?

Cuando ejecutas `dotnet run`, el programa **AUTOMÁTICAMENTE**:

1. ✅ Genera el PDF con tus datos
2. ✅ Lee el template que modificaste (`Templates/FacturaUbl.cs`)
3. ✅ Cambia los namespaces para que funcionen en el servicio
4. ✅ Guarda el archivo listo para copiar
5. ✅ **Intenta copiarlo directamente al servicio** (si la ruta existe)

**¡Ya NO necesitas script ni copiar manualmente!** 🎉

---

## 🚀 CÓMO USAR

### Paso 1: Modificar Template

Edita el archivo:
```
Templates/FacturaUbl.cs
```

Ejemplo - cambiar logo más grande:
```csharp
// Busca línea 113:
c.Height(48).Image(logoBytes).FitHeight();

// Cambia a:
c.Height(80).Image(logoBytes).FitHeight();
```

Guarda (Ctrl+S)

### Paso 2: Ejecutar

```bash
dotnet run
```

### Paso 3: Listo!

El programa hace TODO automáticamente:

```
📤 [6/6] Exportando template para el servicio...
   ✅ Template exportado: F:\...\FacturaUbl_PARA_SERVICIO.cs
   📦 Backup creado: FacturaUbl.cs.backup_20260113_143022
   ✅ Template copiado al servicio
   📁 E:\...\Services.PdfGenerator\...\FacturaUbl.cs

   🎯 SIGUIENTE PASO PARA SOPORTE:
   cd "E:\...\Services.PdfGenerator"
   dotnet build  ← Compilar para verificar

   Luego hacer commit:
   git add .
   git commit -m "Update FacturaUbl template"
   git push
```

---

## ⚙️ CONFIGURACIÓN

En `Program.cs` líneas 23-26:

```csharp
// ⚙️ EXPORTAR TEMPLATE PARA EL SERVICIO (AUTOMÁTICO)
const bool EXPORTAR_TEMPLATE = true;  // true = exporta automáticamente
const string TEMPLATE_EXPORT_PATH = @"F:\Pruebas de Formatos Masivos\DebugPDF\FacturaUbl_PARA_SERVICIO.cs";
const string SERVICE_TEMPLATE_PATH = @"E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs";
```

### Para deshabilitar exportación:

```csharp
const bool EXPORTAR_TEMPLATE = false;  // No exporta
```

### Para cambiar rutas:

```csharp
const string TEMPLATE_EXPORT_PATH = @"C:\tu\ruta\template.cs";
const string SERVICE_TEMPLATE_PATH = @"C:\tu\servicio\FacturaUbl.cs";
```

---

## 📋 QUÉ HACE AUTOMÁTICAMENTE

### 1. Lee tu template modificado
```
Templates/FacturaUbl.cs
```

### 2. Cambia namespaces

**De:**
```csharp
namespace PdfQuickDebug.Templates;
using PdfQuickDebug.Core;
```

**A:**
```csharp
namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;
using Services.PdfGenerator.Domain.Models;
```

### 3. Guarda 2 archivos

**Archivo 1: Versión exportada (siempre)**
```
F:\Pruebas de Formatos Masivos\DebugPDF\FacturaUbl_PARA_SERVICIO.cs
```

**Archivo 2: Copia directa al servicio (si la ruta existe)**
```
E:\...\Services.PdfGenerator\...\FacturaUbl.cs
```

### 4. Crea backup automático

Antes de sobrescribir, crea:
```
FacturaUbl.cs.backup_20260113_143022
```

---

## 💡 FLUJO COMPLETO

### Para Desarrollador/Diseñador:

```bash
# 1. Modificar template
# Editar: Templates/FacturaUbl.cs

# 2. Probar cambios
dotnet run
# → Ve el PDF generado
# → Ve el template exportado automáticamente

# 3. Si te gusta, ya está copiado al servicio!
```

### Para Soporte:

```bash
# 1. El desarrollador ejecutó dotnet run
# 2. El template ya está en el servicio con namespaces correctos
# 3. Solo falta:

cd "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator"
dotnet build  # Verificar que compila

# 4. Commit
git add Infrastructure/Templates/_1193122070/FacturaUbl.cs
git commit -m "Update FacturaUbl template - [descripción]"
git push
```

---

## 🎯 VENTAJAS

| Antes | Ahora |
|-------|-------|
| ❌ Modificar template | ✅ Modificar template |
| ❌ Guardar | ✅ Guardar |
| ❌ Ejecutar script PowerShell | ❌ **NO NECESITAS** |
| ❌ Copiar archivo manualmente | ❌ **NO NECESITAS** |
| ❌ Cambiar namespaces manualmente | ❌ **NO NECESITAS** |
| ❌ Compilar | ✅ Solo compilar y commit |

**Tiempo ahorrado: 5 minutos → 30 segundos**

---

## 🔍 VERIFICAR QUÉ SE EXPORTÓ

### Ver el archivo exportado:

```bash
code "F:\Pruebas de Formatos Masivos\DebugPDF\FacturaUbl_PARA_SERVICIO.cs"
```

Verifica que tenga:
- ✅ `namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;`
- ✅ `using Services.PdfGenerator.Domain.Models;`
- ✅ Tu modificación (ej: `Height(80)`)

### Ver el archivo en el servicio:

```bash
code "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs"
```

Debe ser IDÉNTICO al exportado.

---

## 🆘 TROUBLESHOOTING

### "⚠️ Ruta del servicio no encontrada"

**Causa:** La carpeta del servicio no existe en tu máquina

**Solución:**
1. El archivo exportado sigue creándose en: `FacturaUbl_PARA_SERVICIO.cs`
2. Cópialo manualmente al servicio
3. O actualiza la ruta en línea 26 de Program.cs

### "⚠️ Error al exportar template"

**Causa:** Problema de permisos o ruta incorrecta

**Solución:**
1. El PDF se genera correctamente de todos modos
2. Copia manualmente: `FacturaUbl_PARA_SERVICIO.cs` → servicio

### "Template no refleja cambios en el servicio"

**Causa:** Compilaste pero no ejecutaste `dotnet run`

**Solución:**
```bash
dotnet run  # Esto exporta + genera PDF
```

No solo:
```bash
dotnet build  # Esto NO exporta
```

---

## 📝 EJEMPLO COMPLETO

### Cambio: Logo más grande

```bash
# 1. Modificar
# Templates/FacturaUbl.cs línea 113:
# Height(48) → Height(80)

# 2. Ejecutar
dotnet run

# Output:
# ✅ PDF generado: DEBUG_OUTPUT.pdf
# ✅ Template exportado: FacturaUbl_PARA_SERVICIO.cs
# ✅ Template copiado al servicio

# 3. Verificar
cd "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator"
dotnet build

# 4. Commit
git add .
git commit -m "feat(pdf): Aumentar tamaño de logo de 48px a 80px"
git push
```

**Tiempo total: 1 minuto** ⚡

---

## ✅ CHECKLIST

Después de ejecutar `dotnet run`:

- [ ] PDF generado correctamente
- [ ] Mensaje: "✅ Template exportado"
- [ ] Mensaje: "✅ Template copiado al servicio"
- [ ] Archivo existe: `FacturaUbl_PARA_SERVICIO.cs`
- [ ] Archivo actualizado en el servicio
- [ ] Backup creado (si había archivo anterior)
- [ ] Compilar servicio: `dotnet build`
- [ ] Hacer commit y push

---

🎉 **¡Exportación automática lista para usar!**
