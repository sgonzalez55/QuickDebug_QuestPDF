using System.Xml.XPath;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PdfQuickDebug.Designer.Rendering;

/// <summary>
/// Compone el documento QuestPDF a partir del árbol compositivo del diseño.
/// Cada <see cref="LayoutNode"/> mapea 1:1 a la API fluida de QuestPDF
/// (Column/Row/Table/Container/…). No hay posicionamiento absoluto.
/// </summary>
public sealed class DesignDocumentRenderer : IDocument
{
    private readonly ReportDesign _design;
    private readonly XmlDataResolver _resolver;
    private readonly ExpressionEvaluator _evaluator;
    private readonly Action<string>? _log;

    public DesignDocumentRenderer(ReportDesign design, XmlDataResolver resolver, Action<string>? log = null)
    {
        _design = design ?? throw new ArgumentNullException(nameof(design));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _evaluator = new ExpressionEvaluator(resolver);
        _log = log;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            ApplyPage(page);
            page.DefaultTextStyle(x => x
                .FontSize(_design.Page.DefaultFontSize)
                .FontFamily(_design.Page.FontFamily)
                .FontColor("#000000"));

            var ctx = new RenderContext(_resolver, _evaluator, _design, null, ContentWidth, 0);

            if (_design.Header.Children.Count > 0)
                page.Header().Column(c => ComposeChildren(c, _design.Header, ctx));

            if (_design.Footer.Children.Count > 0)
                page.Footer().Column(c => ComposeChildren(c, _design.Footer, ctx));

            page.Content().Column(c => ComposeChildren(c, _design.Content, ctx));
        });
    }

    /// <summary>Compone los hijos de un nodo raíz directamente en la columna del slot.</summary>
    private void ComposeChildren(ColumnDescriptor col, LayoutNode root, RenderContext ctx)
    {
        foreach (var child in root.Children)
            col.Item().Element(inner => ComposeNode(inner, child, ctx));
    }

    /* ── Despacho por tipo de nodo ─────────────────────────── */

    private void ComposeNode(IContainer container, LayoutNode node, RenderContext ctx)
    {
        if (!node.Visible) return;

        switch (node.Type)
        {
            case "column": ComposeColumn(container, node, ctx); break;
            case "row": ComposeRow(container, node, ctx); break;
            case "container": ComposeContainer(container, node, ctx); break;
            case "table": ComposeTable(container, node, ctx); break;
            case "spacer": ApplyBox(container, node).Height(node.Height ?? node.SpacerHeight); break;
            case "pageBreak": container.PageBreak(); break;
            case "text": ComposeText(container, node, ctx); break;
            case "field": ComposeField(container, node, ctx); break;
            case "image": ComposeImage(container, node, ctx); break;
            case "barcode": ComposeBarcode(container, node, ctx); break;
            case "line": ComposeLine(container, node); break;
            default:
                _log?.Invoke($"[DesignDocumentRenderer] Nodo desconocido '{node.Type}'.");
                break;
        }
    }

    private void ComposeColumn(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var box = ApplyBox(container, node);
        box.Column(col =>
        {
            foreach (var child in node.Children)
                col.Item().Element(inner => ComposeNode(inner, child, ctx));
        });
    }

    private void ComposeRow(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var box = ApplyBox(container, node);
        box.Row(row =>
        {
            foreach (var child in node.Children)
            {
                var item = child.Width is > 0
                    ? row.ConstantItem(child.Width.Value)
                    : row.RelativeItem(child.Weight ?? 1f);
                item.Element(inner => ComposeNode(inner, child, ctx));
            }
        });
    }

    private void ComposeContainer(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var box = ApplyBox(container, node);
        if (node.Height is > 0) box = box.Height(node.Height.Value);
        if (node.Width is > 0) box = box.Width(node.Width.Value);

        box.Column(col =>
        {
            foreach (var child in node.Children)
                col.Item().Element(inner => ComposeNode(inner, child, ctx));
        });
    }

    private void ComposeTable(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var rows = ResolveNodes(ctx, node.RepeatXPath);
        var box = ApplyBox(container, node);

        box.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                foreach (var column in node.Columns)
                {
                    if (column.Width is > 0) cd.ConstantColumn(column.Width.Value);
                    else cd.RelativeColumn(column.Relative ?? 1f);
                }
            });

            if (node.ShowHeader)
            {
                t.Header(h =>
                {
                    foreach (var column in node.Columns)
                        h.Cell().Element(cell => RenderTableCell(cell, column.Header, column.Style));
                });
            }

            foreach (var row in rows)
            {
                foreach (var column in node.Columns)
                {
                    var value = _resolver.ResolveString(column.Binding?.Xpath, row, column.Binding?.Default ?? string.Empty);
                    value = Formatting.Apply(value, column.Format);
                    t.Cell().Element(cell => RenderTableCell(cell, value, column.Style));
                }
            }
        });
    }

    private void RenderTableCell(IContainer cell, string? value, StyleDesign? style)
    {
        var box = StyleApplicator.ApplyBox(cell.Padding(3), style, null);
        var span = box.Text(value ?? string.Empty);
        StyleApplicator.ApplyText(span, style, _design.Page.DefaultFontSize, _design.Page.FontFamily);
    }

    /* ── Hojas ─────────────────────────────────────────────── */

    private void ComposeText(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var box = ApplyBox(container, node);
        var span = box.Text(node.Text ?? string.Empty);
        StyleApplicator.ApplyText(span, node.Style, _design.Page.DefaultFontSize, _design.Page.FontFamily);
    }

    private void ComposeField(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var value = node.Expression != null
            ? ctx.Evaluator.Evaluate(node.Expression, ctx.Current)
            : ctx.Resolver.ResolveString(node.Binding?.Xpath, ctx.Current, node.Binding?.Default ?? string.Empty);
        value = Formatting.Apply(value, node.Format);

        var box = ApplyBox(container, node);
        var span = box.Text(value ?? string.Empty);
        StyleApplicator.ApplyText(span, node.Style, _design.Page.DefaultFontSize, _design.Page.FontFamily);
    }

    private void ComposeImage(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var source = node.Source;
        if (string.IsNullOrWhiteSpace(source) && node.Binding != null)
            source = ctx.Resolver.ResolveString(node.Binding.Xpath, ctx.Current, string.Empty);

        var bytes = ImageSource.Load(source);
        if (bytes == null)
        {
            container.Text(string.Empty);
            return;
        }

        var box = ApplyBox(container, node);
        if (node.Height is > 0) box = box.Height(node.Height.Value);
        if (node.Width is > 0) box = box.Width(node.Width.Value);
        box.Image(bytes).FitArea();
    }

    private void ComposeBarcode(IContainer container, LayoutNode node, RenderContext ctx)
    {
        var value = ctx.Resolver.ResolveString(node.Binding?.Xpath, ctx.Current, node.Binding?.Default ?? string.Empty);
        var bytes = BarcodeRenderer.Render(value, node.Symbology ?? "CODE_128", (int)(node.Height ?? 44f));

        var box = ApplyBox(container, node);
        if (bytes == null)
        {
            box.Text(string.Empty);
            return;
        }
        box.Image(bytes).FitArea();
    }

    private void ComposeLine(IContainer container, LayoutNode node)
    {
        var thickness = node.Thickness <= 0 ? 1f : node.Thickness;
        var color = node.Style?.Color ?? "#000000";
        var box = ApplyBox(container, node);

        if (string.Equals(node.Direction, "vertical", StringComparison.OrdinalIgnoreCase))
            box.AlignCenter().Width(thickness).Background(color);
        else
            box.AlignMiddle().Height(thickness).Background(color);
    }

    /* ── Helpers ───────────────────────────────────────────── */

    private static IContainer ApplyBox(IContainer container, LayoutNode node)
    {
        var p = node.Padding;
        if (p.Top > 0) container = container.PaddingTop(p.Top);
        if (p.Right > 0) container = container.PaddingRight(p.Right);
        if (p.Bottom > 0) container = container.PaddingBottom(p.Bottom);
        if (p.Left > 0) container = container.PaddingLeft(p.Left);

        if (!string.IsNullOrWhiteSpace(node.Background)) container = container.Background(node.Background);
        if (!string.IsNullOrWhiteSpace(node.BorderColor) && (node.BorderWidth ?? 0.5f) > 0)
            container = container.Border(node.BorderWidth!.Value).BorderColor(node.BorderColor);

        var style = node.Style;
        if (style?.VerticalAlign != null)
            container = style.VerticalAlign.ToLowerInvariant() switch
            {
                "middle" => container.AlignMiddle(),
                "bottom" => container.AlignBottom(),
                _ => container.AlignTop()
            };
        if (style?.Align != null)
            container = style.Align.ToLowerInvariant() switch
            {
                "center" => container.AlignCenter(),
                "right" => container.AlignRight(),
                _ => container.AlignLeft()
            };

        return container;
    }

    private IReadOnlyList<XPathNavigator> ResolveNodes(RenderContext ctx, string? xpath)
        => string.IsNullOrWhiteSpace(xpath)
            ? Array.Empty<XPathNavigator>()
            : ctx.Resolver.ResolveNodes(xpath, ctx.Current);

    private void ApplyPage(PageDescriptor page)
    {
        var p = _design.Page;

        if (p.Width > 0 && p.Height > 0)
        {
            page.Size(p.Width, p.Height, Unit.Point);
        }
        else
        {
            var size = p.Size?.ToUpperInvariant() switch
            {
                "A4" => PageSizes.A4,
                "A5" => PageSizes.A5,
                _ => PageSizes.Letter
            };
            page.Size(string.Equals(p.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase)
                ? size.Landscape()
                : size.Portrait());
        }

        var margins = p.Margins ?? new MarginDesign();
        page.MarginTop(margins.Top);
        page.MarginRight(margins.Right);
        page.MarginBottom(margins.Bottom);
        page.MarginLeft(margins.Left);
    }

    private float ContentWidth
    {
        get
        {
            var p = _design.Page;
            var (width, _) = PageDimensions(p.Size, p.Orientation);
            var pageWidth = p.Width > 0 ? p.Width : width;
            var margins = p.Margins ?? new MarginDesign();
            return pageWidth - margins.Left - margins.Right;
        }
    }

    private static (float Width, float Height) PageDimensions(string size, string orientation)
    {
        var (width, height) = size?.ToUpperInvariant() switch
        {
            "A4" => (595.28f, 841.89f),
            "A5" => (419.53f, 595.28f),
            _ => (612f, 792f)
        };
        if (string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            (width, height) = (height, width);
        return (width, height);
    }
}
