# ════════════════════════════════════════════════════════════════
# SCRIPT: INTEGRAR TEMPLATE AL SERVICIO
# ════════════════════════════════════════════════════════════════
#
# Este script copia el template desde PdfQuickDebug al servicio real
# y ajusta automáticamente los namespaces.
#
# Uso: .\INTEGRAR_TEMPLATE.ps1
#
# ════════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"

# Rutas
$origen = "F:\Pruebas de Formatos Masivos\DebugPDF\PdfQuickDebug\Templates\FacturaUbl.cs"
$destino = "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator\Infrastructure\Templates\_1193122070\FacturaUbl.cs"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         INTEGRAR TEMPLATE AL SERVICIO                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Verificar archivo origen existe
if (-not (Test-Path $origen)) {
    Write-Host "❌ ERROR: No se encuentra el archivo origen" -ForegroundColor Red
    Write-Host "   Ruta: $origen" -ForegroundColor Yellow
    exit 1
}

Write-Host "📂 [1/5] Leyendo template desde PdfQuickDebug..." -ForegroundColor Green
$contenido = Get-Content $origen -Raw
$tamano = $contenido.Length
Write-Host "   ✅ Template leído ($tamano caracteres)" -ForegroundColor Gray

# Backup del archivo destino si existe
if (Test-Path $destino) {
    Write-Host ""
    Write-Host "📦 [2/5] Creando backup del template actual..." -ForegroundColor Green
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backup = "$destino.backup_$timestamp"
    Copy-Item $destino $backup
    Write-Host "   ✅ Backup creado: $backup" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "📦 [2/5] No existe template anterior (primera vez)" -ForegroundColor Green
}

# Ajustar namespaces
Write-Host ""
Write-Host "🔧 [3/5] Ajustando namespaces..." -ForegroundColor Green

# Namespace principal
$contenidoOriginal = $contenido
$contenido = $contenido -replace "namespace PdfQuickDebug\.Templates;", "namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;"

# Usings
$contenido = $contenido -replace "using PdfQuickDebug\.Core;", "using Services.PdfGenerator.Domain.Models;"

# Verificar cambios
if ($contenido -eq $contenidoOriginal) {
    Write-Host "   ⚠️  ADVERTENCIA: No se detectaron cambios en namespaces" -ForegroundColor Yellow
    Write-Host "   El archivo ya podría tener los namespaces correctos" -ForegroundColor Gray
} else {
    Write-Host "   ✅ Namespace cambiado: Services.PdfGenerator.Infrastructure.Templates._1193122070" -ForegroundColor Gray
    Write-Host "   ✅ Using cambiado: Services.PdfGenerator.Domain.Models" -ForegroundColor Gray
}

# Guardar archivo
Write-Host ""
Write-Host "💾 [4/5] Guardando template en servicio..." -ForegroundColor Green
$contenido | Set-Content $destino -Encoding UTF8
Write-Host "   ✅ Template guardado en: $destino" -ForegroundColor Gray

# Compilar servicio
Write-Host ""
Write-Host "🔨 [5/5] Compilando servicio..." -ForegroundColor Green
Write-Host "   Ejecutando: dotnet build" -ForegroundColor Gray

$servicePath = "E:\DV-JDL\Modelo Masivos\QA_develop\code\src\Services.PdfGenerator"
Push-Location $servicePath

try {
    $buildOutput = dotnet build 2>&1 | Out-String

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Compilación exitosa" -ForegroundColor Gray
    } else {
        Write-Host "   ❌ Error en compilación" -ForegroundColor Red
        Write-Host ""
        Write-Host "Output del build:" -ForegroundColor Yellow
        Write-Host $buildOutput
        Pop-Location
        exit 1
    }
} finally {
    Pop-Location
}

# Resumen
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                  ✅ INTEGRACIÓN EXITOSA                      ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📄 Template actualizado en:" -ForegroundColor Cyan
Write-Host "   $destino" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Siguiente paso: Commit y Push" -ForegroundColor Cyan
Write-Host ""
Write-Host "   cd 'E:\DV-JDL\Modelo Masivos\QA_develop'" -ForegroundColor Gray
Write-Host "   git add code/src/Services.PdfGenerator/Infrastructure/Templates/_1193122070/FacturaUbl.cs" -ForegroundColor Gray
Write-Host "   git commit -m 'Update FacturaUbl template'" -ForegroundColor Gray
Write-Host "   git push" -ForegroundColor Gray
Write-Host ""
Write-Host "🎉 ¡Listo!" -ForegroundColor Green
Write-Host ""
