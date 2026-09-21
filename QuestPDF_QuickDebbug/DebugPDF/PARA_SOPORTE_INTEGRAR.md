# 📦 PARA SOPORTE: Cómo Integrar Cambios al Servicio

## 🎯 Objetivo

Después de modificar el template en `PdfQuickDebug`, integrarlo al servicio real.

---

## ✅ PASO A PASO (2 minutos)

### 1. Modificar el Template en PdfQuickDebug

Edita el archivo:
```
F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug\Templates\FacturaUbl.cs
```

Haz tus cambios y prueba con:
```bash
dotnet run
```

### 2. Copiar Template al Servicio

**ORIGEN:**
```
F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug\Templates\FacturaUbl.cs
```

**DESTINO:**
```
E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs
```

**Comando PowerShell:**
```powershell
Copy-Item `
  "F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug\Templates\FacturaUbl.cs" `
  "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs" `
  -Force
```

**O manualmente:**
- Abre ambos archivos
- Copia TODO el contenido del archivo de PdfQuickDebug
- Pega en el archivo del servicio
- Guarda

### 3. Ajustar Namespaces

El archivo copiado tiene:
```csharp
namespace PdfQuickDebug.Templates;
```

**Cambiar a:**
```csharp
namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;
```

Y el using:
```csharp
using PdfQuickDebug.Core;
```

**Cambiar a:**
```csharp
using Services.PdfGenerator.Domain.Models;
```

### 4. Compilar Servicio

```bash
cd "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator"
dotnet build
```

Si compila = ✅ **TODO OK**

### 5. Commit y Push

```bash
cd "E:\DV-JDL\Modelo Masivos\QA_develop"
git add code/src/Services.PdfGenerator/Infrastructure/Templates/_1193122070/FacturaUbl.cs
git commit -m "Update FacturaUbl template - [descripción del cambio]"
git push
```

---

## 🤖 SCRIPT AUTOMÁTICO

Crea este archivo: `INTEGRAR_TEMPLATE.ps1`

```powershell
# INTEGRAR_TEMPLATE.ps1
# Copia template desde PdfQuickDebug al servicio y ajusta namespaces

$origen = "F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug\Templates\FacturaUbl.cs"
$destino = "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs"

Write-Host "📂 Copiando template..." -ForegroundColor Cyan

# Leer contenido
$contenido = Get-Content $origen -Raw

# Reemplazar namespaces
$contenido = $contenido -replace "namespace PdfQuickDebug.Templates;", "namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;"
$contenido = $contenido -replace "using PdfQuickDebug.Core;", "using Services.PdfGenerator.Domain.Models;"

# Guardar
$contenido | Set-Content $destino -Encoding UTF8

Write-Host "✅ Template integrado exitosamente" -ForegroundColor Green
Write-Host "📁 Ubicación: $destino" -ForegroundColor Yellow
Write-Host ""
Write-Host "🔧 Siguiente paso: Compilar servicio" -ForegroundColor Cyan
Write-Host "   cd 'E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator'" -ForegroundColor Gray
Write-Host "   dotnet build" -ForegroundColor Gray
```

**Ejecutar:**
```powershell
.\INTEGRAR_TEMPLATE.ps1
```

---

## 📋 CHECKLIST

Antes de hacer commit, verifica:

- [ ] Template copiado al servicio
- [ ] Namespace cambiado a `Services.PdfGenerator.Infrastructure.Templates._1193122070`
- [ ] Using cambiado a `Services.PdfGenerator.Domain.Models`
- [ ] Servicio compila sin errores (`dotnet build`)
- [ ] Probado en local (generar PDF de prueba)
- [ ] Commit con mensaje descriptivo
- [ ] Push a branch correspondiente

---

## 🆘 TROUBLESHOOTING

### Error: "The name 'InvoiceModel' could not be found"

Verifica el using:
```csharp
using Services.PdfGenerator.Domain.Models;
```

### Error: "Namespace does not match"

Verifica el namespace:
```csharp
namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;
```

### PDF no refleja cambios

1. Verifica que copiaste el archivo correcto
2. Recompila: `dotnet build`
3. Reinicia el servicio
4. Limpia cache: `dotnet clean && dotnet build`

---

## 💡 TIPS

1. **Siempre prueba en PdfQuickDebug PRIMERO** antes de integrar
2. **Documenta los cambios** en el mensaje de commit
3. **Haz backup** del template antes de sobrescribirlo
4. **Usa el script automático** para evitar errores manuales

---

## 📝 EJEMPLO DE COMMIT

```bash
git add code/src/Services.PdfGenerator/Infrastructure/Templates/_1193122070/FacturaUbl.cs
git commit -m "feat(pdf-template): Aumentar tamaño de logo de 48px a 80px

- Cambio en línea 113: Height(48) → Height(80)
- Mejora visual según feedback del cliente
- Probado con xml.xml en PdfQuickDebug
"
git push origin feature/update-logo-size
```

---

🎉 **¡Listo para integrar cambios al servicio!**
