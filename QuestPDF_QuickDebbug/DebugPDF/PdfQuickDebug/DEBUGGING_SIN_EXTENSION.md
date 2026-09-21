# 🐛 DEBUGGING SIN EXTENSIÓN (MÉTODO ALTERNATIVO)

Si no quieres instalar C# Dev Kit, puedes debugear con `Console.WriteLine`.

## 📝 EJEMPLO: Ver todo el modelo parseado

Agrega esto en `Program.cs` después de la **línea 91**:

```csharp
// Después de esta línea:
var model = XmlParser.Parse(transformedXml);

// AGREGA ESTO: 👇
Console.WriteLine("\n" + new string('═', 70));
Console.WriteLine("🔍 DEBUGGING COMPLETO - INVOICE MODEL");
Console.WriteLine(new string('═', 70));

// DOCUMENTO
Console.WriteLine("\n📄 DOCUMENTO:");
Console.WriteLine($"   Número: {model.Document.Number}");
Console.WriteLine($"   Prefijo: {model.Document.Prefix}");
Console.WriteLine($"   Fecha Emisión: {model.Document.IssueDate:yyyy-MM-dd}");
Console.WriteLine($"   Fecha Vencimiento: {model.Document.DueDate:yyyy-MM-dd}");
Console.WriteLine($"   Moneda: {model.Document.Currency}");

// EMISOR
Console.WriteLine("\n🏢 EMISOR:");
Console.WriteLine($"   Nombre: {model.Issuer.Name}");
Console.WriteLine($"   NIT: {model.Issuer.TaxId}");
Console.WriteLine($"   Dirección: {model.Issuer.Address}");
Console.WriteLine($"   Ciudad: {model.Issuer.City}");
Console.WriteLine($"   Teléfono: {model.Issuer.Phone}");
Console.WriteLine($"   Email: {model.Issuer.Email}");

// CLIENTE
Console.WriteLine("\n👤 CLIENTE:");
Console.WriteLine($"   Nombre: {model.Customer.Name}");
Console.WriteLine($"   NIT: {model.Customer.TaxId}");
Console.WriteLine($"   Dirección: {model.Customer.Address}");
Console.WriteLine($"   Ciudad: {model.Customer.City}");

// TOTALES
Console.WriteLine("\n💰 TOTALES:");
Console.WriteLine($"   Subtotal: ${model.Totals.Subtotal:N2}");
Console.WriteLine($"   Descuentos: ${model.Totals.TotalDiscount:N2}");
Console.WriteLine($"   Impuestos: ${model.Totals.TotalTax:N2}");
Console.WriteLine($"   TOTAL: ${model.Totals.Total:N2}");

// LÍNEAS DE DETALLE
Console.WriteLine($"\n📦 LÍNEAS DE DETALLE ({model.Lines.Count}):");
for (int i = 0; i < Math.Min(5, model.Lines.Count); i++)
{
    var line = model.Lines[i];
    Console.WriteLine($"   {line.LineNumber}. {line.Description}");
    Console.WriteLine($"      Cantidad: {line.Quantity} {line.Unit}");
    Console.WriteLine($"      Precio: ${line.UnitPrice:N2}");
    Console.WriteLine($"      Descuento: ${line.Discount:N2}");
    Console.WriteLine($"      IVA: {line.TaxRate}%");
    Console.WriteLine($"      Total Línea: ${line.LineTotal:N2}");
    Console.WriteLine();
}

if (model.Lines.Count > 5)
    Console.WriteLine($"   ... y {model.Lines.Count - 5} líneas más");

// QR
Console.WriteLine("\n🔲 CÓDIGO QR:");
Console.WriteLine($"   CUFE: {model.QR.CUFE}");
Console.WriteLine($"   QR Code: {(string.IsNullOrEmpty(model.QR.QRCode) ? "No disponible" : $"{model.QR.QRCode.Length} caracteres")}");

// INFORMACIÓN ADICIONAL
Console.WriteLine("\n📋 INFORMACIÓN ADICIONAL:");
Console.WriteLine($"   Forma de Pago: {model.Additional.PaymentMethod}");
Console.WriteLine($"   Notas: {model.Additional.Notes}");

// CUSTOM FIELDS (LO MÁS IMPORTANTE PARA DEBUGGING)
Console.WriteLine($"\n🔖 CUSTOM FIELDS ({model.Additional.CustomFields.Count}):");
foreach (var field in model.Additional.CustomFields)
{
    var value = field.Value;

    // Manejar campos largos (como logo base64)
    if (value.Length > 100)
    {
        if (field.Key == "LogoBase64" && value.StartsWith("data:image/"))
        {
            var commaIndex = value.IndexOf(',');
            var prefix = value.Substring(0, commaIndex + 1);
            Console.WriteLine($"   • {field.Key}: {prefix}[base64 data: {value.Length - commaIndex - 1} chars]");
        }
        else
        {
            Console.WriteLine($"   • {field.Key}: {value.Substring(0, 100)}... ({value.Length} chars total)");
        }
    }
    else
    {
        Console.WriteLine($"   • {field.Key}: {value}");
    }
}

Console.WriteLine("\n" + new string('═', 70));
```

