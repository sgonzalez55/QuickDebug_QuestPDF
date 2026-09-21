namespace Services.PdfGenerator.Infrastructure.Templates._000000;

using Services.PdfGenerator.Domain.Models;

/// <summary>
/// DTO personalizado del cliente generado por el diseñador (merge con el heredado).
/// Hereda de InvoiceModel y agrega campos específicos del cliente.
/// </summary>

public class DtoUbl : InvoiceModel
{
    /// <summary>Zona de distribución del cliente</summary>
    public string Zona { get; set; } = string.Empty;

    /// <summary>Ruta de entrega</summary>
    public string Ruta { get; set; } = string.Empty;

    /// <summary>Placa del vehículo de entrega</summary>
    public string PlacaVehiculo { get; set; } = string.Empty;

    /// <summary>Número de cargue</summary>
    public string NumCargue { get; set; } = string.Empty;

    /// <summary>Orden de compra del cliente</summary>
    public string OrdenCompra { get; set; } = string.Empty;

    /// <summary>Número de pedido</summary>
    public string NumPedido { get; set; } = string.Empty;

    /// <summary>Fecha de firma DIAN (xades:SigningTime)</summary>
    public string FechaDian { get; set; } = string.Empty;

    /// <summary>Hora de generación del documento</summary>
    public string HoraGeneracion { get; set; } = string.Empty;

    /// <summary>Texto de la resolución DIAN</summary>
    public string ResolucionTexto { get; set; } = string.Empty;

    /// <summary>Logo en formato Base64 o URL</summary>
    public string LogoBase64 { get; set; } = string.Empty;

    /// <summary>Total en letras (calculado)</summary>
    public string ValorLetras { get; set; } = string.Empty;

    /// <summary>Condición de pago (1=CONTADO, 2=CREDITO) según cac:PaymentMeans/cbc:ID del ERP</summary>
    public string CondicionPago { get; set; } = string.Empty;

    /// <summary>Factores de empaque por línea, resueltos desde CustomFields en MapToCustomDto.
    /// Keyed por referencia de LineItemInfo (misma lista que Lines, robusto a paginación).</summary>
    public Dictionary<LineItemInfo, FactorLineInfo> Factores { get; set; } = new();
    
    /// <summary>Motivo de venta (f_motivo, AdditionalItemProperty) por línea: valor crudo 01-08.
    /// Keyed por referencia de LineItemInfo, igual que Factores. Solo se usa en FacturaUbl.</summary>
    public Dictionary<LineItemInfo, string> Motivos { get; set; } = new();
    
    /// <summary>Código de barras principal (f_codigo_barra_principal, AdditionalItemProperty) por línea.
    /// Keyed por referencia de LineItemInfo, igual que Factores. Solo se usa en FacturaUbl.</summary>
    public Dictionary<LineItemInfo, string> CodigosBarra { get; set; } = new();
}

/// <summary>
/// Datos de factor de empaque de una línea, calculados por el template (no viven en Models).
/// </summary>
public class FactorLineInfo
{
    public string UmEmp { get; set; } = string.Empty;
    public decimal FactorEmp { get; set; }
    public decimal FactorEmpaque { get; set; }
}
