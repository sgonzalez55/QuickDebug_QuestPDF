namespace Services.PdfGenerator.Infrastructure.Templates._800153993;

using Services.PdfGenerator.Domain.Models;

/// <summary>
/// DTO personalizado del cliente generado por el diseñador (merge con el heredado).
/// Hereda de InvoiceModel y agrega campos específicos del cliente.
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

    /// <summary>Condición de pago (1=CONTADO, 2=CREDITO) según cac:PaymentMeans/cbc:ID del ERP.</summary>
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


    // ═══════════════════════════════════════════════════════════════════════════════
    // CAMPOS NUEVOS DESCUBIERTOS POR EL CONTRATO
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>Campo <c>CPDF</c> (CustomField descubierto)</summary>
    public string CPDF { get; set; } = string.Empty;

    /// <summary>Campo <c>f_20_00007591_G502_1_ZONA</c> (CustomField descubierto)</summary>
    public string F2000007591G5021ZONA { get; set; } = string.Empty;

    /// <summary>Campo <c>f_20_00007592_G502_1_RUTA</c> (CustomField descubierto)</summary>
    public string F2000007592G5021RUTA { get; set; } = string.Empty;

    /// <summary>Campo <c>f_cargue</c> (CustomField descubierto)</summary>
    public string FCargue { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_1</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal1 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_10</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal10 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_11</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal11 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_12</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal12 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_13</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal13 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_14</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal14 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_15</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal15 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_2</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal2 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_3</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal3 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_4</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal4 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_5</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal5 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_6</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal6 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_7</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal7 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_8</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal8 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_codigo_barra_principal_9</c> (CustomField descubierto)</summary>
    public string FCodigoBarraPrincipal9 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_condicion_pago</c> (CustomField descubierto)</summary>
    public string FCondicionPago { get; set; } = string.Empty;

    /// <summary>Campo <c>f_direccion2_suc</c> (CustomField descubierto)</summary>
    public string FDireccion2Suc { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_1</c> (CustomField descubierto)</summary>
    public string FFactorEmp1 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_10</c> (CustomField descubierto)</summary>
    public string FFactorEmp10 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_11</c> (CustomField descubierto)</summary>
    public string FFactorEmp11 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_12</c> (CustomField descubierto)</summary>
    public string FFactorEmp12 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_13</c> (CustomField descubierto)</summary>
    public string FFactorEmp13 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_14</c> (CustomField descubierto)</summary>
    public string FFactorEmp14 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_15</c> (CustomField descubierto)</summary>
    public string FFactorEmp15 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_2</c> (CustomField descubierto)</summary>
    public string FFactorEmp2 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_3</c> (CustomField descubierto)</summary>
    public string FFactorEmp3 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_4</c> (CustomField descubierto)</summary>
    public string FFactorEmp4 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_5</c> (CustomField descubierto)</summary>
    public string FFactorEmp5 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_6</c> (CustomField descubierto)</summary>
    public string FFactorEmp6 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_7</c> (CustomField descubierto)</summary>
    public string FFactorEmp7 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_8</c> (CustomField descubierto)</summary>
    public string FFactorEmp8 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_emp_9</c> (CustomField descubierto)</summary>
    public string FFactorEmp9 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_1</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque1 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_10</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque10 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_11</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque11 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_12</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque12 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_13</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque13 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_14</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque14 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_15</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque15 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_2</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque2 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_3</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque3 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_4</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque4 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_5</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque5 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_6</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque6 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_7</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque7 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_8</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque8 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_factor_empaque_9</c> (CustomField descubierto)</summary>
    public string FFactorEmpaque9 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_fecha_dian</c> (CustomField descubierto)</summary>
    public string FFechaDian { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_1</c> (CustomField descubierto)</summary>
    public string FMotivo1 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_10</c> (CustomField descubierto)</summary>
    public string FMotivo10 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_11</c> (CustomField descubierto)</summary>
    public string FMotivo11 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_12</c> (CustomField descubierto)</summary>
    public string FMotivo12 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_13</c> (CustomField descubierto)</summary>
    public string FMotivo13 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_14</c> (CustomField descubierto)</summary>
    public string FMotivo14 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_15</c> (CustomField descubierto)</summary>
    public string FMotivo15 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_2</c> (CustomField descubierto)</summary>
    public string FMotivo2 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_3</c> (CustomField descubierto)</summary>
    public string FMotivo3 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_4</c> (CustomField descubierto)</summary>
    public string FMotivo4 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_5</c> (CustomField descubierto)</summary>
    public string FMotivo5 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_6</c> (CustomField descubierto)</summary>
    public string FMotivo6 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_7</c> (CustomField descubierto)</summary>
    public string FMotivo7 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_8</c> (CustomField descubierto)</summary>
    public string FMotivo8 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_motivo_9</c> (CustomField descubierto)</summary>
    public string FMotivo9 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_orden_compra</c> (CustomField descubierto)</summary>
    public string FOrdenCompra { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_1</c> (CustomField descubierto)</summary>
    public string FUmEmp1 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_10</c> (CustomField descubierto)</summary>
    public string FUmEmp10 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_11</c> (CustomField descubierto)</summary>
    public string FUmEmp11 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_12</c> (CustomField descubierto)</summary>
    public string FUmEmp12 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_13</c> (CustomField descubierto)</summary>
    public string FUmEmp13 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_14</c> (CustomField descubierto)</summary>
    public string FUmEmp14 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_15</c> (CustomField descubierto)</summary>
    public string FUmEmp15 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_2</c> (CustomField descubierto)</summary>
    public string FUmEmp2 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_3</c> (CustomField descubierto)</summary>
    public string FUmEmp3 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_4</c> (CustomField descubierto)</summary>
    public string FUmEmp4 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_5</c> (CustomField descubierto)</summary>
    public string FUmEmp5 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_6</c> (CustomField descubierto)</summary>
    public string FUmEmp6 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_7</c> (CustomField descubierto)</summary>
    public string FUmEmp7 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_8</c> (CustomField descubierto)</summary>
    public string FUmEmp8 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_um_emp_9</c> (CustomField descubierto)</summary>
    public string FUmEmp9 { get; set; } = string.Empty;

    /// <summary>Campo <c>f_vehiculo_cargue</c> (CustomField descubierto)</summary>
    public string FVehiculoCargue { get; set; } = string.Empty;
















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