## 🎯 VERIFICAR LOGO

Si quieres ver específicamente si el logo se carga, agrega esto en `Templates/FacturaUbl.cs` **línea 66**:

```csharp
// ANTES de esta línea:
var logoBytes = SafeLoadLogo(GetCustomField(model, "LogoBase64", ""));

// AGREGA ESTO: 👇
var logoUrl = GetCustomField(model, "LogoBase64", "");
Console.WriteLine("\n🔍 DEBUG LOGO:");
Console.WriteLine($"   CustomField 'LogoBase64' existe: {!string.IsNullOrEmpty(logoUrl)}");
Console.WriteLine($"   Longitud: {logoUrl.Length} caracteres");
Console.WriteLine($"   Inicia con 'data:image/': {logoUrl.StartsWith("data:image/")}");

if (logoUrl.Length > 0)
{
    var preview = logoUrl.Substring(0, Math.Min(100, logoUrl.Length));
    Console.WriteLine($"   Primeros 100 chars: {preview}");
}

var logoBytes = SafeLoadLogo(logoUrl);
Console.WriteLine($"   Logo cargado: {(logoBytes != null ? $"SÍ ✅ ({logoBytes.Length} bytes)" : "NO ❌")}");
```

## 🚀 EJECUTAR Y VER OUTPUT

```bash
dotnet run
```

Verás TODO el output detallado en consola.

## 📊 EJEMPLO DE OUTPUT

```
═══════════════════════════════════════════════════════════════════
🔍 DEBUGGING COMPLETO - INVOICE MODEL
═══════════════════════════════════════════════════════════════════

📄 DOCUMENTO:
   Número: 000001
   Prefijo: SETT
   Fecha Emisión: 2024-01-13
   Fecha Vencimiento: 2024-01-20
   Moneda: COP

🏢 EMISOR:
   Nombre: AJECOLOMBIA SAS
   NIT: 1193122070
   Dirección: Calle 123 # 45-67
   Ciudad: BOGOTA
   Teléfono: 3001234567
   Email: facturacion@aje.com

👤 CLIENTE:
   Nombre: CLIENTE EJEMPLO SAS
   NIT: 900123456
   Dirección: Carrera 10 # 20-30
   Ciudad: MEDELLIN

💰 TOTALES:
   Subtotal: $1,000,000.00
   Descuentos: $50,000.00
   Impuestos: $180,500.00
   TOTAL: $1,130,500.00

📦 LÍNEAS DE DETALLE (10):
   1. Producto ejemplo 1
      Cantidad: 100 UND
      Precio: $10,000.00
      Descuento: $0.00
      IVA: 19%
      Total Línea: $119,000.00

   ... y 9 líneas más

🔲 CÓDIGO QR:
   CUFE: abc123def456...
   QR Code: 500 caracteres

📋 INFORMACIÓN ADICIONAL:
   Forma de Pago: CONTADO
   Notas: Factura generada automáticamente

🔖 CUSTOM FIELDS (5):
   • LogoBase64: data:image/png;base64,[base64 data: 21756 chars]
   • ResolucionTexto: GENTE RETENEDOR DE IVA - SOMOS AUTORRETENEDORES...
   • ValorLetras: UN MILLÓN CIENTO TREINTA MIL QUINIENTOS PESOS
   • Zona: SUR
   • Ruta: RUTA-01

═══════════════════════════════════════════════════════════════════
```

## 💡 VENTAJAS

- ✅ No necesitas instalar nada
- ✅ Ves TODOS los datos en consola
- ✅ Puedes copiar/pegar el output
- ✅ Funciona en cualquier editor

## 🆚 COMPARACIÓN

| Con C# Dev Kit | Con Console.WriteLine |
|----------------|----------------------|
| Breakpoints visuales | Manual (agregar código) |
| F5, F10, F11 | Ver output en consola |
| Panel de variables | Todo en texto |
| Step debugging | Print debugging |
| **Más potente** | **Más simple** |

## 🎯 MI RECOMENDACIÓN

Si vas a debugear frecuentemente:
→ **Instala C# Dev Kit** (vale la pena)

Si solo necesitas ver datos ocasionalmente:
→ **Usa Console.WriteLine** (más rápido)

## ✅ CONCLUSIÓN

Ambos métodos funcionan perfectamente. Escoge el que prefieras.
