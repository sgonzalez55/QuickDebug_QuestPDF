using System.Globalization;
using System.Xml.Linq;

namespace PdfQuickDebug.Core;

/// <summary>
/// Parser ligero para XML Simple generado por XSLT.
/// Convierte el XML transformado en un modelo InvoiceModel.
/// </summary>
public static class XmlParser
{
    public static InvoiceModel Parse(string transformedXml)
    {
        // Envolver en <Root> para manejar múltiples elementos raíz (CFD, Adicional)
        var wrappedXml = $"<Root>{transformedXml}</Root>";
        var doc = XDocument.Parse(wrappedXml);

        // Buscar el elemento CFD dentro del Root
        var cfdElement = doc.Root!.Element("CFD");
        if (cfdElement == null)
        {
            throw new InvalidOperationException("No se encontró el elemento CFD en el XML transformado");
        }

        // Buscar el elemento Adicional (puede estar fuera de CFD)
        var adicionalElement = doc.Root!.Element("Adicional");

        return new InvoiceModel
        {
            Folio = ParseFolio(cfdElement.Element("Folio")),
            Document = ParseDocument(cfdElement.Element("Documento")),
            Issuer = ParseParty(cfdElement.Element("Emisor")),
            Customer = ParseParty(cfdElement.Element("Receptor")),
            Lines = cfdElement.Elements("Detalle").Select(ParseLine).ToList(),
            Totals = ParseTotals(cfdElement.Element("Totales")),
            QR = ParseQR(cfdElement.Element("QR")),
            Additional = ParseAdditional(adicionalElement),
            InformacionAdditional = ParseInformacionAdittional(cfdElement.Element("informacion_adicional"))
        };
    }

    private static Folio ParseFolio(XElement? elem)
    {
        return new Folio
        {
            Aprobacion_num = elem.Attribute("aprobacion_num")?.Value ?? string.Empty,
            Prefijo = elem.Attribute("prefijo_cd")?.Value ?? string.Empty,
            Inicio_num = elem.Attribute("inicio_num")?.Value ?? string.Empty,
            Fin_num = elem.Attribute("fin_num")?.Value ?? string.Empty,
            Aprobacion_dt = elem.Attribute("aprobacion_dt")?.Value ?? string.Empty,
            Expiracion_dt = elem.Attribute("expiracion_dt")?.Value ?? string.Empty,
        };
    }

    private static DocumentInfo ParseDocument(XElement? elem)
    {
        if (elem == null) return new DocumentInfo();

        return new DocumentInfo
        {
            Number = elem.Attribute("numero_cd")?.Value ?? string.Empty,
            Prefix = elem.Attribute("prefijo_cd")?.Value ?? string.Empty,
            IssueDate = ParseDate(elem.Attribute("fecha_emision_fe")?.Value),
            HoraEmision = elem.Attribute("hora_documento_dt")?.Value ?? string.Empty,
            DueDate = ParseDate(elem.Attribute("fecha_vencimiento_fe")?.Value),
            Currency = elem.Attribute("moneda_cd")?.Value ?? "COP",
            DocumentType = elem.Attribute("tipo_cd")?.Value ?? string.Empty,
            Total_am = ParseDecimal(elem.Attribute("total_am")),
            Total_ibua = ParseDecimal(elem.Attribute("total_ibua")),
            TotalIVA = ParseDecimal(elem.Attribute("impuestos_am")),
            ValorBruto = ParseDecimal(elem.Attribute("subtotal_am")),
            TotalVentaNeta = elem.Attribute("total_ventaNeta")?.Value ?? string.Empty,
            TotalLetras = elem.Attribute("total_letras")?.Value ?? string.Empty,
            TotalCantidad = elem.Attribute("total_cantidad")?.Value ?? string.Empty,
            TotalPacas = elem.Attribute("total_pacas")?.Value ?? string.Empty,
            TotalValorDescuentos = elem.Attribute("descuentos_am")?.Value ?? string.Empty,
            FirmaDigital = elem.Attribute("firma_digital")?.Value ?? string.Empty,
            Comentario_ds = elem.Attribute("comentario_ds")?.Value ?? string.Empty,
            FacturaBase = elem.Attribute("documento_padre_numero_cd")?.Value ?? string.Empty,
            HoraDocumento = elem.Attribute("hora_documento_dt")?.Value ?? string.Empty,
            CUFE = elem.Attribute("documento_padre_cufe_cd")?.Value ?? string.Empty,
            CUDE = elem.Attribute("documento_nota_cude_cd")?.Value ?? string.Empty,
        };
    }

