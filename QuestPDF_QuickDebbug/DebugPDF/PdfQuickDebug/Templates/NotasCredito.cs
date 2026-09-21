using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfQuickDebug.Core;
using QRCoder;
using QuestPDF.Companion;
using System.Globalization;

namespace PdfQuickDebug.Templates;

/// <summary>
/// Plantilla optimizada de factura electrónica para AJECOLOMBIA SAS (NIT 1193122070)
/// </summary>
public class NotasCredito : IInvoiceTemplate
{
    private static bool _encodingProviderRegistered = false;
    private static readonly object _lock = new object();
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    // Registrar proveedor de codificación para Windows-1252 (UTF-8 fix)
    static NotasCredito()
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

        return Document.Create(container =>
        {
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var productosPagina = pages[pageIndex];
                container.Page(page =>
                {
                    page.Size(612, 399);
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
                            .Text($"VALOR EN LETRAS: {valorLetras} M/CTE*******").FontSize(7.6f);

                        // Footer legal
                        ComposeFooter(footerColumn, dto);
                    });
                });
            }
            // container.Page(page =>
            // {
            //     page.Size(612, 405);
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
            //             .Text($"VALOR EN LETRAS: {valorLetras} *******").FontSize(6f);

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

        Document.Create(container =>
        {
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var productosPagina = pages[pageIndex];
                container.Page(page =>
                {
                    page.Size(612, 399);
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
                            .Text($"VALOR EN LETRAS: {valorLetras} M/CTE*******").FontSize(7.6f);

                        // Footer legal
                        ComposeFooter(footerColumn, dto);
                    });
                });
            }
            // container.Page(page =>
            // {
            //     page.Size(612, 405);
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
            //         footerColumn.Item().PaddingTop(1f).PaddingBottom(0f)
            //             .Text($"VALOR EN LETRAS: {valorLetras} M/CTE*******").FontSize(6f);

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

        column.Item().AlignRight().AlignBottom().Text("NOTA CREDITO DE LA FACTURA ELECTRONICA DE VENTA").Bold().FontSize(8);
        column.Item().Row(row =>
        {
            // Logo corporativo AJE - ultra compacto
            row.ConstantItem(68).Padding(1.0f).AlignCenter().AlignTop()
                .Element(c =>
                {
                    if (logoBytes != null)
                        c.Height(48).Image(logoBytes).FitHeight();
                    else
                        c.Height(48).AlignCenter().AlignMiddle().Text("").FontSize(8);
                });

            // Información del emisor centrada
            row.RelativeItem().PaddingHorizontal(3.5f).Column(col =>
            {
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Name)).FontSize(7f).Bold();
                col.Item().AlignCenter().Text($"NIT. {SafeGetValue(model.Issuer?.TaxId)}").FontSize(7f).Bold();
                col.Item().AlignCenter().Text(SafeGetValue(model.Issuer?.Address)).FontSize(7f);
                col.Item().AlignCenter().Text($"Teléfono: {SafeGetValue(model.Issuer?.Phone)}").FontSize(7f);

                // Usar propiedad tipada del DTO si está disponible
                var resolucionTexto = dto?.ResolucionTexto ?? GetCustomField(model, "ResolucionTexto", "");
                col.Item().AlignCenter().Text(txt =>
                {
                    txt.Span("AGENTE RETENEDOR DE IVA - SOMOS RETENEDORES DE RENTA SEGUN RESOLUCIÓN No 13834 del 20 de Noviembre de 2007 - GRAN CONTRIBUYENTES RESOLUCIÓN No 000200 del 27 de Diciembre de 2024").FontSize(7f);
                });
            });

            // Factura y QR
            row.ConstantItem(160).Column(col =>
            {
                col.Item().PaddingHorizontal(1.2f).PaddingLeft(15f).AlignLeft().Column(inner =>
                {
                    inner.Item().AlignCenter().AlignMiddle().Text($"{SafeGetValue(model.Document?.Prefix)}    {SafeGetValue(model.Document?.Number)}").FontSize(8.0f).FontColor(BrandColors.TextBlack);
                });

                col.Item().PaddingTop(0f).PaddingLeft(15f).AlignLeft().AlignTop()
                    .Element(c =>
                    {
                        if (qrCodeBytes != null)
                            c.Width(25, Unit.Millimetre).Height(20, Unit.Millimetre).Image(qrCodeBytes).FitArea();
                        else
                            c.Width(25, Unit.Millimetre).Height(20, Unit.Millimetre).Text("").FontSize(6);
                    });
            });
        });
    }

    private void ComposeClientInfo(ColumnDescriptor column, InvoiceModel model)
    {
        // Obtener DtoUbl para acceder a propiedades tipadas
        var dto = model as DtoUbl;

        column.Item().PaddingTop(0).Row(row =>
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
                col.Item().Text("NOMBRE CLIENTE: ").FontSize(7f).Bold();
                col.Item().Text("NOMBRE COMERCIAL: ").FontSize(7f).Bold();
                col.Item().Text("NUMERO DOCTO: ").FontSize(7f).Bold();
                col.Item().Text("DIRECCION DESTINO: ").FontSize(7f).Bold();
                col.Item().Text("CONDICION DE PAGO: ").FontSize(7f).Bold();
            });
            row.RelativeItem(2).PaddingLeft(4).Column(col =>
            {
                col.Item().Text(model.Customer?.Name ?? "").FontSize(7f);
                col.Item().Text(model.Customer?.Name_Comercial).FontSize(7f);
                col.Item().Text(model.Customer?.TaxId ?? "").FontSize(7f);
                col.Item().Text(model.Customer?.Address ?? "").FontSize(7f);
                col.Item().Text(model.Additional?.PaymentMethod ?? "").FontSize(7f);
            });

            // COLUMNA 2 - Usar propiedades tipadas del DTO
            row.AutoItem().Column(col =>
            {
                col.Item().Text("ZONA: ").FontSize(7f).Bold();
                col.Item().Text("RUTA: ").FontSize(7f).Bold();
                col.Item().Text("TELEFONO: ").FontSize(7f).Bold(); //Falta mapear en el XML
                col.Item().Text("FECHA Y HORA GENERACION: ").FontSize(7f).Bold();
                col.Item().Text("FACTURA BASE: ").FontSize(7f).Bold(); //Falta mapear en el XML, XSLT y DTO
            });
            row.RelativeItem(1.5f).PaddingLeft(4).Column(col =>
            {
                // Usar propiedades tipadas del DTO si está disponible, sino fallback a CustomFields
                col.Item().Text(dto?.Zona ?? GetCustomField(model, "Zona", "")).FontSize(7f);
                col.Item().Text(dto?.Ruta ?? GetCustomField(model, "Ruta", "")).FontSize(7f);
                col.Item().Text(model.InformacionAdditional?.Telefono_Receptor).FontSize(7f);
                col.Item().Text($"{model.Document?.IssueDate:dd/MM/yyyy} {DateTimeOffset.Parse(model.Document?.HoraEmision):hh:mm:ss tt}").FontSize(7f);
                col.Item().Text($"{model.Document?.FacturaBase}").FontSize(7f);
            });

            // COLUMNA 3 - Usar propiedades tipadas del DTO
            row.AutoItem().Column(col =>
            {
                col.Item().Text("MEDIO DE PAGO:").FontSize(7f).Bold();
                col.Item().Text("FECHA DE VENCIMIENTO: ").FontSize(7f).Bold();
                col.Item().Text("NUM. CARGUE: ").FontSize(7f).Bold();
                col.Item().Text("ORDEN COMPRA: ").FontSize(7f).Bold();
                col.Item().Text("PLACA VEHICULO: ").FontSize(7f).Bold();
            });
            row.RelativeItem(1.5f).PaddingLeft(4).Column(col =>
            {
                col.Item().Text(/*model.Additional?.PaymentMethod ??*/ "").FontSize(7f);
                col.Item().Text($"{model.Document?.DueDate:dd/MM/yyyy} {DateTimeOffset.Parse(model.Document?.HoraEmision):hh:mm:ss tt}").FontSize(7f);
                // Usar propiedades tipadas del DTO si está disponible
                col.Item().Text(dto?.NumCargue ?? model.InformacionAdditional?.NumCargue ?? "").FontSize(7f);
                col.Item().Text(dto?.OrdenCompra ?? model.InformacionAdditional?.OC ?? "").FontSize(7f);
                col.Item().Text(dto?.PlacaVehiculo ?? GetCustomField(model, "Placa", "")).FontSize(7f);
            });
        });
    }

    private void ComposeProductTable(ColumnDescriptor column, List<LineItemInfo> lines, InvoiceModel model)
    {
        column.Item().BorderLeft(0.5f).BorderRight(0.5f).BorderTop(0.5f).BorderBottom(0).BorderColor(BrandColors.TextBlack).Table(table =>
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
                    //.Border(InvoiceTableStyles.BorderWidth)
                    .BorderLeft(0.5f)
                    .BorderBottom(0)
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
                    //.Border(InvoiceTableStyles.BorderWidth)
                    .BorderLeft(0.5f)
                    .BorderBottom(0)
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
                    //.Border(InvoiceTableStyles.BorderWidth)
                    .BorderLeft(0.5f)
                    .BorderBottom(0)
                    .BorderColor(InvoiceTableStyles.BorderColor)
                    .PaddingVertical(InvoiceTableStyles.DataPaddingVertical)
                    .PaddingHorizontal(InvoiceTableStyles.DataPaddingHorizontal)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(text ?? string.Empty)
                    .FontFamily(InvoiceTableStyles.FontFamily)
                    .FontSize(InvoiceTableStyles.DataFontSize)
                    .FontColor(InvoiceTableStyles.TextColor);

                DataCellLeft(line.Description);
                DataCellCenter(line.Unit_Medition);
                DataCellCenter(line.Codigo_cd);
                DataCellCenter(line.Tiquete);
                DataCellCenter(line.TVenta);
                DataCellCenter(line.Cant_Und.ToString());
                DataCellCenter(line.Quantity.ToString("N0"));
                DataCellRight($"$ {line.PrecioAntesIVA.ToString("N0")}");
                DataCellRight(line.ValorDescuento.ToString("N0"));
                DataCellRight(line.TaxRate.ToString("N2"));
                DataCellRight($"$ {line.TaxValor.ToString("N0")}");
                DataCellRight(line.Ibua.ToString("N0"));
                DataCellRight($"$ {line.LineTotal.ToString("N0")}");
            }
        });

        column.Item().ExtendVertical().BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Row(row =>
        {
            row.RelativeItem(4.8f).BorderLeft(0.5f);
            row.ConstantItem(21).BorderLeft(0.5f);
            row.ConstantItem(44).BorderLeft(0.5f);
            row.ConstantItem(44).BorderLeft(0.5f);
            row.ConstantItem(35).BorderLeft(0.5f);
            row.ConstantItem(29).BorderLeft(0.5f);
            row.ConstantItem(33).BorderLeft(0.5f);
            row.ConstantItem(53).BorderLeft(0.5f);
            row.ConstantItem(30).BorderLeft(0.5f);
            row.ConstantItem(27).BorderLeft(0.5f);
            row.ConstantItem(44).BorderLeft(0.5f);
            row.ConstantItem(27).BorderLeft(0.5f);
            row.ConstantItem(58).BorderLeft(0.5f);
        });
    }

    private void ComposeTotalsTable(ColumnDescriptor column, InvoiceModel model, List<LineItemInfo> productosPagina)
    {
        column.Item().Repeat().PaddingTop(3).BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
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
            var totalUnidades = model.Lines?.Sum(l => int.Parse(l.Cant_Und.Replace(".", ""))) ?? 0;
            var totalQuantity = model.Lines?.Sum(l => l.Quantity) ?? 0;
            var totalDescuento = model.Lines?.Sum(l => l.ValorDescuento) ?? 0;
            var totalIva = model.Lines?.Sum(l => l.TaxValor) ?? 0;
            var totalBruto = model.Lines?.Sum(l => l.PrecioAntesIVA) ?? 0;
            var totalFinal = model.Lines?.Sum(l => l.LineTotal) ?? 0;
            int TotalItems = productosPagina.Sum(x => int.Parse(x.Cant_Und.Replace(".", "")));
            decimal TotalPacas = productosPagina.Sum(x => x.Quantity);
            TotalValueCell(TotalItems.ToString());
            TotalValueCell(TotalPacas.ToString("N0"));
            TotalValueCell("0.00");
            TotalValueCell($"$ {totalDescuento:N0}");
            TotalValueCell($"$ {totalBruto:N0}");
            TotalValueCell($"$ {totalIva:N0}");
            TotalValueCell($"{model.Document?.Total_ibua.ToString("N0") ?? "0.00"}");
            TotalValueCell($"$ {totalFinal:N0}");
            TotalValueCell($"$ {totalFinal:N0}", true);
        });
    }

    private void ComposeFooter(ColumnDescriptor column, InvoiceModel model)
    {
        column.Item().PaddingTop(3f).Column(col =>
        {
            col.Item().PaddingTop(0f).PaddingBottom(3f).AlignMiddle().Row(row =>
            {
                row.AutoItem().AlignMiddle().Text("Notas al Documento:").FontSize(6.8f).Bold();
                row.RelativeItem().AlignMiddle().Text(model.Document?.Comentario_ds).FontSize(5.8f);
            });

            col.Item().AlignCenter().Text("AJECOLOMBIA SAS solo reconocerá las averías de producto que se generen por concepto de calidad y/o transporte y que sean reportados al momento de la entrega del producto. No se reconocerán devoluciones de producto vencido, ni ninguna otra causal después de la recibido a satisfacción por EL COMPRADOR. Para cualquier COMPRADOR, deberá reportar la novedad dentro de las 48 horas siguientes a la fecha de recibido el producto al correo atencion.clientes.co@ajegroup.com y remitir el soporte fotográfico y número de factura. Transcurrido el término mencionado sin generarse ninguna novedad, se entenderá recibido el producto a satisfacción.").FontSize(6.8f).LineHeight(1.16f);

            col.Item().PaddingVertical(2.5f).AlignCenter().AlignMiddle().Text("Factura generada por software SIESA de SISTEMAS DE INFORMACION EMPRESARIAL SA. Nit 890.319.193-3. Siesa e-Invoicing Nit 890.319.193-3.").FontSize(6.5f).LineHeight(1.16f);

            col.Item().PaddingTop(2.8f).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Row(row =>
                    {
                        row.AutoItem().Text("CUDE: ").FontSize(5.8f).Bold();
                        row.RelativeItem().Text(model.Document?.CUDE ?? "").FontSize(5.8f);
                    });
                    c.Item().Row(row =>
                    {
                        row.AutoItem().Text("CUFE: ").FontSize(5.8f).Bold();
                        row.RelativeItem().Text(model.Document?.CUFE ?? "").FontSize(5.8f);
                    });
                });
                // r.ConstantItem(198).AlignRight().Text(txt =>
                // {
                //     txt.Span($"Fecha Aceptación DIAN: ").FontSize(5.9f).Bold();
                //     txt.Span($"{model.Document?.IssueDate:yyyy-MM-dd} {model.Document?.HoraDocumento}").FontSize(5.9f);
                // });
            });

            col.Item().PaddingTop(2.9f).Text(txt =>
            {
                txt.Span($"FIRMA DIGITAL: \n").FontSize(5.8f).Bold();
                txt.Span($"{model.Document?.FirmaDigital}").FontSize(5.8f);
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
            Nit = "800153993",
            TemplateName = "notacredito-ubl",
            ClientName = "AJECOLOMBIA SAS",
            Version = "2.0",
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mapea el InvoiceModel genérico a un DtoUbl con campos personalizados de AJECOLOMBIA.
    /// Este método extrae los valores del diccionario CustomFields y los asigna a propiedades tipadas.
    /// </summary>
    /// <param name="baseModel">Modelo base parseado del XML</param>
    /// <returns>DtoUbl con propiedades personalizadas mapeadas</returns>
    public InvoiceModel MapToCustomDto(InvoiceModel baseModel)
    {
        // Si ya es un DtoUbl, retornarlo directamente
        if (baseModel is DtoUbl existingDto)
            return existingDto;

        // Crear nuevo DtoUbl con todas las propiedades base
        var dto = new DtoUbl
        {
            // Copiar propiedades base de InvoiceModel
            Document = baseModel.Document,
            Issuer = baseModel.Issuer,
            Customer = baseModel.Customer,
            Lines = baseModel.Lines,
            Totals = baseModel.Totals,
            QR = baseModel.QR,
            Additional = baseModel.Additional,
            InformacionAdditional = baseModel.InformacionAdditional,

            // Mapear campos personalizados desde CustomFields a propiedades tipadas
            Zona = GetCustomFieldStatic(baseModel, "Zona"),
            Ruta = GetCustomFieldStatic(baseModel, "Ruta"),
            PlacaVehiculo = GetCustomFieldStatic(baseModel, "Placa"),
            NumCargue = baseModel.InformacionAdditional?.NumCargue ?? GetCustomFieldStatic(baseModel, "NumCargue"),
            OrdenCompra = baseModel.InformacionAdditional?.OC ?? GetCustomFieldStatic(baseModel, "OrdenCompra"),
            NumPedido = GetCustomFieldStatic(baseModel, "NumPedido"),
            HoraGeneracion = GetCustomFieldStatic(baseModel, "HoraGeneracion"),
            ResolucionTexto = GetCustomFieldStatic(baseModel, "ResolucionTexto"),
            LogoBase64 = GetCustomFieldStatic(baseModel, "LogoBase64"),

            // Calcular valor en letras
            ValorLetras = PDF_Utils.NumeroALetrasCOP(baseModel.Document?.Total_am ?? 0)
        };

        return dto;
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
}
