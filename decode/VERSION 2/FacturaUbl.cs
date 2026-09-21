using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Services.PdfGenerator.Domain.Models;
using QRCoder;
using QuestPDF.Companion;
using System.Globalization;
using ZXing;

namespace Services.PdfGenerator.Infrastructure.Templates._830081407;

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
    const int itemsPorPagina = 12;

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

        // Convertir a DtoUbl si no lo es (usar MapToCustomDto si viene InvoiceModel base)
        var dto = model as DtoUbl ?? (DtoUbl)MapToCustomDto(model);

        // Usar propiedades tipadas del DTO personalizado
        var valorLetras = PDF_Utils.NumeroALetrasCOP(model.Lines?.Sum(l => l.LineTotal) ?? 0);

        // Generar códigos QR y cargar logo usando propiedades tipadas del DTO
        var qrCodeBytes = SafeGenerateQRCode(dto.QR?.QRCode ?? "");
        var logoBytes = SafeLoadLogo(dto.LogoBase64);
        var pages = dto.Lines?
                    .Select((item, index) => new { item, index })
                    .GroupBy(x => x.index / 12)
                    .Select(g => g.Select(x => x.item).ToList())
                    .ToList();
        var ListaPrueba = model.Lines?.Select(cu => cu.Cant_Und).ToList();
        var listaPrueba = CreaListaPaginadaCant_Und(ListaPrueba ?? new List<string>());

        return Document.Create(container =>
        {
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var productosPagina = pages[pageIndex];
                container.Page(page =>
                {
                    page.Size(612, 391);
                    page.MarginTop(10);
                    page.MarginRight(10);
                    page.MarginBottom(10);
                    page.MarginLeft(10);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Liberation Sans").FontColor(BrandColors.TextBlack));

                    page.Header().Column(columnHeader =>
                    {
                        // Header compacto - usa DtoUbl con propiedades tipadas
                        ComposeHeader(columnHeader, dto, qrCodeBytes, logoBytes);

                        // Bloque de información del cliente - usa DtoUbl con propiedades tipadas
                        ComposeClientInfo(columnHeader, dto);
                    });

                    page.Content().Column(column =>
                    {
                        // TABLA 1: PRODUCTOS - CON ALTURA FIJA 90PT
                        ComposeProductTable(column, productosPagina, dto);
                    });

                    page.Footer().Column(footerColumn =>
                    {
                        // TABLA 2: TOTALES
                        ComposeTotalsTable(footerColumn, dto, productosPagina);

                        // Valor en letras - usa propiedad tipada del DTO
                        footerColumn.Item().PaddingTop(3.3f).PaddingBottom(1.7f)
                            .Text($"VALOR EN LETRAS: {valorLetras:N0} M/CTE*******").FontSize(7.6f);

                        // Footer legal
                        ComposeFooter(footerColumn, dto);
                    });
                });
            }
            // container.Page(page =>
            // {
            //     page.Size(612, 399);
            //     page.MarginTop(10);
            //     page.MarginRight(10);
            //     page.MarginBottom(10);
            //     page.MarginLeft(10);
            //     page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(BrandColors.TextBlack));

            //     page.Header().Column(columnHeader =>
            //     {
            //         // Header compacto - usa DtoUbl con propiedades tipadas
            //         ComposeHeader(columnHeader, dto, qrCodeBytes, logoBytes);

            //         // Bloque de información del cliente - usa DtoUbl con propiedades tipadas
            //         ComposeClientInfo(columnHeader, dto);
            //     });

            //     page.Content().Column(column =>
            //     {
            //         for (int i = 0; i < pages.Count; i++)
            //         {
            //             if (i > 0)
            //                 column.Item().PageBreak();

            //             // TABLA 1: PRODUCTOS - CON ALTURA FIJA 90PT
            //             ComposeProductTable(column, pages[i], dto);
            //         }
            //     });

            //     page.Footer().Column(footerColumn =>
            //     {
            //         // TABLA 2: TOTALES
            //         ComposeTotalsTable(footerColumn, dto);

            //         // Valor en letras - usa propiedad tipada del DTO
            //         footerColumn.Item().PaddingTop(3.3f).PaddingBottom(1.7f)
            //             .Text($"VALOR EN LETRAS: {valorLetras} *******").FontSize(7.6f);

            //         // Footer legal
            //         ComposeFooter(footerColumn, dto);
            //     });
            // });
        }).GeneratePdf();
    }

    public void PreviewOnQuestPDFCompanion(InvoiceModel model)
    {
        // Validación de entrada
        if (model == null)
            throw new ArgumentNullException(nameof(model), "InvoiceModel cannot be null");

        if (model.Document == null)
            throw new ArgumentException("model.Document cannot be null", nameof(model));

        // Convertir a DtoUbl si no lo es (usar MapToCustomDto si viene InvoiceModel base)
        var dto = model as DtoUbl ?? (DtoUbl)MapToCustomDto(model);

        // Usar propiedades tipadas del DTO personalizado
        var valorLetras = PDF_Utils.NumeroALetrasCOP(model.Lines?.Sum(l => l.LineTotal) ?? 0);

        // Generar códigos QR y cargar logo usando propiedades tipadas del DTO
        var qrCodeBytes = SafeGenerateQRCode(dto.QR?.QRCode ?? "");
        var logoBytes = SafeLoadLogo(dto.LogoBase64);
        var pages = dto.Lines?
                    .Select((item, index) => new { item, index })
                    .GroupBy(x => x.index / 12)
                    .Select(g => g.Select(x => x.item).ToList())
                    .ToList();
        var ListaPrueba = model.Lines?.Select(cu => cu.Cant_Und).ToList();
        var listaPrueba = CreaListaPaginadaCant_Und(ListaPrueba ?? new List<string>());

        Document.Create(container =>
        {
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var productosPagina = pages[pageIndex];
                container.Page(page =>
                {
                    page.Size(612, 391);
                    page.MarginTop(10);
                    page.MarginRight(10);
                    page.MarginBottom(10);
                    page.MarginLeft(10);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Liberation Sans").FontColor(BrandColors.TextBlack));

                    page.Header().Column(columnHeader =>
                    {
                        // Header compacto - usa DtoUbl con propiedades tipadas
                        ComposeHeader(columnHeader, dto, qrCodeBytes, logoBytes);

                        // Bloque de información del cliente - usa DtoUbl con propiedades tipadas
                        ComposeClientInfo(columnHeader, dto);
                    });

                    page.Content().Column(column =>
                    {
                        // TABLA 1: PRODUCTOS - CON ALTURA FIJA 90PT
                        ComposeProductTable(column, productosPagina, dto);
                    });

                    page.Footer().Column(footerColumn =>
                    {
                        // TABLA 2: TOTALES
                        ComposeTotalsTable(footerColumn, dto, productosPagina);

                        // Valor en letras - usa propiedad tipada del DTO
                        footerColumn.Item().PaddingTop(3.3f).PaddingBottom(1.7f)
                            .Text($"VALOR EN LETRAS: {valorLetras:N0} M/CTE*******").FontSize(7.6f);

                        // Footer legal
                        ComposeFooter(footerColumn, dto);
                    });
                });
            }
            // container.Page(page =>
            // {
            //     page.Size(612, 399);
            //     page.MarginTop(10);
            //     page.MarginRight(10);
            //     page.MarginBottom(10);
            //     page.MarginLeft(10);
            //     page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(BrandColors.TextBlack));

            //     page.Header().Column(columnHeader =>
            //     {
            //         // Header compacto - usa DtoUbl con propiedades tipadas
            //         ComposeHeader(columnHeader, dto, qrCodeBytes, logoBytes);

            //         // Bloque de información del cliente - usa DtoUbl con propiedades tipadas
            //         ComposeClientInfo(columnHeader, dto);
            //     });

            //     page.Content().Column(column =>
            //     {
            //         for (int i = 0; i < pages.Count; i++)
            //         {
            //             if (i > 0)
            //                 column.Item().PageBreak();

            //             // TABLA 1: PRODUCTOS - CON ALTURA FIJA 90PT
            //             ComposeProductTable(column, pages[i], dto);
            //         }
            //     });

            //     page.Footer().Column(footerColumn =>
            //     {
            //         // TABLA 2: TOTALES
            //         ComposeTotalsTable(footerColumn, dto);

            //         // Valor en letras - usa propiedad tipada del DTO
            //         footerColumn.Item().PaddingTop(3.3f).PaddingBottom(1.7f)
            //             .Text($"VALOR EN LETRAS: {valorLetras} *******").FontSize(7.6f);

            //         // Footer legal
            //         ComposeFooter(footerColumn, dto);
            //     });
            // });
        }).ShowInCompanion();
    }

    private void ComposeHeader(ColumnDescriptor column, InvoiceModel model, byte[]? qrCodeBytes, byte[]? logoBytes)
    {
        // Obtener DtoUbl para acceder a propiedades tipadas
        var dto = model as DtoUbl;

        column.Item().Row(row =>
        {
            // Logo corporativo AJE - ultra compacto
            row.ConstantItem(68).PaddingTop(-15).AlignCenter().AlignMiddle()
                .Element(c =>
                {
                    if (logoBytes != null)
                        c.Height(48).Image(logoBytes).FitHeight();
                    else
                        c.Height(48).AlignCenter().AlignMiddle().Text("").FontSize(8);
                });

            // Información del emisor centrada
            row.RelativeItem().PaddingTop(4).PaddingHorizontal(2f).Column(col =>
            {
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Name)).FontSize(7f).Bold();
                col.Item().AlignCenter().Text($"NIT. {SafeGetValue(model.Issuer?.TaxId)}").FontSize(7f);
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Address)).FontSize(7f);
                col.Item().AlignCenter().Text($"Teléfono: {SafeGetValue(model.Issuer?.Phone)} Fax:").FontSize(7f);

                // Usar propiedad tipada del DTO si está disponible
                var resolucionTexto = dto?.ResolucionTexto ?? GetCustomField(model, "ResolucionTexto", "");
                col.Item().AlignCenter().Text(txt =>
                {
                    txt.Span("AGENTE RETENEDOR DE IVA -  SOMOS AUTORRETENEDORES  DE RENTA SEGUN RESOLUCIÓN No 13834 del 20 de Noviembre de 2007 - GRAN CONTRIBUYENTE RESOLUCIÓN No  000200 del 27 de Diciembre de 2024").FontSize(7f);
                });
                col.Item().AlignCenter().Text(txt =>
                {
                    txt.Span($"Autorización Numeración de Facturación No. {model.Folio?.Aprobacion_num} Numeración: AUTORIZADA Rango desde: {model.Folio?.Prefijo}{model.Folio?.Inicio_num} hasta: {model.Folio?.Prefijo}{model.Folio?.Fin_num} Vigencia desde: {model.Folio?.Aprobacion_dt:dd/MM/yyyy} hasta: {model.Folio?.Expiracion_dt:dd/MM/yyyy} - 24 Meses").FontSize(7f);
                });

            });

            // Factura y QR
            row.ConstantItem(160).Column(col =>
            {
                col.Item().PaddingHorizontal(1.2f).AlignCenter().Column(inner =>
                {
                    inner.Item().AlignCenter().AlignMiddle().Text("FACTURA ELECTRONICA DE VENTA").FontSize(8f).Bold().FontColor(BrandColors.TextBlack);
                    inner.Item().AlignCenter().AlignMiddle().Text($"{SafeGetValue(model.Document?.Prefix)}{SafeGetValue(model.Document?.Number)}").FontSize(8.0f).Bold().FontColor(BrandColors.TextBlack);
                });

                col.Item().PaddingTop(0f).Padding(0f).AlignCenter().AlignMiddle()
                    .Element(c =>
                    {
                        if (qrCodeBytes != null)
                            c.Width(27, Unit.Millimetre).Height(24, Unit.Millimetre).Image(qrCodeBytes).FitArea();
                        else
                            c.Width(27, Unit.Millimetre).Height(24, Unit.Millimetre).Text("").FontSize(6);
                    });
            });
        });
    }

    private void ComposeClientInfo(ColumnDescriptor column, InvoiceModel model)
    {
        // Obtener DtoUbl para acceder a propiedades tipadas
        var dto = model as DtoUbl;

        // string xmlTime = model.Document?.HoraEmision;
        // var hora = DateTimeOffset.Parse(xmlTime);
        // string horaFormateada = hora
        //     .ToString("hh:mm:ss tt", new CultureInfo("es-CO"))
        //     .Replace("AM", "a. m.")
        //     .Replace("PM", "p. m.");

        column.Item().PaddingTop(-5).Row(row =>
        {
            void CellHorizontal(IContainer container, string label, string value) =>
                container.Padding(0.15f).Row(r =>
                {
                    r.AutoItem().Text($"{label} ").FontSize(7f).Bold();
                    r.RelativeItem().Text(value).FontSize(7f);
                });

            // COLUMNA 1
            row.AutoItem().Column(col =>
            {
                col.Item().AlignMiddle().Text("NOMBRE CLIENTE: ").FontSize(6f).Bold();
                col.Item().AlignMiddle().Text("NOMBRE COMERCIAL: ").FontSize(6f).Bold();
                col.Item().AlignMiddle().Text("NUMERO DOCTO: ").FontSize(6f).Bold();
                col.Item().AlignMiddle().Text("DIRECCION DESTINO: ").FontSize(6f).Bold();
                col.Item().AlignMiddle().Text("CONDICION DE PAGO: ").FontSize(6f).Bold();
            });
            row.RelativeItem(2.5f).PaddingLeft(4).Column(col =>
            {
                col.Item().AlignMiddle().Text(model.Customer?.Name ?? "").FontSize(6f);
                col.Item().AlignMiddle().Text(model.Customer?.Name_Comercial).FontSize(6f);
                col.Item().AlignMiddle().Text(model.Customer?.TaxId ?? "").FontSize(6f);
                col.Item().AlignMiddle().Text(ConDireccionDestino(model)).FontSize(6f);
                col.Item().AlignMiddle().Text(dto?.CondicionPago ?? model.Additional?.PaymentCondition ?? "").FontSize(6f);
            });

            // COLUMNA 2 - Usar propiedades tipadas del DTO
            row.AutoItem().Column(col =>
            {
                col.Item().Text("ZONA: ").FontSize(6f).Bold();
                col.Item().Text("RUTA: ").FontSize(6f).Bold();
                col.Item().Text("TELEFONO: ").FontSize(6f).Bold();
                col.Item().Text("FECHA Y HORA GENERACION: ").FontSize(6f).Bold();
                col.Item().Text("NUMERO PEDIDO: ").FontSize(6f).Bold();
            });
            row.RelativeItem(1.5f).PaddingLeft(4).Column(col =>
            {
                string FechaGeneracion = string.IsNullOrEmpty(model.Document?.IssueDate.ToString()) ? string.Empty : model.Document?.IssueDate.ToString("dd/MM/yyyy");
                var FormatedHora = DateTimeOffset.TryParse(model.Document?.HoraEmision, out var hora) ? hora.ToString("hh:mm:ss tt"): "";
                string HoraGeneracion = string.IsNullOrEmpty(model.Document?.HoraEmision) ? string.Empty : FormatedHora;
                // Usar propiedades tipadas del DTO si está disponible, sino fallback a CustomFields
                col.Item().Text(dto?.Zona ?? string.Empty).FontSize(6f);
                col.Item().Text(dto?.Ruta ?? GetCustomField(model, "Ruta", "")).FontSize(6f);
                col.Item().Text(model.InformacionAdditional?.Telefono_Receptor).FontSize(6f);
                col.Item().Text($"{FechaGeneracion} {HoraGeneracion}").FontSize(6f);
                col.Item().Text(dto?.NumPedido ?? GetCustomField(model, "NumPedido", "")).FontSize(6f);
            });

            // COLUMNA 3 - Usar propiedades tipadas del DTO
            row.AutoItem().Column(col =>
            {
                col.Item().Text("MEDIO DE PAGO: ").FontSize(6f).Bold();
                col.Item().Text("FECHA DE VENCIMIENTO: ").FontSize(6f).Bold();
                col.Item().Text("NUM. CARGUE: ").FontSize(6f).Bold();
                col.Item().Text("ORDEN COMPRA: ").FontSize(6f).Bold();
                col.Item().Text("PLACA VEHICULO: ").FontSize(6f).Bold();
            });
            row.RelativeItem(1.5f).PaddingLeft(4).Column(col =>
            {
                 string FechaVencimiento = string.IsNullOrEmpty(model.Document?.DueDate.ToString()) ? string.Empty : model.Document?.DueDate.ToString("dd/MM/yyyy");
                var FormatedHora = DateTimeOffset.TryParse(model.Document?.HoraEmision, out var hora) ? hora.ToString("hh:mm:ss tt"): "";
                string HoraVencimiento = string.IsNullOrEmpty(model.Document?.HoraEmision) ? string.Empty : FormatedHora;
                col.Item().Text(model.Additional?.PaymentMethod).FontSize(6f);
                col.Item().Text($"{FechaVencimiento} {HoraVencimiento}").FontSize(6f);
                // Usar propiedades tipadas del DTO si está disponible
                col.Item().Text(dto?.NumCargue ?? model.InformacionAdditional?.NumCargue ?? "").FontSize(6f);
                col.Item().Text(dto?.OrdenCompra ?? model.InformacionAdditional?.OC ?? "").FontSize(6f);
                col.Item().Text(dto?.PlacaVehiculo ?? GetCustomField(model, "Placa", "")).FontSize(6f);
            });
        });
    }

    private void ComposeProductTable(ColumnDescriptor column, List<LineItemInfo> lines, InvoiceModel model)
    {
        column.Item().PaddingTop(3).BorderLeft(0.5f).BorderRight(0.5f).BorderTop(0.5f).BorderColor(BrandColors.TextBlack).AlignMiddle().Table(table =>
        {
            // Definir anchos de columna
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4.8f);  // DESCRIPCION
                columns.ConstantColumn(22);    // U M
                columns.ConstantColumn(44);    // CODIGO PRODUCTO
                columns.ConstantColumn(44);    // COD. BARRAS
                columns.ConstantColumn(36);    // TIPO VENTA
                columns.ConstantColumn(30);    // CANT UND
                columns.ConstantColumn(33);    // CANT PACAS
                columns.ConstantColumn(53);    // PRECIO ANTES IVA
                columns.ConstantColumn(38);    // DCTO
                columns.ConstantColumn(27);    // IVA%
                columns.ConstantColumn(44);    // IVA
                columns.ConstantColumn(27);    // IBUA
                columns.ConstantColumn(50);    // VALOR TOTAL
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
                    .Text(text)
                    .FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.HeaderFontSize)
                    .Bold()
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

            // DATOS (SOLO LAS LÍNEAS DE ESTA PÁGINA)
            foreach (var line in lines)
            {
                if (line == null)
                    continue;

                void DataCellLeft(string text) => table.Cell()
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignLeft()
                    .AlignMiddle()
                    .Text(text ?? string.Empty)
                    .FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                void DataCellRight(string text) => table.Cell()
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(text ?? string.Empty)
                    .FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                void DataCellCenter(string text) => table.Cell()
                    .Border(InvoiceTableStyles.BorderWidth)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(text ?? string.Empty)
                    .FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                // DataCellLeft(line.Description);
                // DataCellCenter(line.Unit_Medition);
                // DataCellLeft(line.Tiquete);
                // DataCellCenter(line.Codigo_cd);
                // DataCellCenter(line.TVenta);
                // DataCellCenter(line.Cant_Und.ToString());
                // DataCellCenter(line.Quantity.ToString("N1"));
                // DataCellRight($"$ {line.PrecioAntesIVA:N0}");
                // DataCellRight($"$ {line.ValorDescuento.ToString("N0")}");
                // DataCellCenter(line.TaxRate.ToString("N2"));
                // DataCellRight($"$ {line.TaxValor.ToString("N0")}");
                // DataCellCenter(line.Ibua.ToString("N0"));
                // DataCellRight($"$ {line.LineTotal.ToString("N0")}");

                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignLeft().AlignMiddle().Text(line.PrecioAntesIVA == 0 ? $"{line.Description.Trim()} *" : (line.Description.Trim() ?? string.Empty)).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                //table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignLeft().AlignMiddle().Text(line.Description.Trim() ?? string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text((AplicaFactor(model, line)) && !string.IsNullOrWhiteSpace(line.Unit_Medition) ? line.Unit_Medition.Trim() : "UND").FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignLeft().AlignMiddle().Text(line.Tiquete.Trim() ?? string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(line.Codigo_cd.Trim() ?? string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(line.PrecioAntesIVA == 0 ? "Bonifi" : (line.TVenta.Trim() ?? string.Empty)).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(AplicaFactor(model, line) ? (line.Quantity * FactorDe(model, line)).ToString("N1").Trim() : line.Quantity.ToString("N1").Trim()).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(AplicaFactor(model, line) ? line.Quantity.ToString("N1").Trim() : string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignRight().AlignMiddle().Text(line.PrecioAntesIVA.ToString("N0").Trim()).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignRight().AlignMiddle().Text(line.ValorDescuento.ToString("N0").Trim()).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(line.TaxRate.ToString().Trim() ?? string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignRight().AlignMiddle().Text(line.TaxValor.ToString("N0").Trim() ).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignCenter().AlignMiddle().Text(line.Ibua.ToString("N0").Trim() ?? string.Empty).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                //table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignRight().AlignMiddle().Text(line.LineTotal.ToString("N0").Trim() ).FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
                table.Cell().Border(InvoiceTableStyles.BorderWidth).BorderColor(InvoiceTableStyles.BorderColor).PaddingVertical(0.75f).PaddingHorizontal(1.1f).AlignRight().AlignMiddle().Text(line.PrecioAntesIVA == 0 ? "$ 0" : $"$ {line.LineTotal.ToString("N0")}").FontFamily(InvoiceTableStyles.FontFamily).FontSize(InvoiceTableStyles.DataFontSize).FontColor(InvoiceTableStyles.TextColor);
            }
        });

        column.Item().ExtendVertical().Row(row =>
        {
            row.RelativeItem(4.8f).BorderLeft(0.5f).BorderRight(0.5f).BorderHorizontal(0.5f);// DESCRIPCION
            row.ConstantItem(22).BorderRight(0.5f).BorderHorizontal(0.5f);// U M
            row.ConstantItem(44).BorderRight(0.5f).BorderHorizontal(0.5f);// CODIGO PRODUCTO
            row.ConstantItem(44).BorderRight(0.5f).BorderHorizontal(0.5f);// COD. BARRAS
            row.ConstantItem(36).BorderRight(0.5f).BorderHorizontal(0.5f);// TIPO VENTA
            row.ConstantItem(30).BorderRight(0.5f).BorderHorizontal(0.5f);// CANT UND
            row.ConstantItem(33).BorderRight(0.5f).BorderHorizontal(0.5f);// CANT PACAS
            row.ConstantItem(53).BorderRight(0.5f).BorderHorizontal(0.5f);// PRECIO ANTES IVA
            row.ConstantItem(38).BorderRight(0.5f).BorderHorizontal(0.5f);// DCTO
            row.ConstantItem(27).BorderRight(0.5f).BorderHorizontal(0.5f);// IVA%
            row.ConstantItem(44).BorderRight(0.5f).BorderHorizontal(0.5f);// IVA
            row.ConstantItem(27).BorderRight(0.5f).BorderHorizontal(0.5f);// IBUA
            row.ConstantItem(50).BorderRight(0.5f).BorderHorizontal(0.5f);// VALOR TOTAL
        });
    }

    private void ComposeTotalsTable(ColumnDescriptor column, InvoiceModel model, List<LineItemInfo> productosPagina)
    {
        column.Item().Repeat().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
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
            decimal TotalItems = productosPagina.Sum(x => AplicaFactor(model, x) ? x.Quantity * FactorDe(model, x) : x.Quantity);
            decimal TotalPacas = productosPagina.Where(x => AplicaFactor(model, x)).Sum(x => x.Quantity);
            var totalDescuento = model.Lines?.Sum(l => l.ValorDescuento) ?? 0;
            TotalValueCell(TotalItems.ToString("N0"));
            TotalValueCell(TotalPacas.ToString("N1"));
            TotalValueCell("0.00");
            TotalValueCell($"$ {totalDescuento:N0}");
            TotalValueCell($"$ {model.Document?.ValorBruto.ToString("N0")}");
            TotalValueCell($"$ {model.Document?.TotalIVA.ToString("N0") ?? "0.00"}");
            TotalValueCell($"$ {model.Document?.Total_ibua.ToString("N0") ?? "0.00"}");
            TotalValueCell($"$ {model.Document?.Total_am.ToString("N0") ?? "0.00"}");
            TotalValueCell($"$ {model.Document?.Total_am.ToString("N0") ?? "0.00"}");
        });
    }

    private void ComposeFooter(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().PaddingTop(3.5f).Column(col =>
        {
            col.Item().AlignCenter().Text("AJECOLOMBIA SAS solo reconocerá las averías de producto que se generen por concepto de calidad y/o transporte y que sean reportados al momento de la entrega del producto. No se reconocerán devoluciones de producto vencido, ni ninguna otra causal después de la recibido a satisfacción por EL COMPRADOR. Para cualquier COMPRADOR, deberá reportar la novedad dentro de las 48 horas siguientes a la fecha de recibido el producto al correo atencion.clientes.co@ajegroup.com y remitir el soporte fotográfico y número de factura. Transcurrido el término mencionado sin generarse ninguna novedad, se entenderá recibido el producto a satisfacción.").FontSize(6.8f).LineHeight(1.16f);

            col.Item().PaddingTop(4.6f).AlignMiddle().Row(row =>
            {
                row.AutoItem().AlignMiddle().Text("Notas al Documento:").FontSize(6.8f).Bold();
                row.RelativeItem().AlignMiddle().Text(model.Document?.Comentario_ds).FontSize(5.8f);
            });
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
                //r.ConstantItem(198).AlignRight().Text($"FECHA DIAN: {model.Document?.IssueDate:yyyy-MM-dd}").FontSize(5.9f);
            });

            col.Item().PaddingTop(2.9f).Text(text =>
            {
                text.Span($"FIRMA DIGITAL: ").FontSize(6.9f).Bold();
                text.Span($"{model.Document?.FirmaDigital}").FontSize(6.9f);
            });
            col.Item().PaddingTop(2.9f).AlignRight().Text(txt =>
            {
                txt.Span("Pag ").FontSize(6.9f);
                txt.CurrentPageNumber().FontSize(6.9f);
                txt.Span(" de ").FontSize(6.9f);
                txt.TotalPages().FontSize(6.9f);
            });
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
            Nit = "830081407",  
            TemplateName = "factura-ubl",
            ClientName = "AJECOLOMBIA SAS",
            Version = "2.0",
            LastUpdated = DateTime.UtcNow
        };
    }

    public List<int> CreaListaPaginadaCant_Und(List<string> ListaCantUnd)
    {
        List<int> LstSumaPagina = new List<int>();
        int count = 0;

        for (var i = 0; i < ListaCantUnd.Count(); i++)
        {
            count += string.IsNullOrEmpty(ListaCantUnd[i]) ? 0 : int.Parse(ListaCantUnd[i].Replace(".", ""));
            if ((i + 1) % 12 == 0)
            {
                LstSumaPagina.Add(count);
                count = 0;
            }

        }

        if (count > 0)
        {
            LstSumaPagina.Add(count);
        }

        return LstSumaPagina;
    }

    /// <summary>
    /// Mapea el InvoiceModel genérico a un DtoUbl con campos personalizados de AJECOLOMBIA.
    /// Este método extrae los valores del diccionario CustomFields y los asigna a propiedades tipadas.
    /// Se validan cambios DFMP-16062026
    /// </summary>
    /// <param name="baseModel">Modelo base parseado del XML</param>
    /// <returns>DtoUbl con propiedades personalizadas mapeadas</returns>
    public InvoiceModel MapToCustomDto(InvoiceModel baseModel)
    {
        var factores = ResolveFactorEmp(baseModel.Lines, baseModel.Additional);
        var condicionPago = ResolveCondicionPago(baseModel);

        if (baseModel is DtoUbl existingDto)
        {
            existingDto.Factores = factores;
            AgruparPorSku(existingDto);
            return existingDto;
        }
        
        var dto = new DtoUbl
        {
            Folio = baseModel.Folio,
            Document = baseModel.Document,
            Issuer = baseModel.Issuer,
            Customer = baseModel.Customer,
            Lines = baseModel.Lines,
            Totals = baseModel.Totals,
            QR = baseModel.QR,
            Additional = baseModel.Additional,
            InformacionAdditional = baseModel.InformacionAdditional,
            
            // Para Zona, primero intenta la propiedad directa; si falla, busca en CustomFields
            Zona = !string.IsNullOrEmpty(baseModel.InformacionAdditional?.Zona) ? baseModel.InformacionAdditional.Zona : GetCustomFieldStatic(baseModel, "f_20_00007591_G502_1_ZONA")?? string.Empty,
            // Similar para otros...
            Ruta = GetCustomFieldStatic(baseModel, "f_20_00007592_G502_1_RUTA"),
            PlacaVehiculo = GetCustomFieldStatic(baseModel, "f_vehiculo_cargue"),
            NumCargue = baseModel.InformacionAdditional?.NumCargue ?? GetCustomFieldStatic(baseModel, "f_cargue"),
            OrdenCompra = GetCustomFieldStatic(baseModel, "f_orden_compra")
                      ?? baseModel.InformacionAdditional?.OC
                      ?? GetCustomFieldStatic(baseModel, "OrdenCompra"),
            NumPedido = baseModel.InformacionAdditional?.NumPedido ?? GetCustomFieldStatic(baseModel, "NumPedido"),
            ResolucionTexto = GetCustomFieldStatic(baseModel, "ResolucionTexto"),
            LogoBase64 = GetCustomFieldStatic(baseModel, "LogoBase64"),
            ValorLetras = PDF_Utils.NumeroALetrasCOP(baseModel.Document?.Total_am ?? 0),
            CondicionPago = condicionPago,
            Factores = factores
        };
        
        // Asegurar que el modelo base también tenga el valor en AdditionalInformation
        // por si alguna parte usa model.InformacionAdditional directamente.
        if (string.IsNullOrEmpty(dto.InformacionAdditional.Zona) && !string.IsNullOrEmpty(dto.Zona))
        {
            dto.InformacionAdditional.Zona = dto.Zona;
        }

        AgruparPorSku(dto);

        return dto;
    }

    /// <summary>
    /// Agrupa líneas con el mismo cac:Item/cac:SellersItemIdentification/cbc:ID (Tiquete).
    /// El primer ítem del grupo es la plantilla (descripción, código, UM, % IVA, factor);
    /// se suman cantidades e importes. Líneas sin Tiquete quedan individuales
    /// (nunca se fusionan entre sí).
    /// </summary>
    private static void AgruparPorSku(DtoUbl dto)
    {
        if (dto.Lines == null || dto.Lines.Count < 2) return;

        var merged = new List<LineItemInfo>();
        var mergedFactores = new Dictionary<LineItemInfo, FactorLineInfo>();
        var bySku = new Dictionary<string, LineItemInfo>();

        foreach (var line in dto.Lines)
        {
            var key = string.IsNullOrWhiteSpace(line.Tiquete)
                ? $"__ind_{line.LineNumber}"
                : line.Tiquete;

            if (bySku.TryGetValue(key, out var target))
            {
                target.Quantity += line.Quantity;
                target.PrecioAntesIVA += line.PrecioAntesIVA;
                target.ValorDescuento += line.ValorDescuento;
                target.Discount += line.Discount;
                target.TaxValor += line.TaxValor;
                target.Ibua += line.Ibua;
                target.LineTotal += line.LineTotal;
                continue;
            }

            var copy = CopiaLinea(line);
            merged.Add(copy);
            bySku[key] = copy;

            if (dto.Factores != null && dto.Factores.TryGetValue(line, out var factor))
                mergedFactores[copy] = factor;
        }

        dto.Lines = merged;
        dto.Factores = mergedFactores;
    }

    /// <summary>
    /// Copia superficial de una línea (primera aparición = plantilla del grupo).
    /// </summary>
    private static LineItemInfo CopiaLinea(LineItemInfo line)
    {
        return new LineItemInfo
        {
            LineNumber = line.LineNumber,
            Cant_Und = line.Cant_Und,
            Description = line.Description,
            Tiquete = line.Tiquete,
            Codigo_cd = line.Codigo_cd,
            TVenta = line.TVenta,
            Quantity = line.Quantity,
            Unit_Medition = line.Unit_Medition,
            Unit = line.Unit,
            UnitPrice = line.UnitPrice,
            PrecioAntesIVA = line.PrecioAntesIVA,
            ValorDescuento = line.ValorDescuento,
            Discount = line.Discount,
            TaxRate = line.TaxRate,
            TaxValor = line.TaxValor,
            LineTotal = line.LineTotal,
            Ibua = line.Ibua
        };
    }

    /// <summary>
    /// Factor de empaque de una línea resuelto en el DTO; 0 si no aplica.
    /// </summary>
    private static decimal FactorDe(InvoiceModel model, LineItemInfo line)
    {
        var dto = model as DtoUbl;
        return dto != null && dto.Factores != null && dto.Factores.TryGetValue(line, out var f)
            ? f.FactorEmp
            : 0m;
    }

    /// <summary>
    /// ¿Aplica la lógica de pacas a una línea? Con el formato por ítem el factor es
    /// "unidades por paca": va >= 2 (0.0000 = sin pacas). No depende de la cantidad.
    /// </summary>
    private static bool AplicaFactor(InvoiceModel model, LineItemInfo line)
    {
        return line.Quantity > 0 && FactorDe(model, line) >= 2m;
    }

    /// <summary>
    /// Direccion destino: dirección del receptor + f_direccion2_suc (si viene) separada por un espacio.
    /// </summary>
    private static string ConDireccionDestino(InvoiceModel model)
    {
        var baseAddr = model.Customer?.Address ?? string.Empty;
        var extra = GetCustomFieldStatic(model, "f_direccion2_suc").Trim();
        return string.IsNullOrEmpty(extra) ? baseAddr : $"{baseAddr} {extra}";
    }

    /// <summary>
    /// Helper estático para extraer CustomField del modelo base.
    /// </summary>
    private static string GetCustomFieldStatic(InvoiceModel model, string key)
    {
        return model.Additional?.CustomFields?.TryGetValue(key, out var value) == true && !string.IsNullOrEmpty(value)
            ? value
            : string.Empty;
    }

    /// <summary>
    /// Condicion de pago segun cac:PaymentMeans/cbc:ID del ERP (f_condicion_pago: 1=CONTADO, 2=CREDITO).
    /// OJO: el catalogo DIAN cbc:PaymentMeansCode es el MEDIO de pago, no la condicion.
    /// Para credito se anexan los dias: (cbc:PaymentDueDate - cbc:IssueDate).
    /// Tambien deja el texto en Additional.PaymentCondition para los render.
    /// </summary>
    private static string ResolveCondicionPago(InvoiceModel baseModel)
    {
        var condicion = GetCustomFieldStatic(baseModel, "f_condicion_pago");
        var texto = condicion == "1" ? "CONTADO" : condicion == "2" ? DiasCredito(baseModel) : string.Empty;

        if (baseModel.Additional != null)
            baseModel.Additional.PaymentCondition = texto;

        return texto;
    }

    /// <summary>
    /// "CREDITO" o "CREDITO A {N} DIAS" segun la diferencia entre vencimiento y emision.
    /// </summary>
    private static string DiasCredito(InvoiceModel baseModel)
    {
        var doc = baseModel.Document;
        if (doc != null && doc.IssueDate > DateTime.MinValue && doc.DueDate > DateTime.MinValue)
        {
            var dias = (doc.DueDate - doc.IssueDate).Days;
            if (dias > 0)
                return dias == 1 ? "CREDITO A 1 DIA" : $"CREDITO A {dias} DIAS";
        }
        return "CREDITO";
    }

    /// <summary>
    /// Resuelve FactorEmp, UmEmp y FactorEmpaque por línea.
    /// Modo por item (nuevo formato): el XSLT expone f_factor_emp_{n}, f_um_emp_{n} y
    /// f_factor_empaque_{n} desde cac:Item/cac:AdditionalItemProperty de cada InvoiceLine
    /// (valores directos; 0.0000 = sin pacas).
    /// Modo legado: CustomFields agrupados por el XSLT (valores repetidos unidos por espacio).
    /// Regla: si la Descripción termina en número, ese es su factor; si no,
    /// toma el siguiente valor pendiente en el orden del XML.
    /// Devuelve los factores keyed por referencia de línea (no muta los models).
    /// </summary>
    private static Dictionary<LineItemInfo, FactorLineInfo> ResolveFactorEmp(List<LineItemInfo>? lines, AdditionalInfo? additional)
    {
        var result = new Dictionary<LineItemInfo, FactorLineInfo>();
        if (lines == null || additional?.CustomFields == null) return result;

        var porItem = additional.CustomFields.ContainsKey("f_factor_emp_1")
                      || additional.CustomFields.ContainsKey("f_um_emp_1")
                      || additional.CustomFields.ContainsKey("f_factor_empaque_1");

        if (porItem)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var n = i + 1;
                result[lines[i]] = new FactorLineInfo
                {
                    FactorEmp = TryDecimal(additional.CustomFields.GetValueOrDefault($"f_factor_emp_{n}")),
                    UmEmp = additional.CustomFields.GetValueOrDefault($"f_um_emp_{n}") ?? string.Empty,
                    FactorEmpaque = TryDecimal(additional.CustomFields.GetValueOrDefault($"f_factor_empaque_{n}"))
                };
            }
            return result;
        }

        var pendientes = SplitFactors(additional.CustomFields.GetValueOrDefault("f_factor_emp"));
        var usados = new HashSet<int>();

        for (var i = 0; i < lines.Count; i++)
        {
            if (!TryGetTrailingNumber(lines[i].Description, out var factor))
                continue;

            var idx = pendientes.FindIndex(p => p == factor);
            if (idx < 0) continue;

            result[lines[i]] = new FactorLineInfo { FactorEmp = factor };
            pendientes.RemoveAt(idx);
            usados.Add(i);
        }

        var pendienteIdx = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (usados.Contains(i) || pendienteIdx >= pendientes.Count)
                continue;

            result[lines[i]] = new FactorLineInfo { FactorEmp = pendientes[pendienteIdx] };
            pendienteIdx++;
        }

        var umEmp = additional.CustomFields.GetValueOrDefault("f_um_emp") ?? string.Empty;
        var empaques = SplitFactors(additional.CustomFields.GetValueOrDefault("f_factor_empaque"));
        for (var i = 0; i < lines.Count; i++)
        {
            if (!result.TryGetValue(lines[i], out var factorInfo))
                factorInfo = result[lines[i]] = new FactorLineInfo();

            factorInfo.UmEmp = umEmp;
            factorInfo.FactorEmpaque = i < empaques.Count ? empaques[i] : 0m;
        }

        return result;
    }

    private static decimal TryDecimal(string? value)
    {
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    }

    private static List<decimal> SplitFactors(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<decimal>();

        return value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m)
            .ToList();
    }

    private static bool TryGetTrailingNumber(string? description, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(description)) return false;

        var tokens = description.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;

        return decimal.TryParse(tokens[^1], NumberStyles.Any, CultureInfo.InvariantCulture, out value);
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
    public static string FontFamily = "Liberation Sans";

    public static float BorderWidth = 0.5f;

    public static float HeaderPaddingVertical = 0.9f;
    public static float HeaderPaddingHorizontal = 1.1f;
    public static float DataPaddingVertical = 0.75f;
    public static float DataPaddingHorizontal = 1.1f;

}