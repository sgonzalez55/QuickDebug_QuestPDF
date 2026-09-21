using Services.PdfGenerator.Domain.Models;

namespace Services.PdfGenerator.Infrastructure.Templates._830081407;

  
/// <summary>
/// DTO personalizado para AJECOLOMBIA SAS (NIT 1193122070) ka.
/// Hereda de InvoiceModel y agrega campos específicos del cliente.
///
/// Estos campos se extraen de CustomFields y se mapean a propiedades tipadas
/// para facilitar su uso en el template.
///
/// IMPORTANTE: Al exportar para el servicio, este archivo también debe copiarse
/// a la carpeta _1193122070/ del servicio Services.PdfGenerator.
/// </summary>
public class DtoUbl : InvoiceModel
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // CAMPOS PERSONALIZADOS DE AJECOLOMBIA
    // Estos campos se extraen del diccionario CustomFields en MapToCustomDto()
    // ═══════════════════════════════════════════════════════════════════════════════

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

    /// <summary>Hora de generación del documento</summary>
    public string HoraGeneracion { get; set; } = string.Empty;

    /// <summary>Texto de la resolución DIAN</summary>
    public string ResolucionTexto { get; set; } = string.Empty;

    /// <summary>Logo en formato Base64 o URL</summary>
    public string LogoBase64 { get; set; } = string.Empty;

    /// <summary>Total en letras (calculado)</summary>
    public string ValorLetras { get; set; } = string.Empty;

    /// <summary>Condición de pago según PaymentMeansCode DIAN (1=CONTADO, 2=CREDITO)</summary>
    public string CondicionPago { get; set; } = string.Empty;

    /// <summary>Factores de empaque por línea, resueltos desde CustomFields en MapToCustomDto.
    /// Keyed por referencia de LineItemInfo (misma lista que Lines, robusto a paginación).</summary>
    public Dictionary<LineItemInfo, FactorLineInfo> Factores { get; set; } = new();
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
