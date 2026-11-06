using System.Globalization;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceFlow.Infrastructure.Pdf;

/// <summary>A4 invoice layout. Same visual language as the web "sheet": ink blue, one typeface, tabular numbers.</summary>
internal sealed class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    private const string Ink = "#13294B";
    private const string Muted = "#5B6B82";
    private const string Rule = "#DCE3EC";
    private const string Stamp = "#B4232C";

    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-PT");

    static QuestPdfInvoiceRenderer()
    {
        // Community licence: free for individuals and companies under USD 1M annual revenue.
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
    }

    public byte[] Render(InvoiceDto invoice, CompanyProfile company, string? qrPayload) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink));

            page.Header().Element(header => Header(header, invoice, company));
            page.Content().PaddingVertical(18).Element(content => Content(content, invoice));
            page.Footer().Element(footer => Footer(footer, invoice, company, qrPayload));
        })).GeneratePdf();

    private static void Header(IContainer container, InvoiceDto invoice, CompanyProfile company) =>
        container.BorderTop(5).BorderColor(invoice.Status == InvoiceStatus.Draft ? "#8A5D00" : Ink).PaddingTop(14).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(company.Name).FontSize(13).Bold();
                col.Item().Text(company.Street);
                col.Item().Text($"{company.PostalCode} {company.City}");
                col.Item().Text($"NIF {company.Nif}");
                if (company.Email is not null)
                {
                    col.Item().Text(company.Email).FontColor(Muted);
                }
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(TypeLabel(invoice.Type)).FontColor(Muted);
                col.Item().AlignRight().Text(invoice.DocumentNumber ?? "DRAFT · not a valid document").FontSize(18).Bold();
                col.Item().AlignRight().Text($"Date {FormatDate(invoice.IssueDate)}");
                if (invoice.DueDate is not null)
                {
                    col.Item().AlignRight().Text($"Due {FormatDate(invoice.DueDate)}");
                }

                if (invoice.Atcud is not null)
                {
                    col.Item().AlignRight().Text($"ATCUD: {invoice.Atcud}").SemiBold();
                }

                if (invoice.Status == InvoiceStatus.Voided)
                {
                    col.Item().AlignRight().PaddingTop(4).Text($"VOIDED: {invoice.VoidReason}").FontColor(Stamp).Bold();
                }
            });
        });

    private static void Content(IContainer container, InvoiceDto invoice) => container.Column(col =>
    {
        col.Spacing(14);

        col.Item().Column(customer =>
        {
            customer.Item().Text("Billed to").FontColor(Muted);
            customer.Item().Text(invoice.Customer.Name).FontSize(11).SemiBold();
            customer.Item().Text(invoice.Customer.Street);
            customer.Item().Text($"{invoice.Customer.PostalCode} {invoice.Customer.City}");
            customer.Item().Text($"NIF {invoice.Customer.Nif}");
        });

        if (invoice.OriginalDocumentNumber is not null)
        {
            col.Item().Text($"Credit note for {invoice.OriginalDocumentNumber}. Reason: {invoice.CreditReason}").Italic();
        }

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(5);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1.6f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "Description");
                HeaderCell(header.Cell(), "Qty", right: true);
                HeaderCell(header.Cell(), "Unit price", right: true);
                HeaderCell(header.Cell(), "Disc.", right: true);
                HeaderCell(header.Cell(), "VAT", right: true);
                HeaderCell(header.Cell(), "Net", right: true);
            });

            foreach (var line in invoice.Lines)
            {
                BodyCell(table.Cell()).Column(c =>
                {
                    c.Item().Text(line.Description);
                    c.Item().Text(line.ProductCode).FontSize(7.5f).FontColor(Muted);
                    if (line.ExemptionCode is not null)
                    {
                        c.Item().Text($"{line.ExemptionCode}: {line.ExemptionReason}").FontSize(7.5f).FontColor(Muted);
                    }
                });
                BodyCell(table.Cell()).AlignRight().Text($"{line.Quantity.ToString("0.####", Portuguese)} {line.Unit}");
                BodyCell(table.Cell()).AlignRight().Text(Money(line.UnitPrice, "N2"));
                BodyCell(table.Cell()).AlignRight().Text(line.DiscountPercent == 0 ? "" : $"{line.DiscountPercent:0.##}%");
                BodyCell(table.Cell()).AlignRight().Text($"{line.TaxPercentage:0.##}%");
                BodyCell(table.Cell()).AlignRight().Text(Money(line.NetAmount));
            }
        });

        col.Item().Row(row =>
        {
            row.RelativeItem().Column(vat =>
            {
                vat.Item().Text("VAT summary").FontColor(Muted);
                foreach (var tax in invoice.TaxSummary)
                {
                    vat.Item().Text($"{tax.Percentage:0.##}%  on {Money(tax.TaxableAmount)}  =  {Money(tax.TaxAmount)}");
                }
            });

            row.ConstantItem(200).Column(totals =>
            {
                TotalRow(totals, "Net total", invoice.NetTotal);
                TotalRow(totals, "VAT", invoice.TaxTotal);
                totals.Item().PaddingTop(4).BorderTop(1.5f).BorderColor(Ink).PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("Total").FontSize(13).Bold();
                    r.RelativeItem().AlignRight().Text(Money(invoice.GrossTotal)).FontSize(13).Bold();
                });
            });
        });
    });

    private static void Footer(IContainer container, InvoiceDto invoice, CompanyProfile company, string? qrPayload) =>
        container.BorderTop(1).BorderColor(Rule).PaddingTop(8).Row(row =>
        {
            if (qrPayload is not null)
            {
                row.ConstantItem(80).Image(QrPng(qrPayload));
                row.ConstantItem(12);
            }

            row.RelativeItem().AlignMiddle().Column(col =>
            {
                if (invoice.HashExcerpt is not null)
                {
                    col.Item().Text($"{invoice.HashExcerpt} - Processed by a non-certified program (InvoiceFlow demo, certificate {company.SoftwareCertificateNumber}/AT)")
                        .FontSize(7.5f);
                }

                col.Item().Text("InvoiceFlow is a portfolio project. This document has no fiscal validity.").FontSize(7.5f).FontColor(Muted);
            });

            row.ConstantItem(60).AlignRight().AlignBottom().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Muted));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });

    private static void HeaderCell(IContainer cell, string text, bool right = false)
    {
        var container = cell.BorderBottom(1).BorderColor(Ink).PaddingVertical(4);
        (right ? container.AlignRight() : container).Text(text).FontColor(Muted).SemiBold();
    }

    private static IContainer BodyCell(IContainer cell) => cell.BorderBottom(0.5f).BorderColor(Rule).PaddingVertical(5);

    private static void TotalRow(ColumnDescriptor column, string label, decimal amount) => column.Item().Row(r =>
    {
        r.RelativeItem().Text(label).FontColor(Muted);
        r.RelativeItem().AlignRight().Text(Money(amount));
    });

    private static byte[] QrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(data).GetGraphic(8);
    }

    private static string Money(decimal amount, string format = "N2") => $"{amount.ToString(format, Portuguese)} €";

    private static string FormatDate(DateOnly? date) => date?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";

    private static string TypeLabel(DocumentType type) => type switch
    {
        DocumentType.Invoice => "Fatura · Invoice",
        DocumentType.InvoiceReceipt => "Fatura-recibo · Invoice-receipt",
        DocumentType.CreditNote => "Nota de crédito · Credit note",
        _ => type.ToString(),
    };
}
