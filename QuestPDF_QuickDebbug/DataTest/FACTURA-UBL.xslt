<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:dif="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
	xmlns:fe='urn:oasis:names:specification:ubl:schema:xsd:Invoice-2'
	xmlns:cac='urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2'
	xmlns:cbc='urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2'
	xmlns:clm54217='urn:un:unece:uncefact:codelist:specification:54217:2001'
	xmlns:clm66411='urn:un:unece:uncefact:codelist:specification:66411:2001'
	xmlns:clmIANAMIMEMediaType='urn:un:unece:uncefact:codelist:specification:IANAMIMEMediaType:2003'
	xmlns:ext='urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2'
	xmlns:ds='http://www.w3.org/2000/09/xmldsig#'
	xmlns:qdt='urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2'
	xmlns:sac="urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1"
	xmlns:sts="dian:gov:co:facturaelectronica:Structures-2-1"
	xmlns:udt='urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2'
	xmlns:xades="http://uri.etsi.org/01903/v1.3.2#" xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	exclude-result-prefixes="cbc cac ext ds sts xades"
	xmlns:inv="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">
	<xsl:output method="xml" encoding="utf-8" indent="no" />
	<xsl:variable name="TaxExclusiveAmount" select="/fe:Invoice/cac:LegalMonetaryTotal/cbc:TaxExclusiveAmount" />
	<xsl:variable name="Notas" select="/fe:Invoice/cbc:Note" />
	<xsl:variable name="LineExtensionAmount" select="/fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount" />
	<xsl:variable name="PayableAmount" select="/fe:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount" />
	<xsl:variable name="TaxAmount" select="/fe:Invoice/cac:TaxTotal/cac:TaxSubtotal/cbc:TaxAmount" />
	<xsl:variable name="Folio"
		select="/fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl" />
	<xsl:variable name="ForeignMonetaryTotal"
		select="/fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/dif:ForeignCurrencyExtension/dif:ForeignCurrency/dif:LegalMonetaryTotal" />
	<xsl:variable name="InfoAdicional"
		select="/fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/dif:CustomFieldExtension/dif:CustomField" />
	<xsl:variable name="InfoAdicionalRows"
		select="/fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/dif:CustomFieldRowsExtension/dif:CustomFieldRow" />
	<xsl:variable name="CurrencyID" select="$InfoAdicional[@Name = 'Moneda']/@Value" />
	<xsl:variable name="prefijo"
		select="fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl/sts:AuthorizedInvoices/sts:Prefix" />
	<xsl:variable name="numeracionId" select="/fe:Invoice/cbc:ID" />
	<!-- Logo como variable global -->
	<xsl:variable name="LOGO_BASE64">
		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
	</xsl:variable>
	<xsl:decimal-format name="european" decimal-separator=',' grouping-separator='.' />
	<xsl:decimal-format name="us" decimal-separator='.' grouping-separator=',' />
	<xsl:decimal-format name="default" decimal-separator="." grouping-separator="," />
	<xsl:template name="numero-a-letras">
		<xsl:param name="numero" />
		<xsl:variable name="n" select="floor(number($numero))" />
		<xsl:variable name="d" select="round((number($numero) - $n) * 100)" />
		<xsl:call-template name="num-texto">
			<xsl:with-param name="n" select="$n" />
		</xsl:call-template>
		<xsl:text> PESOS CON </xsl:text>
		<xsl:value-of select="format-number($d,'00')" />
		<xsl:text>/100 M/CTE *******</xsl:text>
	</xsl:template>
	<xsl:template name="num-texto">
		<xsl:param name="n" />
		<xsl:choose>
			<xsl:when test="$n = 0">CERO</xsl:when>
			<xsl:when test="$n &lt; 10">
				<xsl:call-template name="unidad">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$n &lt; 100">
				<xsl:call-template name="decena">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$n &lt; 1000">
				<xsl:call-template name="centena">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$n &lt; 1000000">
				<xsl:call-template name="miles">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$n &lt; 1000000000">
				<xsl:call-template name="millones">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>VALOR FUERA DE RANGO</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="unidad">
		<xsl:param name="n" />
		<xsl:choose>
			<xsl:when test="$n=1">UNO</xsl:when>
			<xsl:when test="$n=2">DOS</xsl:when>
			<xsl:when test="$n=3">TRES</xsl:when>
			<xsl:when test="$n=4">CUATRO</xsl:when>
			<xsl:when test="$n=5">CINCO</xsl:when>
			<xsl:when test="$n=6">SEIS</xsl:when>
			<xsl:when test="$n=7">SIETE</xsl:when>
			<xsl:when test="$n=8">OCHO</xsl:when>
			<xsl:when test="$n=9">NUEVE</xsl:when>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="decena">
		<xsl:param name="n" />
		<xsl:choose>
			<xsl:when test="$n &lt; 10">
				<xsl:call-template name="unidad">
					<xsl:with-param name="n" select="$n" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="$n &lt; 20">
				<xsl:choose>
					<xsl:when test="$n=10">DIEZ</xsl:when>
					<xsl:when test="$n=11">ONCE</xsl:when>
					<xsl:when test="$n=12">DOCE</xsl:when>
					<xsl:when test="$n=13">TRECE</xsl:when>
					<xsl:when test="$n=14">CATORCE</xsl:when>
					<xsl:when test="$n=15">QUINCE</xsl:when>
					<xsl:otherwise>
						<xsl:text>DIECI</xsl:text>
						<xsl:call-template name="unidad">
							<xsl:with-param name="n" select="$n - 10" />
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:when test="$n &lt; 30">
				<xsl:choose>
					<xsl:when test="$n=20">VEINTE</xsl:when>
					<xsl:otherwise>
						<xsl:text>VEINTI</xsl:text>
						<xsl:call-template name="unidad">
							<xsl:with-param name="n" select="$n - 20" />
						</xsl:call-template>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="d" select="floor($n div 10)" />
				<xsl:variable name="u" select="$n mod 10" />
				<xsl:choose>
					<xsl:when test="$d=3">TREINTA</xsl:when>
					<xsl:when test="$d=4">CUARENTA</xsl:when>
					<xsl:when test="$d=5">CINCUENTA</xsl:when>
					<xsl:when test="$d=6">SESENTA</xsl:when>
					<xsl:when test="$d=7">SETENTA</xsl:when>
					<xsl:when test="$d=8">OCHENTA</xsl:when>
					<xsl:when test="$d=9">NOVENTA</xsl:when>
				</xsl:choose>
				<xsl:if test="$u &gt; 0">
					<xsl:text> Y </xsl:text>
					<xsl:call-template name="unidad">
						<xsl:with-param name="n" select="$u" />
					</xsl:call-template>
				</xsl:if>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="centena">
		<xsl:param name="n" />
		<xsl:variable name="c" select="floor($n div 100)" />
		<xsl:variable name="r" select="$n mod 100" />
		<xsl:choose>
			<xsl:when test="$n=100">CIEN</xsl:when>
			<xsl:when test="$c=1">CIENTO</xsl:when>
			<xsl:when test="$c=2">DOSCIENTOS</xsl:when>
			<xsl:when test="$c=3">TRESCIENTOS</xsl:when>
			<xsl:when test="$c=4">CUATROCIENTOS</xsl:when>
			<xsl:when test="$c=5">QUINIENTOS</xsl:when>
			<xsl:when test="$c=6">SEISCIENTOS</xsl:when>
			<xsl:when test="$c=7">SETECIENTOS</xsl:when>
			<xsl:when test="$c=8">OCHOCIENTOS</xsl:when>
			<xsl:when test="$c=9">NOVECIENTOS</xsl:when>
		</xsl:choose>
		<xsl:if test="$r &gt; 0">
			<xsl:text> </xsl:text>
			<xsl:call-template name="decena">
				<xsl:with-param name="n" select="$r" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<xsl:template name="miles">
		<xsl:param name="n" />
		<xsl:variable name="m" select="floor($n div 1000)" />
		<xsl:variable name="r" select="$n mod 1000" />
		<xsl:if test="$m &gt; 1">
			<xsl:call-template name="num-texto">
				<xsl:with-param name="n" select="$m" />
			</xsl:call-template>
		</xsl:if>
		<xsl:text> MIL</xsl:text>
		<xsl:if test="$r &gt; 0">
			<xsl:text> </xsl:text>
			<xsl:call-template name="num-texto">
				<xsl:with-param name="n" select="$r" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<xsl:template name="millones">
		<xsl:param name="n" />
		<xsl:variable name="m" select="floor($n div 1000000)" />
		<xsl:variable name="r" select="$n mod 1000000" />
		<xsl:choose>
			<xsl:when test="$m=1">UN MILLÃ“N</xsl:when>
			<xsl:otherwise>
				<xsl:call-template name="num-texto">
					<xsl:with-param name="n" select="$m" />
				</xsl:call-template>
				<xsl:text> MILLONES</xsl:text>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:if test="$r &gt; 0">
			<xsl:text> </xsl:text>
			<xsl:call-template name="num-texto">
				<xsl:with-param name="n" select="$r" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	<xsl:template match="/">
		<xsl:variable name="NotaPlaca"
			select="normalize-space(substring-after(/fe:Invoice/cbc:Note[starts-with(normalize-space(.), 'PPA-001')], 'PPA-001'))" />
		<CFD>
			<Folio aprobacion_num="{$Folio/sts:InvoiceAuthorization}"
				aprobacion_dt="{$Folio/sts:AuthorizationPeriod/cbc:StartDate}"
				expiracion_dt="{$Folio/sts:AuthorizationPeriod/cbc:EndDate}"
				prefijo_cd="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl/sts:AuthorizedInvoices/sts:Prefix}"
				inicio_num="{$Folio/sts:AuthorizedInvoices/sts:From}"
				fin_num="{$Folio/sts:AuthorizedInvoices/sts:To}" />
			<xsl:choose>
				<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
					<Documento tipo_cd="01" total_imp_am="{$InfoAdicional[@Name='TotalImpExt']/@Value}"
						numero_cd="{normalize-space(substring-after($numeracionId, $prefijo))}"
						prefijo_cd="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl/sts:AuthorizedInvoices/sts:Prefix}"
						fecha_emision_fe="{fe:Invoice/cbc:IssueDate}" hora_emision_fe="{fe:Invoice/cbc:IssueTime}"
						fecha_vencimiento_fe="{fe:Invoice/cac:PaymentMeans/cbc:PaymentDueDate}"
						hora_documento_dt="{fe:Invoice/cbc:IssueTime}"
						tasaCambio="{$InfoAdicional[@Name='tasaCambio']/@Value}" moneda_cd="{$CurrencyID}"
						comentario_ds="{fe:Invoice/cbc:Note}"
						total_cargos_nogravados="{sum(fe:Invoice/cac:InvoiceLine/cac:WithholdingTaxTotal/cbc:TaxAmount)}"
						total_cargos_gravados="{concat('$ ', format-number(sum(fe:Invoice/cac:InvoiceLine/cac:TaxTotal/cbc:TaxAmount), '#,##0.00##'))}"
						total_cantidad="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='CantUni']/cbc:Value)}"
						total_pacas="{sum(fe:Invoice/cac:InvoiceLine/cbc:InvoicedQuantity)}"
						total_vlr_descuento="{concat('$ ', format-number(sum(fe:Invoice/cac:InvoiceLine/cac:AllowanceCharge/cbc:Amount), '#,##0.00##'))}"
						total_ibua="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='IBUA']/cbc:Value)}"
						Elaborado_por="{$InfoAdicional[@Name='usuario']/@Value}"
						total2letra_ds="{$InfoAdicional[@Name = 'FacturaLetra_extranjero']/@Value}"
						total_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount}"
						descuentos_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount}"
						impuestos_am="{fe:Invoice/cac:TaxTotal/cbc:TaxAmount}"
						total_ic_am="{$InfoAdicional[@Name='TotalIcaExt']/@Value}"
						total_ipc_am="{$InfoAdicional[@Name='TotalImpExt']/@Value}"
						total_retefuente_am="{$InfoAdicional[@Name='Total_ReteFnt_extranjero']/@Value}"
						total_reteiva_am="{$InfoAdicional[@Name='Total_ReteIVA_extranjero']/@Value}"
						total_reteica_am="{$InfoAdicional[@Name='Total_ReteICA_extranjero']/@Value}"
						textoResolucion="{$InfoAdicional[@Name='textoResolucion']/@Value}"
						firma_digital="{inv:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature/ds:SignatureValue}"
						texto="{$InfoAdicional[@Name='ReferenciaPago']/@Value}">
						<!-- subtotal_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount}"					            -->
						<xsl:attribute name="subtotal_am">
							<!--  SUBTOTAL -->
							<xsl:variable name="subtotbr"
								select="number(fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount)" />
							<!--IVA (si no existe, vale 0) -->
							<xsl:variable name="totdescb">
								<xsl:choose>
									<xsl:when test="sum(fe:Invoice/cac:InvoiceLine/cac:AllowanceCharge/cbc:Amount)">
										<xsl:value-of
											select="sum(fe:Invoice/cac:InvoiceLine/cac:AllowanceCharge/cbc:Amount)" />
									</xsl:when>
									<xsl:otherwise>0</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<!--  TOTAL  -->
							<xsl:value-of select="$subtotbr + $totdescb" />
						</xsl:attribute>
						<!-- <xsl:attribute name="subtotal_am"> -->
						<!--  SUBTOTAL  -->
						<!-- <xsl:variable name="subtotbr" -->
						<!-- select="number(fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount)"/> -->
						<!--	  DESCUENTOS  -->
						<!-- <xsl:variable name="totdescb" -->
						<!-- select="sum(fe:Invoice/cac:InvoiceLine/cac:AllowanceCharge/cbc:Amount)"/> -->
						<!--  TOTAL  -->
						<!-- <xsl:value-of select="format-number($subtotbr + $totdescb, '00.00##')"/> -->
						<!-- </xsl:attribute> -->
						<xsl:attribute name="total_ventaNeta">
							<xsl:variable name="Subto">
								<xsl:choose>
									<xsl:when test="$ForeignMonetaryTotal/dif:LineExtensionAmount = ''">0</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="format-number($ForeignMonetaryTotal/dif:LineExtensionAmount, '0.00##')" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:variable name="desc">
								<xsl:choose>
									<xsl:when test="$ForeignMonetaryTotal/dif:AllowanceTotalAmount = ''">0</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="format-number($ForeignMonetaryTotal/dif:AllowanceTotalAmount, '0.00##')" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:value-of select="format-number($Subto - $desc, '0.00##')" />
						</xsl:attribute>
						<!--<TotalEnLetras>-->
						<xsl:attribute name="total_letras">
							<xsl:call-template name="numero-a-letras">
								<xsl:with-param name="numero"
									select="number(fe:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount)" />
							</xsl:call-template>
						</xsl:attribute>
						<!--</TotalEnLetras>-->
					</Documento>
				</xsl:when>
				<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
					<Documento tipo_cd="01" total_imp_am="{$InfoAdicional[@Name='TotalImpExt']/@Value}"
						numero_cd="{normalize-space(substring-after($numeracionId, $prefijo))}"
						prefijo_cd="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl/sts:AuthorizedInvoices/sts:Prefix}"
						fecha_emision_fe="{fe:Invoice/cbc:IssueDate}" hora_emision_fe="{fe:Invoice/cbc:IssueTime}"
						fecha_vencimiento_fe="{fe:Invoice/cac:PaymentMeans/cbc:PaymentDueDate}"
						hora_documento_dt="{$InfoAdicional[@Name='horaFactura']/@Value}"
						tasaCambio="{$InfoAdicional[@Name='tasaCambio']/@Value}" moneda_cd="{$CurrencyID}"
						comentario_ds="{fe:Invoice/cbc:Note}"
						total_cargos_nogravados="{sum(fe:Invoice/cac:InvoiceLine/cac:WithholdingTaxTotal/cbc:TaxAmount)}"
						total_cargos_gravados="{sum(fe:Invoice/cac:InvoiceLine/cac:TaxTotal/cbc:TaxAmount)}"
						total_cantidad="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='CantUni']/cbc:Value)}"
						total_pacas="{sum(fe:Invoice/cac:InvoiceLine/cbc:InvoicedQuantity)}"
						total_vlr_descuento="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='VDesc']/cbc:Value)}"
						total_ibua="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='IBUA']/cbc:Value)}"
						Elaborado_por="{$InfoAdicional[@Name='usuario']/@Value}"
						total2letra_ds="{$InfoAdicional[@Name = 'FacturaLetra_extranjero']/@Value}"
						total_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount}"
						descuentos_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount}"
						impuestos_am="{fe:Invoice/cac:TaxTotal/cbc:TaxAmount}"
						total_ic_am="{$InfoAdicional[@Name='TotalIcaExt']/@Value}"
						total_ipc_am="{$InfoAdicional[@Name='TotalImpExt']/@Value}"
						subtotal_am="{$ForeignMonetaryTotal/dif:LineExtensionAmount}"
						total_retefuente_am="{$InfoAdicional[@Name='Total_ReteFnt_extranjero']/@Value}"
						total_reteiva_am="{$InfoAdicional[@Name='Total_ReteIVA_extranjero']/@Value}"
						total_reteica_am="{$InfoAdicional[@Name='Total_ReteICA_extranjero']/@Value}"
						textoResolucion="{$InfoAdicional[@Name='textoResolucion']/@Value}"
						texto="{$InfoAdicional[@Name='ReferenciaPago']/@Value}">
						<xsl:attribute name="total_ventaNeta">
							<xsl:variable name="Subto">
								<xsl:choose>
									<xsl:when test="$ForeignMonetaryTotal/dif:LineExtensionAmount = ''">0</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="format-number($ForeignMonetaryTotal/dif:LineExtensionAmount, '0.00##')" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:variable name="desc">
								<xsl:choose>
									<xsl:when test="$ForeignMonetaryTotal/dif:AllowanceTotalAmount = ''">0</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="format-number($ForeignMonetaryTotal/dif:AllowanceTotalAmount, '0.00##')" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:value-of select="$Subto - $desc" />
						</xsl:attribute>
					</Documento>
				</xsl:when>
				<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
					<xsl:variable name="totalIva"
						select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID = '01']/cbc:TaxAmount" />
					<xsl:variable name="totalInc"
						select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID = '04']/cbc:TaxAmount" />
					<xsl:variable name="tasaCambio" select="'1.00'" />
					<xsl:variable name="moneda" select="COP" />
					<Documento tipo_cd="01" total_imp_am="{$totalInc}"
						numero_cd="{normalize-space(substring-after($numeracionId, $prefijo))}"
						prefijo_cd="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:InvoiceControl/sts:AuthorizedInvoices/sts:Prefix}"
						fecha_emision_fe="{fe:Invoice/cbc:IssueDate}"
						fecha_vencimiento_fe="{fe:Invoice/cac:PaymentMeans/cbc:PaymentDueDate}"
						hora_emision_fe="{fe:Invoice/cbc:IssueTime}"
						tasaCambio="{$InfoAdicional[@Name='tasaCambio']/@Value}" moneda_cd="{$CurrencyID}"
						comentario_ds="{fe:Invoice/cbc:Note}"
						total_cargos_nogravados="{sum(fe:Invoice/cac:InvoiceLine/cac:WithholdingTaxTotal/cbc:TaxAmount)}"
						total_cargos_gravados="{sum(fe:Invoice/cac:InvoiceLine/cac:TaxTotal/cbc:TaxAmount)}"
						total_cantidad="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='CantUni']/cbc:Value)}"
						total_pacas="{sum(fe:Invoice/cac:InvoiceLine/cbc:InvoicedQuantity)}"
						total_vlr_descuento="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='VDesc']/cbc:Value)}"
						total_ibua="{sum(fe:Invoice/cac:InvoiceLine/cac:Item/cac:AdditionalItemProperty[cbc:Name='IBUA']/cbc:Value)}"
						Elaborado_por="{$InfoAdicional[@Name='usuario']/@Value}"
						total2letra_ds="{$InfoAdicional[@Name = 'FacturaLetra_extranjero']/@Value}"
						total_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount}"
						descuentos_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount}"
						impuestos_am="{fe:Invoice/cac:TaxTotal/cbc:TaxAmount}"
						total_ic_am="{$InfoAdicional[@Name='TotalIcaExt']/@Value}"
						total_ipc_am="{$InfoAdicional[@Name='TotalImpExt']/@Value}"
						subtotal_am="{fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount}"
						total_retefuente_am="{$InfoAdicional[@Name='Total_ReteFnt']/@Value}"
						total_reteiva_am="{$InfoAdicional[@Name='Total_ReteIVA']/@Value}"
						total_reteica_am="{$InfoAdicional[@Name='Total_ReteICA']/@Value}"
						textoResolucion="{$InfoAdicional[@Name='textoResolucion']/@Value}"
						texto="{$InfoAdicional[@Name='ReferenciaPago']/@Value}">
						<xsl:attribute name="total_ventaNeta">
							<xsl:variable name="Subto">
								<xsl:choose>
									<xsl:when test="fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount = ''">0
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="format-number(fe:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount, '0.00##')" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:variable name="desc">
								<xsl:choose>
									<xsl:when test="fe:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount = ''">0
									</xsl:when>
									<xsl:otherwise>
										<xsl:value-of
											select="fe:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount" />
									</xsl:otherwise>
								</xsl:choose>
							</xsl:variable>
							<xsl:value-of select="format-number($Subto - $desc, '0.00##')" />
						</xsl:attribute>
					</Documento>
				</xsl:when>
			</xsl:choose>
			<Emisor nit_cd="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID}"
				nombre_comercial_temp="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:RegistrationName}"
				telefono_temp="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone}"
				Fax="{$InfoAdicional[@Name = 'EmpFax']/@Value}"
				email_temp="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail}"
				direccion_temp="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cac:AddressLine/cbc:Line}"
				ciudad_temp="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cbc:CountrySubentity}"
				Pais="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cac:Country/cbc:Name}"
				nombre_comercial_ds="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:RegistrationName}"
				nombre_tx="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:RegistrationName}"
				tipo_id_cd="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID/@schemeName}"
				num_identificacion_cd="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID}"
				DigitoVerificacion="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID/@schemeID}"
				direccion_tx="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cac:AddressLine/cbc:Line}"
				telefonos_ds_proximo="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone}"
				pais_nm="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cac:Country/cbc:Name}"
				ciudad_tx="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cbc:CountrySubentity}"
				departamento_nm="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:CityName}"
				telefono_tx="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone}"
				fax_ds="{$InfoAdicional[@Name = 'EmpFax']/@Value}"
				email_tx="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail}"
				TelefonoEmisor="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone}"
				regimen_fiscal_cd="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:TaxLevelCode}" />
			<Receptor nombre_comercial_ds="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Name}"
				nombre_tx="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyName/cbc:Name}"
				regimen_fiscal_cd="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cbc:TaxLevelCode}"
				tipo_id_cd="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID/@schemeName}"
				nit_cd="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID}"
				direccion_tx="{concat(fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PhysicalLocation/cac:Address/cac:AddressLine[2]/cbc:Line, ' ', fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PhysicalLocation/cac:Address/cac:AddressLine/cbc:Line)}"
				direccion="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cac:RegistrationAddress/cac:AddressLine[2]/cbc:Line}"
				pais_nm="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PhysicalLocation/cac:Address/cac:Country/cbc:Name}"
				ciudad_tx="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:CityName}"
				departamento_nm="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PhysicalLocation/cac:Address/cbc:Department}"
				email_recepcion_ds="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:ElectronicMail}">
				<xsl:attribute name="TelefonoReceptor">
					<xsl:choose>
						<xsl:when test="$InfoAdicional[@Name = 'TelefonoReceptor']/@Value != ''">
							<xsl:value-of
								select="fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone" />
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="$InfoAdicional[@Name = 'TelefonoReceptor']/@Value" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</Receptor>
			<xsl:variable name="otrosConceptos">
				<xsl:choose>
					<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos_extranjero']/@Value" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos_extranjero']/@Value" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos']/@Value" />
					</xsl:when>
				</xsl:choose>
			</xsl:variable>
			<xsl:variable name="Anticpo">
				<xsl:choose>
					<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos_extranjero']/@Value" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos_extranjero']/@Value" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
						<xsl:value-of select="$InfoAdicional[@Name = 'OtrosConceptos']/@Value" />
					</xsl:when>
				</xsl:choose>
			</xsl:variable>
			<informacion_adicional Elaborado_por="{$InfoAdicional[@Name = 'ElaboradoPor']/@Value}"
				Vendedor="{$InfoAdicional[@Name = 'vendedornombre']/@Value}"
				textoResolucion="{$InfoAdicional[@Name = 'textoResolucion']/@Value}"
				TelefonoReceptor="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:Contact/cbc:Telephone}"
				ZonaVendedor="{$InfoAdicional[@Name = 'ZonaVendedor']/@Value}"
				Zona="{$InfoAdicional[@Name = 'f_20_00007591_G502_1_ZONA']/@Value}"
				Ruta="{$InfoAdicional[@Name = 'f_20_00007592_G502_1_RUTA']/@Value}"
				NumPedido="{fe:Invoice/cac:OrderReference/cbc:SalesOrderID}"
				NumCargue="{$InfoAdicional[@Name = 'f_cargue']/@Value}"
				LeyendaFactura="{$InfoAdicional[@Name = 'LeyendaFactura']/@Value}"
				ERazonSocial="{$InfoAdicional[@Name='ERazonSocial']/@Value}"
				Enombre_comercial_temp="{$InfoAdicional[@Name='ENombreComercial']/@Value}"
				ECategoriaFiscal="{$InfoAdicional[@Name='ECategoriaFiscal']/@Value}"
				Placa="{$InfoAdicional[@Name = 'Placa']/@Value}"
				EContribuyente="{$InfoAdicional[@Name='EContribuyente']/@Value}"
				ERegimenTributario="{$InfoAdicional[@Name='ERegimenTributario']/@Value}"
				ERetenedorTributario="{$InfoAdicional[@Name='ERetenedorTributario']/@Value}"
				MesesVigencia="{$InfoAdicional[@Name='MesesVigencia']/@Value}"
				LeyendaMandato="{$InfoAdicional[@Name = 'LeyendaMandato']/@Value}"
				TelefonoEmisor="{$InfoAdicional[@Name = 'TelefonoEmisor']/@Value}"
				MonedaFactura="{$InfoAdicional[@Name = 'MonedaFactura']/@Value}"
				FormaPago="{$InfoAdicional[@Name='CPago']/@Value}" Estado="{$InfoAdicional[@Name = 'Estado']/@Value}"
				NoInternoCompleto="{$InfoAdicional[@Name = 'NoInternoCompleto']/@Value}"
				NoInterno="{$InfoAdicional[@Name = 'NoInterno']/@Value}" BU="{$InfoAdicional[@Name = 'BU']/@Value}"
				conBU="{$InfoAdicional[@Name = 'conBU']/@Value}"
				FacturaLetra="{$InfoAdicional[@Name = 'FacturaLetra']/@Value}"
				FacturaLetra_extranjero="{$InfoAdicional[@Name = 'FacturaLetra_extranjero']/@Value}"
				Relacionados="{$InfoAdicional[@Name = 'Relacionados']/@Value}"
				Observaciones="{$InfoAdicional[@Name = 'Observaciones']/@Value}"
				Detalles="{$InfoAdicional[@Name = 'Detalle']/@Value}"
				fechaAnticipo="{$InfoAdicional[@Name = 'fechaAnticipo']/@Value}"
				Plan="{$InfoAdicional[@Name = 'Plan']/@Value}" Folio="{$InfoAdicional[@Name = 'Folio']/@Value}"
				FechaVencimiento="{$InfoAdicional[@Name = 'FechaVencimiento']/@Value}"
				Habitacion="{$InfoAdicional[@Name = 'Habitacion']/@Value}"
				Noches="{$InfoAdicional[@Name = 'Noches']/@Value}" Llegada="{$InfoAdicional[@Name = 'Llegada']/@Value}"
				Salida="{$InfoAdicional[@Name = 'Salida']/@Value}" Caja="{$InfoAdicional[@Name = 'Caja']/@Value}"
				Usuario="{$InfoAdicional[@Name = 'Usuario']/@Value}" Ident="{$InfoAdicional[@Name = 'Ident']/@Value}"
				Compania="{$InfoAdicional[@Name = 'Compania']/@Value}"
				NumeroPersonas="{$InfoAdicional[@Name = 'NumeroPersonas']/@Value}"
				Paidout="{$InfoAdicional[@Name = 'Paidout']/@Value}"
				Payment="{$InfoAdicional[@Name = 'Payment']/@Value}"
				Propina="{$InfoAdicional[@Name = 'Propina']/@Value}"
				TotalAbonosyPagos="{$InfoAdicional[@Name = 'TotalAbonosyPagos']/@Value}"
				BaseRetencion="{$InfoAdicional[@Name = 'BaseRetencion']/@Value}"
				DireccionZeus="{$InfoAdicional[@Name = 'DireccionZeus']/@Value}"
				ConmutadorEmisor="{$InfoAdicional[@Name = 'ConmutadorEmisor']/@Value}"
				FaxEmisor="{$InfoAdicional[@Name = 'FaxEmisor']/@Value}"
				ReservasEmisor="{$InfoAdicional[@Name = 'ReservasEmisor']/@Value}"
				UbicacionEmisor="{$InfoAdicional[@Name = 'UbicacionEmisor']/@Value}"
				DireccionOficinas="{$InfoAdicional[@Name = 'DireccionOficinas']/@Value}"
				ResumenCargo="{$InfoAdicional[@Name = 'ResumenCargo']/@Value}"
				TelOficinas="{$InfoAdicional[@Name = 'TelOficinas']/@Value}"
				FaxOficinas="{$InfoAdicional[@Name = 'FaxOficinas']/@Value}"
				UbicacionOficinas="{$InfoAdicional[@Name = 'UbicacionOficinas']/@Value}"
				OC="{$InfoAdicional[@Name = 'OCompra']/@Value}" Codigo="{$InfoAdicional[@Name = 'Codigo']/@Value}"
				Consecutivo="{$InfoAdicional[@Name = 'Consecutivo']/@Value}"
				codigoBarra="{$InfoAdicional[@Name = 'codigoBarra']/@Value}"
				total_reteiCa_am="{$InfoAdicional[@Name = 'Total_ReteICA_extranjero']/@Value}"
				total_reteiva_am="{$InfoAdicional[@Name = 'Total_ReteIVA_extranjero']/@Value}"
				Total_Concepto="{$InfoAdicional[@Name = 'Total_Concepto_extranjero']/@Value}"
				TotalKilos="{$InfoAdicional[@Name = 'TotalKilos_extranjero']/@Value}"
				Total_Facturado="{$InfoAdicional[@Name = 'Total_Facturado_extranjero']/@Value}"
				OtrosConceptos="{$otrosConceptos}" Anticipo="{$Anticpo}"
				ValorPagar="{$InfoAdicional[@Name = 'ValorPagar_extranjero']/@Value}"
				ValorLetras="{$InfoAdicional[@Name = 'VLetras']/@Value}"
				VehiculoCargue="{$InfoAdicional[@Name = 'f_vehiculo_cargue']/@Value}" />
			<xsl:variable name="cuota">
				<xsl:choose>
					<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
						<xsl:value-of select="'ValueQuotaExtranjero'" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
						<xsl:value-of select="'ValueQuotaExtranjero'" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
						<xsl:value-of select="'ValueQuota'" />
					</xsl:when>
				</xsl:choose>
			</xsl:variable>
			<xsl:variable name="balance">
				<xsl:choose>
					<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
						<xsl:value-of select="'BalanceExtranjero'" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
						<xsl:value-of select="'BalanceExtranjero'" />
					</xsl:when>
					<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
						<xsl:value-of select="'Balance'" />
					</xsl:when>
				</xsl:choose>
			</xsl:variable>
			<xsl:for-each select="$InfoAdicionalRows">
				<xsl:if test="dif:CustomField/@Category = 'Cuota'">
					<cuota NumberQuota="{dif:CustomField[@Name='NumberQuota']/@Value}"
						ExpirationDate="{dif:CustomField[@Name='ExpirationDate']/@Value}"
						ValueQuota="{dif:CustomField[@Name=$cuota]/@Value}"
						Balance="{dif:CustomField[@Name=$balance]/@Value}" />
				</xsl:if>
			</xsl:for-each>
			<xsl:for-each select="fe:Invoice/cac:InvoiceLine">
				<xsl:variable name="IvaItem"
					select="cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID = '01' ]/cbc:TaxAmount[string(number())!='NaN']" />
				<xsl:variable name="Subtotal" select="cbc:LineExtensionAmount[string(number())!='NaN']" />
				<xsl:variable name="ForeignMonetary"
					select="/fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/dif:ForeignCurrencyExtension/dif:ForeignCurrency/dif:Line" />
				<xsl:variable name="PosicionActual" select="position()" />
				<xsl:variable name="tiq"
					select="cac:Item/cac:AdditionalItemProperty[cbc:Name='tiqueteItem']/cbc:Value" />
				<xsl:variable name="bod"
					select="cac:Item/cac:AdditionalItemProperty[cbc:Name='bodegaItem']/cbc:Value" />
				<xsl:variable name="lot" select="cac:Item/cac:AdditionalItemProperty[cbc:Name='loteItem']/cbc:Value" />
				<xsl:variable name="preciounitario">
					<xsl:choose>
						<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
							<xsl:value-of select="$ForeignMonetary[$PosicionActual]/dif:PriceAmount" />
						</xsl:when>
						<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
							<xsl:value-of select="$ForeignMonetary[$PosicionActual]/dif:PriceAmount" />
						</xsl:when>
						<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
							<xsl:value-of select="cac:Price/cbc:PriceAmount" />
						</xsl:when>
					</xsl:choose>
				</xsl:variable>
				<xsl:variable name="itemSubtotal">
					<xsl:choose>
						<xsl:when test="not($InfoAdicional[@Name = 'Pesos']/@Value)">
							<xsl:value-of select="$ForeignMonetary[$PosicionActual]/dif:LineExtensionAmount" />
						</xsl:when>
						<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = ''">
							<xsl:value-of select="$ForeignMonetary[$PosicionActual]/dif:LineExtensionAmount" />
						</xsl:when>
						<xsl:when test="$InfoAdicional[@Name = 'Pesos']/@Value = 'Pesos'">
							<xsl:value-of select="cbc:LineExtensionAmount[string(number())!='NaN']" />
						</xsl:when>
					</xsl:choose>
				</xsl:variable>
				<Detalle linea_nu="{position()}" tiquete="{cac:Item/cac:SellersItemIdentification/cbc:ID}"
					bodega="{cac:Item/cac:AdditionalItemProperty[cbc:Name='bodega']/cbc:Value}"
					lote="{cac:Item/cac:ItemInstance/cac:LotIdentification/cbc:LotNumberID}"
					codigo_cd="{cac:Item/cac:StandardItemIdentification/cbc:ID}"
					descripcion_tx="{cac:Item/cbc:Description}"
					bodeganombre="{cac:Item/cac:AdditionalItemProperty[cbc:Name='bodeganombre']/cbc:Value}"
					descuento_am="{cbc:AllowanceChargeAmount/cbc:Amount}"
					vlr_descuento="{concat('$ ', format-number(sum((cac:AllowanceCharge/cbc:Amount)), '#,##0.00##'))}"
					cantidad_unit="{cac:Item/cac:AdditionalItemProperty[cbc:Name='CantUni']/cbc:Value}"
					ibua="{cac:Item/cac:AdditionalItemProperty[cbc:Name='IBUA']/cbc:Value}"
					remesa="{cac:Item/cac:AdditionalItemProperty[cbc:Name='remesa']/cbc:Value}"
					origen="{cac:Item/cac:AdditionalItemProperty[cbc:Name='origen']/cbc:Value}"
					peso="{cac:Item/cac:AdditionalItemProperty[cbc:Name='peso']/cbc:Value}"
					fecha="{cac:Item/cac:ItemInstance/cac:LotIdentification/cbc:ExpiryDate}"
					destino="{cac:Item/cac:AdditionalItemProperty[cbc:Name='destino']/cbc:Value}"
					cantidad_nu="{cbc:InvoicedQuantity}"
					total_linea_am="{sum(cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='01']/cbc:TaxAmount) + sum(cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='02']/cbc:TaxAmount) + cbc:LineExtensionAmount}"
					abono="{cac:Item/cac:AdditionalItemProperty[cbc:Name='Abono']/cbc:Value}"
					precio_unitario_am="{$preciounitario}"
					porcentajeDescuento="{cac:Item/cac:AdditionalItemProperty[cbc:Name='porc_descuento_extranjero']/cbc:Value}"
					impuesto_tasa_nu="{cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:Percent}"
					item_iva_am="{concat('$ ', format-number(sum(cac:TaxTotal/cbc:TaxAmount), '#,##0.00##'))}"
					porcentaje_imp_num="{$ForeignMonetary[$PosicionActual]/dif:TaxPercentageImp}"
					item_imp_am="{$ForeignMonetary[$PosicionActual]/dif:TotalTaxImp}"
					porcentaje_ica_num="{$ForeignMonetary[$PosicionActual]/dif:TaxPercentageIca}"
					item_ica_am="{$ForeignMonetary[$PosicionActual]/dif:TotalTaxIca}">
					<!-- item_total_am="{cac:Item/cac:AdditionalItemProperty[cbc:Name='ValorTotalItemExtranjero']/cbc:Value}" -->
					<!-- <xsl:attribute name="item_total_am">  -->
					<!-- <xsl:variable name="subtot">  -->
					<!-- <xsl:choose>  -->
					<!-- <xsl:when test="cbc:LineExtensionAmount = ''">0</xsl:when>  -->
					<!-- <xsl:otherwise>  -->
					<!-- <xsl:value-of select="format-number(cbc:LineExtensionAmount, '0.00##')"/>  -->
					<!-- </xsl:otherwise>  -->
					<!-- </xsl:choose>  -->
					<!-- </xsl:variable>  -->
					<!-- <xsl:variable name="totiva">  -->
					<!-- <xsl:choose>  -->
					<!-- <xsl:when test="cac:TaxTotal/cbc:TaxAmount = ''">0</xsl:when>  -->
					<!-- <xsl:otherwise>  -->
					<!-- <xsl:value-of select="format-number(cac:TaxTotal/cbc:TaxAmount, '0.00##')"/>  -->
					<!-- </xsl:otherwise>  -->
					<!-- </xsl:choose>  -->
					<!-- </xsl:variable>  -->
					<!-- <xsl:value-of select="format-number($subtot + $totiva, '0.00##')"/>  -->
					<!-- </xsl:attribute>   -->
					<!-- unidad_cd="{cac:Item/cac:AdditionalItemProperty[cbc:Name='UMedida']/cbc:Value | 'PAC'[not(cac:Item/cac:AdditionalItemProperty[cbc:Name='UMedida']/cbc:Value)]}" -->
					<xsl:attribute name="unidad_medida_cd">
						<xsl:choose>
							<xsl:when
								test="normalize-space(cac:Item/cac:AdditionalItemProperty[cbc:Name='f_um_base']/cbc:Value) != ''">
								<xsl:value-of
									select="cac:Item/cac:AdditionalItemProperty[cbc:Name='f_um_base']/cbc:Value" />
							</xsl:when>
							<xsl:when
								test="normalize-space(cac:Item/cac:AdditionalItemProperty[cbc:Name='UMedida']/cbc:Value) != ''">
								<xsl:value-of
									select="cac:Item/cac:AdditionalItemProperty[cbc:Name='UMedida']/cbc:Value" />
							</xsl:when>
							<xsl:otherwise>PAC</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<!-- tventa="{cac:Item/cac:AdditionalItemProperty[cbc:Name='TVenta']/cbc:Value}"				          -->
					<xsl:attribute name="tventa">
						<xsl:choose>
							<xsl:when
								test="normalize-space(cac:Item/cac:AdditionalItemProperty[cbc:Name='TVenta']/cbc:Value) != ''">
								<xsl:value-of
									select="cac:Item/cac:AdditionalItemProperty[cbc:Name='TVenta']/cbc:Value" />
							</xsl:when>
							<xsl:otherwise>Venta</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<!-- item_subtotal_am="{cbc:LineExtensionAmount}" -->
					<xsl:attribute name="item_subtotal_am">
						<!-- SUBTOTAL -->
						<xsl:variable name="subtotb" select="number(cbc:LineExtensionAmount)" />
						<!-- IVA (si no existe, vale 0) -->
						<xsl:variable name="totdesc">
							<xsl:choose>
								<xsl:when test="cac:AllowanceCharge/cbc:Amount">
									<xsl:value-of select="number(cac:AllowanceCharge/cbc:Amount)" />
								</xsl:when>
								<xsl:otherwise>0</xsl:otherwise>
							</xsl:choose>
						</xsl:variable>
						<!-- TOTAL -->
						<xsl:value-of select="$subtotb + $totdesc" />
					</xsl:attribute>
					<xsl:attribute name="item_total_am">
						<!-- SUBTOTAL -->
						<xsl:variable name="subtot" select="number(cbc:LineExtensionAmount)" />
						<!-- IVA (si no existe, vale 0) -->
						<xsl:variable name="totiva">
							<xsl:choose>
								<xsl:when test="cac:TaxTotal/cbc:TaxAmount">
									<xsl:value-of select="number(cac:TaxTotal/cbc:TaxAmount)" />
								</xsl:when>
								<xsl:otherwise>0</xsl:otherwise>
							</xsl:choose>
						</xsl:variable>
						<!-- TOTAL -->
						<xsl:value-of select="$subtot + $totiva" />
					</xsl:attribute>
					<!-- <xsl:attribute name="tiquete"> -->
					<!-- <xsl:choose> -->
					<!-- <xsl:when test="$tiq !=''"> -->
					<!-- <xsl:value-of select="$tiq"/> -->
					<!-- </xsl:when> -->
					<!-- <xsl:otherwise> -->
					<!-- <xsl:value-of select="cac:Item/cac:AdditionalItemProperty[cbc:Name='tiquete']/cbc:Value"/> -->
					<!-- </xsl:otherwise> -->
					<!-- </xsl:choose> -->
					<!-- </xsl:attribute> -->
					<xsl:attribute name="bodega">
						<xsl:choose>
							<xsl:when test="$bod !=''">
								<xsl:value-of select="$bod" />
							</xsl:when>
							<xsl:otherwise>
								<xsl:value-of
									select="cac:Item/cac:AdditionalItemProperty[cbc:Name='bodega']/cbc:Value" />
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<!-- <xsl:attribute name="lote"> -->
					<!-- <xsl:choose> -->
					<!-- <xsl:when test="$lot !=''"> -->
					<!-- <xsl:value-of select="$lot"/> -->
					<!-- </xsl:when> -->
					<!-- <xsl:otherwise> -->
					<!-- <xsl:value-of select="cac:Item/cac:AdditionalItemProperty[cbc:Name='lote']/cbc:Value"/> -->
					<!-- </xsl:otherwise> -->
					<!-- </xsl:choose> -->
					<!-- </xsl:attribute> -->
				</Detalle>
			</xsl:for-each>
			<xsl:for-each select="$InfoAdicionalRows">
				<xsl:if test="dif:CustomField/@Category = 'Impuestos'  ">
					<ResumenImpuestos>
						<xsl:attribute name="porcentajeIva">
							<xsl:choose>
								<xsl:when test="dif:CustomField[@Name='Codigo']/@Value = '01'">
									<xsl:value-of select="concat(dif:CustomField[@Name='porcentaje']/@Value,' %')" />
								</xsl:when>
								<xsl:otherwise />
							</xsl:choose>
						</xsl:attribute>
						<xsl:attribute name="valorIva">
							<xsl:choose>
								<xsl:when test="dif:CustomField[@Name='Codigo']/@Value = '01'">
									<xsl:value-of select="dif:CustomField[@Name='Valor']/@Value" />
								</xsl:when>
								<xsl:otherwise />
							</xsl:choose>
						</xsl:attribute>
						<xsl:attribute name="porcentajeInc">
							<xsl:choose>
								<xsl:when
									test="dif:CustomField[@Name='Codigo']/@Value = '04' or dif:CustomField[@Name='Codigo']/@Value = '02' ">
									<xsl:value-of select="concat(dif:CustomField[@Name='porcentaje']/@Value,' %')" />
								</xsl:when>
								<xsl:otherwise />
							</xsl:choose>
						</xsl:attribute>
						<xsl:attribute name="valorInc">
							<xsl:choose>
								<xsl:when
									test="dif:CustomField[@Name='Codigo']/@Value = '04' or dif:CustomField[@Name='Codigo']/@Value = '02'">
									<xsl:value-of select="dif:CustomField[@Name='Valor']/@Value" />
								</xsl:when>
								<xsl:otherwise />
							</xsl:choose>
						</xsl:attribute>
					</ResumenImpuestos>
				</xsl:if>
			</xsl:for-each>
			<ResumenImpuestos porcentajeIva="" valorIva="" porcentajeInc="" valorInc="">

			</ResumenImpuestos>
			<xsl:variable name="ValIva">
				<xsl:choose>
					<xsl:when
						test="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='01']/cbc:TaxAmount">
						<xsl:variable name="elIva"
							select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='01']/cbc:TaxAmount" />
						<xsl:if test="$elIva != ''">
							<xsl:value-of
								select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='01']/cbc:TaxAmount" />
						</xsl:if>
						<xsl:if test="$elIva = ''">0.00</xsl:if>
					</xsl:when>
					<xsl:otherwise>0.00</xsl:otherwise>
				</xsl:choose>
			</xsl:variable>
			<xsl:variable name="ValOtroIm">
				<xsl:variable name="elIca">
					<xsl:choose>
						<xsl:when
							test="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='04']/cbc:TaxAmount = ''">
							0</xsl:when>
						<xsl:otherwise>
							<xsl:value-of
								select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='04']/cbc:TaxAmount" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:variable name="elImp">
					<xsl:choose>
						<xsl:when
							test="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='03']/cbc:TaxAmount = ''">
							0</xsl:when>
						<xsl:otherwise>
							<xsl:value-of
								select="fe:Invoice/cac:TaxTotal[cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID='03']/cbc:TaxAmount" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
				<xsl:if test="$elIca !='' and $elImp !=''">
					<xsl:value-of select="$elImp+$elIca" />
				</xsl:if>
				<xsl:if test="$elIca ='' and $elImp =''">0.00</xsl:if>
				<xsl:if test="$elIca ='' and $elImp !=''">
					<xsl:value-of select="$elImp" />
				</xsl:if>
				<xsl:if test="$elIca !='' and $elImp =''">
					<xsl:value-of select="$elIca" />
				</xsl:if>
			</xsl:variable>
			<QR NumFac="{fe:Invoice/cbc:ID}" FecFac="{translate(fe:Invoice/cbc:IssueDate, '-','')}"
				HorFac="{fe:Invoice/cbc:IssueTime}"
				NitFac="{fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID}"
				DocAdq="{fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID}"
				ValFac="{$LineExtensionAmount}" ValIva="{$ValIva}" ValOtroIm="{$ValOtroIm}" ValTolFac="{$PayableAmount}"
				cufe_cd="{fe:Invoice/cbc:UUID}"
				qr_base64="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:QRCode}"
				QRText="{concat(
		 
		 'NumFac: ',
		 fe:Invoice/cbc:ID,
		 
		 '&#10;FecFac: ',
		 translate(fe:Invoice/cbc:IssueDate, '-',''),
		 
		 '&#10;HorFac: ',
		 fe:Invoice/cbc:IssueTime,
		 
		 '&#10;NitFac: ',
		fe:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID,
		
		
		'&#10;DocAdq: ',
		fe:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID,
		
		'&#10;ValFac: ',
		$LineExtensionAmount,
		
		'&#10;ValIva: ',
		$ValIva,
		
		'&#10;ValOtroIm: ',
		$ValOtroIm,
		
		'&#10;ValTolFac: ',
		$PayableAmount,
		
		'&#10;CUFE: ',
		fe:Invoice/cbc:UUID,
		
		'&#10;QRCode: ',
		fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sts:DianExtensions/sts:QRCode
	 
		 )}">

			</QR>
			<InforAdicional hora="{fe:Invoice/cbc:IssueTime}">
				<xsl:attribute name="forma_pago">
					<xsl:choose>
						<xsl:when test="fe:Invoice/cac:PaymentMeans/cbc:ID = '1'">Contado</xsl:when>
						<xsl:otherwise>CrÃ©dito</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</InforAdicional>
			<MedioDePago descripcion="" />
		</CFD>
		<Adicional forma_pago_cd="{fe:Invoice/cac:PaymentMeans/cbc:PaymentMeansCode/@name}"
			notas_tx="{fe:Invoice/cbc:Note}">
			<!-- Campos personalizados del cliente (dedup: campos repetidos se agrupan en un solo Campo) -->
			<xsl:for-each select="$InfoAdicional">
				<xsl:if test="generate-id() = generate-id($InfoAdicional[@Name = current()/@Name][1])">
					<Campo clave="{@Name}">
						<xsl:attribute name="valor">
							<xsl:for-each select="$InfoAdicional[@Name = current()/@Name]">
								<xsl:if test="position() > 1">
									<xsl:text> </xsl:text>
								</xsl:if>
								<xsl:value-of select="@Value" />
							</xsl:for-each>
						</xsl:attribute>
					</Campo>
				</xsl:if>
			</xsl:for-each>
			<!-- Condicion de pago segun cac:PaymentMeans/cbc:ID del ERP (1=Contado, 2=Credito);
				       OJO: cbc:PaymentMeansCode es el catalogo DIAN del MEDIO de pago, no la condicion -->
			<Campo clave="f_condicion_pago" valor="{fe:Invoice/cac:PaymentMeans/cbc:ID}" />
			<!-- Orden de compra: cac:OrderReference/cbc:ID -->
			<Campo clave="f_orden_compra" valor="{fe:Invoice/cac:OrderReference/cbc:ID}" />
			<!-- Numero de pedido: cac:OrderReference/cbc:SalesOrderID -->
			<Campo clave="NumPedido" valor="{fe:Invoice/cac:OrderReference/cbc:SalesOrderID}" />
			<!-- Fecha firma DIAN: ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningTime -->
			<Campo clave="f_fecha_dian"
				valor="{fe:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningTime}" />
			<!-- Factores de empaque por item (nuevo formato: AdditionalItemProperty dentro de cada InvoiceLine) -->
			<xsl:for-each select="fe:Invoice/cac:InvoiceLine">
				<Campo clave="f_factor_emp_{position()}"
					valor="{cac:Item/cac:AdditionalItemProperty[cbc:Name='f_factor_emp']/cbc:Value}" />
				<Campo clave="f_um_emp_{position()}"
					valor="{cac:Item/cac:AdditionalItemProperty[cbc:Name='f_um_emp']/cbc:Value}" />
				<Campo clave="f_factor_empaque_{position()}"
					valor="{cac:Item/cac:AdditionalItemProperty[cbc:Name='f_factor_empaque']/cbc:Value}" />
				<Campo clave="f_motivo_{position()}"
					valor="{cac:Item/cac:AdditionalItemProperty[cbc:Name='f_motivo']/cbc:Value}" />
				<Campo clave="f_codigo_barra_principal_{position()}"
					valor="{cac:Item/cac:AdditionalItemProperty[cbc:Name='f_cod_barras_movto']/cbc:Value}" />
			</xsl:for-each>
			<!-- CustomFields requeridos por template FacturaUbl.cs -->
			<Campo clave="LogoBase64">
				<xsl:attribute name="valor">
					<xsl:text>data:image/png;base64,</xsl:text>
					<xsl:value-of select="$LOGO_BASE64" />
				</xsl:attribute>
			</Campo>
			<Campo clave="ResolucionTexto"
				valor="AGENTE RETENEDOR DE IVA -  SOMOS AUTORRETENEDORES  DE RENTA SEGUN RESOLUCIÓN No 13834 del 20 de Noviembre de 2007 - GRAN CONTRIBUYENTE RESOLUCIÓN No  000200 del 27 de Diciembre de 2024 Autorización Numeración de Facturación No. 18764096091725 Numeración: AUTORIZADA Rango desde: PFUA1 hasta: PFUA5000000 Vigencia desde: 25/07/2025 hasta: 25/07/2027 - 24 Meses" />
			<Campo clave="ValorLetras">
				<xsl:attribute name="valor">
					<xsl:value-of select="fe:Invoice/cbc:Note[2]" />
				</xsl:attribute>
			</Campo>
		</Adicional>
	</xsl:template>
</xsl:stylesheet>