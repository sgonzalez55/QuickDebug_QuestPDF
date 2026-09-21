using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Services.PdfGenerator.Domain.Models;
using QRCoder;

namespace Services.PdfGenerator.Infrastructure.Templates._1193122070;

/// <summary>
/// Plantilla optimizada de factura electrónica para AJECOLOMBIA SAS (NIT 1193122070)
/// </summary>
public class FacturaUbl : IInvoiceTemplate
{
    private static bool _encodingProviderRegistered = false;
    private static readonly object _lock = new object();
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    // Registrar proveedor de codificación para Windows-1252 (UTF-8 fix)
    static FacturaUbl()
    {
        RegisterEncodingProvider();
    }

    private static void RegisterEncodingProvider()
    {
        if (_encodingProviderRegistered) return;

        lock (_lock)
        {
            if (_encodingProviderRegistered) return;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _encodingProviderRegistered = true;
            }
            catch (Exception)
            {
                // Si falla el registro, el encoding fix no funcionará pero el PDF se generará
                _encodingProviderRegistered = false;
            }
        }
    }

    public byte[] GeneratePdf(InvoiceModel model)
    {
        // Validación de entrada
        if (model == null)
            throw new ArgumentNullException(nameof(model), "InvoiceModel cannot be null");

        if (model.Document == null)
            throw new ArgumentException("model.Document cannot be null", nameof(model));

        // Corregir problemas de codificación UTF-8 (safe)
        var valorLetras = SafeFixEncodingIssues(GetCustomField(model, "ValorLetras", ""));

        // Generar códigos QR y cargar logo (safe)
        var qrCodeBytes = SafeGenerateQRCode(model.QR?.QRCode ?? "");
        var logoBytes = SafeLoadLogo(GetCustomField(model, "LogoBase64", ""));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginTop(10);
                page.MarginRight(10);
                page.MarginBottom(10);
                page.MarginLeft(10);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(BrandColors.TextBlack));

                page.Content().Column(column =>
                {
                    // Header compacto
                    ComposeHeader(column, model, qrCodeBytes, logoBytes);

                    // Bloque de información del cliente
                    ComposeClientInfo(column, model);

                    // TABLA 1: PRODUCTOS - CON ALTURA FIJA 90PT
                    ComposeProductTable(column, model);

                    // TABLA 2: TOTALES
                    ComposeTotalsTable(column, model);

                    // Valor en letras
                    column.Item().PaddingTop(3.3f).PaddingBottom(1.7f)
                        .Text($"VALOR EN LETRAS: {valorLetras}").FontSize(7.6f).Bold();

                    // Footer legal
                    ComposeFooter(column, model);
                });
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(ColumnDescriptor column, InvoiceModel model, byte[]? qrCodeBytes, byte[]? logoBytes)
    {
        column.Item().Row(row =>
        {
            // Logo corporativo AJE - ultra compacto
            row.ConstantItem(68).Padding(1.0f).AlignCenter().AlignMiddle()
                .Element(c =>
                {
                    if (logoBytes != null)
                        c.Height(48).Image(logoBytes).FitHeight();
                    else
                        c.Height(48).AlignCenter().AlignMiddle().Text("").FontSize(8);
                });

            // Información del emisor centrada
            row.RelativeItem().PaddingLeft(3.5f).PaddingRight(3.5f).Column(col =>
            {
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Name)).FontSize(6.9f).Bold();
                col.Item().AlignCenter().Text($"NIT. {SafeGetValue(model.Issuer?.TaxId)}").FontSize(5.0f);
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Address)).FontSize(4.6f);
                col.Item().AlignCenter().Text($"Teléfono : {SafeGetValue(model.Issuer?.Phone)}").FontSize(4.6f);

                var resolucionTexto = GetCustomField(model, "ResolucionTexto", "");
                if (!string.IsNullOrEmpty(resolucionTexto))
                {
                    col.Item().AlignCenter().Text(resolucionTexto).FontSize(3.8f).LineHeight(0.93f);
                }
            });

            // Factura y QR
            row.ConstantItem(138).Column(col =>
            {
                col.Item().Padding(1.2f).AlignCenter().Column(inner =>
                {
                    inner.Item().Text("FACTURA ELECTRONICA DE VENTA").FontSize(9.5f).Bold().FontColor(BrandColors.TextBlack);
                    inner.Item().Text($"{SafeGetValue(model.Document?.Prefix)}{SafeGetValue(model.Document?.Number)}").FontSize(8.0f).Bold().FontColor(BrandColors.TextBlack);
                });

                col.Item().PaddingTop(1.0f).Padding(1.0f).AlignCenter().AlignMiddle()
                    .Element(c =>
                    {
                        if (qrCodeBytes != null)
                            c.Width(28, Unit.Millimetre).Height(28, Unit.Millimetre).Image(qrCodeBytes).FitArea();
                        else
                            c.Width(28, Unit.Millimetre).Height(28, Unit.Millimetre).Text("").FontSize(6);
                    });
            });
        });
    }

    private void ComposeClientInfo(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().PaddingTop(0.1f).Row(row =>
        {
            void CellHorizontal(IContainer container, string label, string value) =>
                container.Padding(0.15f).Row(r =>
                {
                    r.AutoItem().Text($"{label} ").FontSize(4.7f).Bold();
                    r.RelativeItem().Text(value).FontSize(4.7f);
                });

            // COLUMNA 1
            row.RelativeItem(1f).Column(col =>
            {
                CellHorizontal(col.Item(), "NOMBRE CLIENTE:", model.Customer?.Name ?? "");
                CellHorizontal(col.Item(), "NOMBRE COMERCIAL:", "");
                CellHorizontal(col.Item(), "NUMERO DOCTO:", model.Customer?.TaxId ?? "");
                CellHorizontal(col.Item(), "DIRECCION DESTINO:", model.Customer?.Address ?? "");
                CellHorizontal(col.Item(), "CONDICION DE PAGO:", model.Additional?.PaymentMethod ?? "");
            });

            // COLUMNA 2
            row.RelativeItem(1f).Column(col =>
            {
                CellHorizontal(col.Item(), "ZONA:", GetCustomField(model, "Zona", ""));
                CellHorizontal(col.Item(), "RUTA:", GetCustomField(model, "Ruta", ""));
                CellHorizontal(col.Item(), "TELEFONO:", "");
                CellHorizontal(col.Item(), "FECHA Y HORA GENERACION:", $"{model.Document?.IssueDate:yyyy-MM-dd} {GetCustomField(model, "HoraGeneracion", "")}");
                CellHorizontal(col.Item(), "NUMERO PEDIDO:", GetCustomField(model, "NumPedido", ""));
            });

            // COLUMNA 3
            row.RelativeItem(1f).Column(col =>
            {
                CellHorizontal(col.Item(), "MEDIO DE PAGO:", model.Additional?.PaymentMethod ?? "");
                CellHorizontal(col.Item(), "FECHA DE VENCIMIENTO:", $"{model.Document?.DueDate:yyyy-MM-dd}");
                CellHorizontal(col.Item(), "NUM. CARGUE:", GetCustomField(model, "NumCargue", ""));
                CellHorizontal(col.Item(), "ORDEN COMPRA:", GetCustomField(model, "OrdenCompra", ""));
                CellHorizontal(col.Item(), "PLACA VEHICULO:", GetCustomField(model, "PlacaVehiculo", ""));
            });
        });
    }

    private void ComposeProductTable(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().Height(90).BorderLeft(0.5f).BorderRight(0.5f).BorderTop(0.5f)
            .BorderColor(BrandColors.TextBlack).Table(table =>
        {
            // Definir anchos de columna
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4.8f);  // DESCRIPCION
                columns.ConstantColumn(21);    // U M
                columns.ConstantColumn(44);    // CODIGO PRODUCTO
                columns.ConstantColumn(44);    // COD. BARRAS
                columns.ConstantColumn(35);    // TIPO VENTA
                columns.ConstantColumn(29);    // CANT UND
                columns.ConstantColumn(33);    // CANT PACAS
                columns.ConstantColumn(53);    // PRECIO ANTES IVA
                columns.ConstantColumn(30);    // DCTO
                columns.ConstantColumn(27);    // IVA%
                columns.ConstantColumn(44);    // IVA
                columns.ConstantColumn(27);    // IBUA
                columns.ConstantColumn(58);    // VALOR TOTAL
            });

            // ENCABEZADOS
            table.Header(header =>
            {
                void HeaderCell(string text) => header.Cell()
                    .Background("#FFFFFF")
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.HeaderPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.HeaderPaddingHorizontal)
                    .AlignCenter().AlignMiddle()
                    .Text(text).FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.HeaderFontSize).Bold()
                    .FontColor(InvoiceTableStyles.TextColor)
                    .LineHeight(1.35f);

                HeaderCell("DESCRIPCION");
                HeaderCell("U M");
                HeaderCell("CODIGO\nPRODUCTO");
                HeaderCell("COD. BARRAS");
                HeaderCell("TIPO\nVENTA");
                HeaderCell("CANT\nUND");
                HeaderCell("CANT\nPACAS");
                HeaderCell("PRECIO ANTES\nIVA");
                HeaderCell("DCTO");
                HeaderCell("IVA%");
                HeaderCell("IVA");
                HeaderCell("IBUA");
                HeaderCell("VALOR TOTAL");
            });

            // DATOS - Solo primera línea
            var firstLine = model.Lines?.FirstOrDefault();
            if (firstLine != null)
            {
                void DataCellLeft(string text) => table.Cell()
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignLeft().AlignMiddle()
                    .Text(text).FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                void DataCellRight(string text) => table.Cell()
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignRight().AlignMiddle()
                    .Text(text).FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                DataCellLeft(firstLine.Description);
                DataCellLeft(firstLine.Unit);
                DataCellRight(GetCustomField(model, "CodigoProducto", ""));
                DataCellRight(GetCustomField(model, "CodigoBarras", ""));
                DataCellRight(GetCustomField(model, "TipoVenta", ""));
                DataCellRight(firstLine.Quantity.ToString("N2"));
                DataCellRight(GetCustomField(model, "CantPacas", "0.00"));
                DataCellRight(firstLine.UnitPrice.ToString("N2"));
                DataCellRight(firstLine.Discount.ToString("N2"));
                DataCellRight(firstLine.TaxRate.ToString("N2"));
                DataCellRight((firstLine.LineTotal * firstLine.TaxRate / 100).ToString("N2"));
                DataCellRight(GetCustomField(model, "Ibua", "0.00"));
                DataCellRight(firstLine.LineTotal.ToString("N2"));
            }
        });
    }

    private void ComposeTotalsTable(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
            .BorderColor(BrandColors.TextBlack).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.10f);  // TOTAL UNIDADES
                columns.RelativeColumn(1.10f);  // TOTAL PACAS
                columns.RelativeColumn(1.60f);  // OPER. GRATUITAS
                columns.RelativeColumn(0.87f);  // DESCUENTO
                columns.RelativeColumn(1.50f);  // VR. BRUTO
                columns.RelativeColumn(0.87f);  // IVA
                columns.RelativeColumn(0.67f);  // IBUA
                columns.RelativeColumn(1.80f);  // TOTAL FACTURA
                columns.RelativeColumn(1.80f);  // TOTAL A PAGAR
            });

            void TotalHeaderCell(string text) => table.Cell()
                .BorderLeft(0.5f).BorderRight(0.5f).BorderTop(0.5f)
                .BorderColor(BrandColors.TextBlack)
                .PaddingVertical(0.3f).PaddingHorizontal(0.3f)
                .AlignCenter().AlignMiddle()
                .Text(text).FontSize(6.5f).Bold()
                .LineHeight(1.0f);

            void TotalValueCell(string text, bool isBold = false)
            {
                var cell = table.Cell()
                    .BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
                    .BorderColor(BrandColors.TextBlack)
                    .PaddingVertical(0.3f).PaddingHorizontal(0.3f)
                    .AlignCenter().AlignMiddle();

                var textStyle = cell.Text(text).FontSize(isBold ? 8.5f : 7.2f);
                if (isBold) textStyle.Bold();
            }

            // ENCABEZADOS
            TotalHeaderCell("TOTAL UNIDADES");
            TotalHeaderCell("TOTAL PACAS");
            TotalHeaderCell("OPER. GRATUITAS");
            TotalHeaderCell("DESCUENTO");
            TotalHeaderCell("VR. BRUTO");
            TotalHeaderCell("IVA");
            TotalHeaderCell("IBUA");
            TotalHeaderCell("TOTAL FACTURA");
            TotalHeaderCell("TOTAL A PAGAR");

            // VALORES
            var totalQuantity = model.Lines?.Sum(l => l.Quantity) ?? 0;
            TotalValueCell(totalQuantity.ToString("N2"));
            TotalValueCell(GetCustomField(model, "TotalPacas", "0.00"));
            TotalValueCell("0.00");
            TotalValueCell(model.Totals?.TotalDiscount.ToString("N2") ?? "0.00");
            TotalValueCell(model.Totals?.Subtotal.ToString("N2") ?? "0.00");
            TotalValueCell(model.Totals?.TotalTax.ToString("N2") ?? "0.00");
            TotalValueCell(GetCustomField(model, "TotalIbua", "0.00"));
            TotalValueCell(model.Totals?.Total.ToString("N2") ?? "0.00", true);
            TotalValueCell(model.Totals?.Total.ToString("N2") ?? "0.00", true);
        });
    }

    private void ComposeFooter(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().PaddingTop(3.5f).Column(col =>
        {
            col.Item().AlignCenter().Text("AJECOLOMBIA SAS solo reconocerá las averías de producto que se generen por concepto de calidad y/o transporte y que sean reportados al momento de la entrega del producto. No se reconocerán devoluciones de producto vencido, ni ninguna otra causal después de la recibido a satisfacción por EL COMPRADOR. Para cualquier COMPRADOR, deberá reportar la novedad dentro de las 48 horas siguientes a la fecha de recibido el producto al correo atencion.clientes.co@ajegroup.com y remitir el soporte fotográfico y número de factura. Transcurrido el término mencionado sin generarse ninguna novedad, se entenderá recibido el producto a satisfacción.").FontSize(6.8f).LineHeight(1.16f);

            col.Item().PaddingTop(4.6f).Text("Notas al Documento:").FontSize(6.8f).Bold();
            col.Item().Text("CPDF FACTURA-UBL").FontSize(5.8f);
            col.Item().Text("Proveedor Tecnológico de Facturación Electrónica generada por software de Sistemas de Información Empresarial S.A.S. NIT 890.319.193-3 Siesa e-Invoicing NIT 890.319.193-3.").FontSize(4.8f).LineHeight(1.16f);

            col.Item().PaddingTop(2.8f).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Row(row =>
                    {
                        row.AutoItem().Text("CUFE: ").FontSize(5.8f).Bold();
                        row.RelativeItem().Text(model.QR?.CUFE ?? "").FontSize(5.8f);
                    });
                });
                r.ConstantItem(198).AlignRight().Text($"FECHA DIAN: {model.Document?.IssueDate:yyyy-MM-dd}").FontSize(5.9f);
            });

            col.Item().PaddingTop(2.9f).Text("FIRMA DIGITAL:").FontSize(6.9f).Bold();
            col.Item().PaddingTop(2.9f).AlignRight().Text("Pag 1 de 1").FontSize(6.9f);
        });
    }

    // Helpers
    private static string SafeGetValue(string? value) => value ?? string.Empty;

    private string GetCustomField(InvoiceModel model, string key, string defaultValue)
    {
        return model.Additional?.CustomFields?.TryGetValue(key, out var value) == true && !string.IsNullOrEmpty(value)
            ? value
            : defaultValue;
    }

    private string SafeFixEncodingIssues(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Solo intentar fix si el provider está registrado
        if (!_encodingProviderRegistered) return text;

        try
        {
            // El XML tiene bytes UTF-8 pero interpretados como Windows-1252
            // Recodificar: tomar como Windows-1252 bytes, reinterpretar como UTF-8
            var win1252 = Encoding.GetEncoding(1252);
            if (win1252 == null) return text;

            var win1252Bytes = win1252.GetBytes(text);
            return Encoding.UTF8.GetString(win1252Bytes);
        }
        catch (Exception)
        {
            // Si falla, devolver texto original sin modificar
            return text;
        }
    }

    private byte[]? SafeGenerateQRCode(string qrUrl)
    {
        if (string.IsNullOrWhiteSpace(qrUrl)) return null;

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(8);
        }
        catch (Exception)
        {
            // Si falla generación de QR, continuar sin QR
            return null;
        }
    }

    private byte[]? SafeLoadLogo(string logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return null;

        try
        {
            // En producción, el logo debería venir como Base64 en CustomFields["LogoBase64"]
            // o como URL HTTP/HTTPS que se descarga

            // Caso 1: Ruta local (solo para desarrollo/testing)
            if (File.Exists(logoUrl))
            {
                return File.ReadAllBytes(logoUrl);
            }

            // Caso 2: URL HTTP/HTTPS (producción) - Usar HttpClient compartido
            if (Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return _httpClient.GetByteArrayAsync(logoUrl).GetAwaiter().GetResult();
            }

            // Caso 3: Base64 encoded
            if (logoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var base64Data = logoUrl.Substring(logoUrl.IndexOf(",") + 1);
                return Convert.FromBase64String(base64Data);
            }

            return null;
        }
        catch (Exception)
        {
            // Si falla carga de logo, continuar sin logo
            return null;
        }
    }

    public TemplateMetadata GetMetadata()
    {
        return new TemplateMetadata
        {
            Nit = "1193122070",
            TemplateName = "factura-ubl",
            ClientName = "AJECOLOMBIA SAS",
            Version = "2.0",
            LastUpdated = DateTime.UtcNow
        };
    }
}

// Paleta de colores corporativa AJE
static class BrandColors
{
    public static string AjeGreen = "#00B4A5"; // Verde agua AJE
    public static string TableHeaderBg = "#E6F7F5"; // Verde menta claro
    public static string BorderGray = "#CCCCCC"; // Gris medio para bordes
    public static string BorderDark = "#666666"; // Gris oscuro
    public static string TextBlack = "#000000"; // Negro sólido
}

// Estilos específicos para la tabla de items
static class InvoiceTableStyles
{
    public static string HeaderBackground = "#E6F7F5";
    public static string CellBackground = "#FFFFFF";
    public static string BorderColor = "#000000";
    public static string TextColor = "#000000";

    public static float HeaderFontSize = 5.5f;
    public static float DataFontSize = 5.5f;
    public static string FontFamily = "Arial";

    public static float BorderWidth = 0.5f;

    public static float HeaderPaddingVertical = 0.9f;
    public static float HeaderPaddingHorizontal = 1.1f;
    public static float DataPaddingVertical = 0.75f;
    public static float DataPaddingHorizontal = 1.1f;
}
