namespace PdfQuickDebug.Designer.Model;

/// <summary>
/// Expresión calculada simple. Operaciones soportadas por ExpressionEvaluator:
/// concat, add, sub, mul, div, if, upper, lower.
/// </summary>
public sealed class ExpressionDesign
{
    public string Op { get; set; } = "concat";
    public List<OperandDesign> Args { get; set; } = new();
}

/// <summary>Operando: literal, XPath o sub-expresión anidada.</summary>
public sealed class OperandDesign
{
    public string? Xpath { get; set; }
    public string? Value { get; set; }
    public ExpressionDesign? Expression { get; set; }
}
