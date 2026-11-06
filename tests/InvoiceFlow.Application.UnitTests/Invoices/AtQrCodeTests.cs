using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.UnitTests.Invoices;

public sealed class AtQrCodeTests
{
    private static readonly CompanyProfile Company = new() { Nif = "509123457", SoftwareCertificateNumber = "0" };

    [Fact]
    public void Builds_the_AT_payload_with_one_group_per_vat_rate()
    {
        var invoice = Invoice(
            InvoiceStatus.Issued,
            [
                new TaxSummaryDto(TaxRateCode.Normal, 23m, 100m, 23m),
                new TaxSummaryDto(TaxRateCode.Reduced, 6m, 50m, 3m),
                new TaxSummaryDto(TaxRateCode.Exempt, 0m, 10m, 0m),
            ]);

        AtQrCode.Build(invoice, Company).ShouldBe(
            "A:509123457*B:999999990*C:PT*D:FT*E:N*F:20260310*G:FT 2026A/7*H:CSDF7T5H-7*I1:PT" +
            "*I2:10.00*I3:50.00*I4:3.00*I7:100.00*I8:23.00*N:26.00*O:186.00*Q:AKU4*R:0");
    }

    [Fact]
    public void Voided_documents_are_flagged() =>
        AtQrCode.Build(Invoice(InvoiceStatus.Voided, []), Company)!.ShouldContain("*E:A*");

    [Fact]
    public void Drafts_have_no_qr_code() =>
        AtQrCode.Build(Invoice(InvoiceStatus.Draft, []) with { DocumentNumber = null, Atcud = null }, Company).ShouldBeNull();

    private static InvoiceDto Invoice(InvoiceStatus status, IReadOnlyList<TaxSummaryDto> taxes) => new(
        Guid.NewGuid(), DocumentType.Invoice, status, "FT 2026A/7", "CSDF7T5H-7", Guid.NewGuid(),
        new InvoiceCustomerDto("Consumidor final", "999999990", "-", "0000-000", "-", "PT"),
        30, new DateOnly(2026, 3, 10), null, null, [], taxes,
        NetTotal: 160m, TaxTotal: 26m, GrossTotal: 186m,
        PaidOn: null, VoidReason: null, OriginalInvoiceId: null, OriginalDocumentNumber: null, CreditReason: null,
        CreditedAmount: 0, HashExcerpt: "AKU4");
}