    private static PartyInfo ParseParty(XElement? elem)
    {
        if (elem == null) return new PartyInfo();

        return new PartyInfo
        {
            TaxId = elem.Attribute("nit_cd")?.Value ?? string.Empty,
            TaxIdType = elem.Attribute("tipo_id_cd")?.Value ?? string.Empty,
            Name = elem.Attribute("nombre_tx")?.Value ?? string.Empty,
            Name_Comercial = elem.Attribute("nombre_comercial_ds")?.Value ?? string.Empty,
            Address = elem.Attribute("direccion_tx")?.Value ?? string.Empty,
            City = elem.Attribute("ciudad_tx")?.Value ?? string.Empty,
            Phone = elem.Attribute("telefono_tx")?.Value ?? string.Empty,
            Email = elem.Attribute("email_tx")?.Value ?? string.Empty,
            TaxScheme = elem.Attribute("regimen_fiscal_cd")?.Value ?? string.Empty
        };
    }

    private static LineItemInfo ParseLine(XElement elem)
    {
        Console.WriteLine(ParseInt(elem.Attribute("cantidad_unit")));
        return new LineItemInfo
        {
            LineNumber = ParseInt(elem.Attribute("linea_nu")),
            Cant_Und = elem.Attribute("cantidad_unit")?.Value ?? string.Empty,
            Description = elem.Attribute("descripcion_tx")?.Value ?? string.Empty,
            Codigo_cd = elem.Attribute("codigo_cd")?.Value ?? string.Empty,
            Tiquete = elem.Attribute("tiquete")?.Value ?? string.Empty,
            TVenta = elem.Attribute("tventa")?.Value ?? string.Empty,
            Quantity = ParseDecimal(elem.Attribute("cantidad_nu")),
            Unit = elem.Attribute("unidad_cd")?.Value ?? string.Empty,
            Unit_Medition = elem.Attribute("unidad_medida_cd")?.Value ?? string.Empty,
            UnitPrice = ParseDecimal(elem.Attribute("precio_unitario_am")),
            PrecioAntesIVA = ParseDecimal(elem.Attribute("item_subtotal_am")),
            Discount = ParseDecimal(elem.Attribute("descuento_am")),
            TaxRate = ParseDecimal(elem.Attribute("impuesto_tasa_nu")),
            TaxValor = ParseDecimal(elem.Attribute("item_iva_am")),
            LineTotal = ParseDecimal(elem.Attribute("total_linea_am")),
            Ibua = ParseDecimal(elem.Attribute("ibua")),
            ValorDescuento = ParseDecimal(elem.Attribute("vlr_descuento")),
        };
    }

    private static TotalsInfo ParseTotals(XElement? elem)
    {
        if (elem == null) return new TotalsInfo();

        return new TotalsInfo
        {
            Subtotal = ParseDecimal(elem.Attribute("subtotal_am")),
            TotalDiscount = ParseDecimal(elem.Attribute("descuentos_am")),
            TotalTax = ParseDecimal(elem.Attribute("impuestos_am")),
            Total = ParseDecimal(elem.Attribute("total_am"))
        };
    }

    private static QRInfo ParseQR(XElement? elem)
    {
        if (elem == null) return new QRInfo();

        return new QRInfo
        {
            QRCode = elem.Attribute("qr_base64")?.Value ?? string.Empty,
            CUFE = elem.Attribute("cufe_cd")?.Value ?? string.Empty
        };
    }

    private static AdditionalInfo ParseAdditional(XElement? elem)
    {
        if (elem == null) return new AdditionalInfo();

        return new AdditionalInfo
        {
            PaymentMethod = elem.Attribute("forma_pago_cd")?.Value ?? string.Empty,
            Notes = elem.Attribute("notas_tx")?.Value ?? string.Empty,
            CustomFields = elem.Elements("Campo")
                .ToDictionary(
                    e => e.Attribute("clave")?.Value ?? string.Empty,
                    e => e.Attribute("valor")?.Value ?? string.Empty
                )
        };
    }

    private static AdditionalInformation ParseInformacionAdittional(XElement? elem)
    {
        if (elem == null) return new AdditionalInformation();

        return new AdditionalInformation
        {
            Telefono_Receptor = elem.Attribute("TelefonoReceptor")?.Value ?? string.Empty,
            NumCargue = elem.Attribute("NumCargue")?.Value ?? string.Empty,
            OC = elem.Attribute("OC")?.Value ?? string.Empty,
        };
    }

    // Helpers
    private static DateTime ParseDate(string? value)
    {
        return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : DateTime.MinValue;
    }

    private static decimal ParseDecimal(XAttribute? attr)
    {
        var strValue = attr?.Value;

        if (string.IsNullOrWhiteSpace(strValue))
            return 0m;

        // Remover símbolos de moneda, espacios y comas
        strValue = strValue.Replace("$", "").Replace(",", "").Trim();

        return decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static int ParseInt(XAttribute? attr)
    {
        return int.TryParse(attr?.Value, out var value) ? value : 0;
    }
}
