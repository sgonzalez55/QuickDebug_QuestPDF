using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Services.PdfGenerator.Domain.Models;
using System;


namespace Services.PdfGenerator.Infrastructure.Templates._830081407;

public class DummyUbl : IInvoiceTemplate
{
    public TemplateMetadata GetMetadata() => new()
    {
        Nit = "830081407",
        TemplateName = "dummy-ubl",
        ClientName = "Dummy test client",
        Version = "1.0",
        LastUpdated = DateTime.UtcNow
    };

    public byte[] GeneratePdf(InvoiceModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(20));

                page.Header()
                    .Text("DUMMY UBL TEMPLATE FOR TESTING")
                    .SemiBold().FontSize(30).FontColor(Colors.Red.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);
                        
                        x.Item().Text($"Invoice Number: {model.Document?.Number ?? "N/A"}");
                        
                        string dateStr = "N/A";
                        if (model.Document != null && model.Document.IssueDate != default)
                        {
                            dateStr = model.Document.IssueDate.ToString("yyyy-MM-dd");
                        }
                        x.Item().Text($"Issue Date: {dateStr}");
                        
                        x.Item().Text($"Total Amount: {model.Document?.Total_am ?? 0}");
                        
                        x.Item().Text("This is a dummy template to test if the routing and integration are working.")
                            .FontSize(14).FontColor(Colors.Grey.Darken2);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        }).GeneratePdf();
    }
}
