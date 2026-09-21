using System.Xml.XPath;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using QuestPDF.Infrastructure;

namespace PdfQuickDebug.Designer.Rendering;

/// <summary>Contexto de render: resolver, evaluador, diseño y nodo actual.</summary>
public sealed class RenderContext
{
    public XmlDataResolver Resolver { get; }
    public ExpressionEvaluator Evaluator { get; }
    public ReportDesign Design { get; }
    public XPathNavigator? Current { get; }
    public float ContentWidth { get; }
    public int Depth { get; }

    public RenderContext(
        XmlDataResolver resolver,
        ExpressionEvaluator evaluator,
        ReportDesign design,
        XPathNavigator? current,
        float contentWidth,
        int depth = 0)
    {
        Resolver = resolver;
        Evaluator = evaluator;
        Design = design;
        Current = current;
        ContentWidth = contentWidth;
        Depth = depth;
    }

    public RenderContext With(XPathNavigator? current, int depth)
        => new(Resolver, Evaluator, Design, current, ContentWidth, depth);
}
