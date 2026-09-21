namespace PdfQuickDebug.Core;

/// <summary>
/// Modelo que representa la estructura del XML Simple después de la transformación XSLT.
/// Este es el resultado directo del parser: TransformedXmlParser.Parse(xmlSimple)
/// </summary>
public class InvoiceModel
{
    public Folio Folio {get; set;} = new();
    public DocumentInfo Document { get; set; } = new();
    public PartyInfo Issuer { get; set; } = new();
    public PartyInfo Customer { get; set; } = new();
    public List<LineItemInfo> Lines { get; set; } = new();
    public TotalsInfo Totals { get; set; } = new();
    public QRInfo QR { get; set; } = new();
    public AdditionalInfo Additional { get; set; } = new();
    public AdditionalInformation InformacionAdditional { get; set; } = new();
}

public class Folio
{
    public string Aprobacion_num {get; set;} = string.Empty;
    public string Prefijo {get; set;} = string.Empty;
    public string Inicio_num {get; set;} = string.Empty;
    public string Fin_num {get; set;} = string.Empty;
    public string Aprobacion_dt {get; set;} = string.Empty;
    public string Expiracion_dt {get; set;} = string.Empty;
}

public class DocumentInfo
{
    public string CUFE { get; set; } = string.Empty;
    public string CUDE { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string HoraEmision { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "COP";
    public string DocumentType { get; set; } = string.Empty;
    public decimal Total_am {get; set;}
    public decimal Total_ibua {get; set;}
    public decimal ValorBruto {get; set;}
    public string TotalVentaNeta {get; set;}
    public decimal TotalIVA {get; set;}
    public string TotalLetras {get; set;} = string.Empty;
    public string Comentario_ds {get; set;} = string.Empty;
    public string FacturaBase {get; set;} = string.Empty;
    public string HoraDocumento {get; set;} = string.Empty;
    public string TotalCantidad {get; set;} = string.Empty;
    public string TotalPacas {get; set;} = string.Empty;
    public string TotalValorDescuentos {get; set;} = string.Empty;
    public string FirmaDigital {get; set;} = string.Empty;
}

public class PartyInfo
{
    public string TaxId { get; set; } = string.Empty;
    public string TaxIdType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Name_Comercial { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxScheme { get; set; } = string.Empty;
}

public class LineItemInfo
{
    public int LineNumber { get; set; }
    public string Cant_Und { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Tiquete { get; set; } = string.Empty;
    public string Codigo_cd { get; set; } = string.Empty;
    public string TVenta { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit_Medition { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal PrecioAntesIVA { get; set; }
    public decimal ValorDescuento { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxValor { get; set; }
    public decimal LineTotal { get; set; }
    public decimal Ibua { get; set; }
}

public class TotalsInfo
{
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal Total { get; set; }
}

public class QRInfo
{
    public string QRCode { get; set; } = string.Empty;
    public string CUFE { get; set; } = string.Empty;
}

public class AdditionalInfo
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentCondition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

public class AdditionalInformation
{
    public string Telefono_Receptor { get; set; } = string.Empty;
    public string NumCargue { get; set; } = string.Empty;
    public string OC { get; set; } = string.Empty;
    public string NumPedido { get; set; } = string.Empty;
    public string Zona {get; set;} = string.Empty;
}
// Interfaces y clases necesarias para templates
public interface IInvoiceTemplate
{
    byte[] GeneratePdf(InvoiceModel model);
    TemplateMetadata GetMetadata();

    /// <summary>
    /// Mapea el modelo genérico InvoiceModel a un DTO personalizado del cliente.
    /// Por defecto retorna el mismo modelo sin cambios.
    /// Los templates que requieran campos personalizados deben sobrescribir este método.
    /// </summary>
    /// <param name="baseModel">Modelo base parseado del XML</param>
    /// <returns>DTO personalizado (puede ser el mismo InvoiceModel o una subclase)</returns>
    InvoiceModel MapToCustomDto(InvoiceModel baseModel) => baseModel;
}

public class TemplateMetadata
{
    public string Nit { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
