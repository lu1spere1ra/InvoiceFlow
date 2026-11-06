using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Domain.UnitTests.Invoices;

public sealed class InvoiceTotalsTests
{
    [Fact]
    public void Computes_net_vat_and_gross_for_a_single_line()
    {
        var invoice = TestData.DraftWithLine(quantity: 2, unitPrice: 50m);

        invoice.NetTotal.ShouldBe(100m);
        invoice.TaxTotal.ShouldBe(23m);
        invoice.GrossTotal.ShouldBe(123m);
    }

    [Fact]
    public void Groups_vat_by_rate()
    {
        var invoice = TestData.Draft();
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 100m);
        invoice.AddLine(TestData.Product(), TestData.Intermediate, 1, 100m);
        invoice.AddLine(TestData.Product(), TestData.Reduced, 1, 100m);
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 50m);

        invoice.TaxSummary.ShouldBe(
        [
            new TaxSummaryLine(TaxRateCode.Normal, 23m, 150m, 34.50m),
            new TaxSummaryLine(TaxRateCode.Intermediate, 13m, 100m, 13m),
            new TaxSummaryLine(TaxRateCode.Reduced, 6m, 100m, 6m),
        ]);
        invoice.TaxTotal.ShouldBe(53.50m);
        invoice.GrossTotal.ShouldBe(403.50m);
    }

    [Fact]
    public void Applies_line_discount_before_vat()
    {
        var invoice = TestData.Draft();
        invoice.AddLine(TestData.Product(), TestData.Normal, 3, 19.99m, discountPercent: 10);

        // 3 × 19.99 = 59.97; −10% = 53.973 → 53.97
        invoice.Lines[0].NetAmount.ShouldBe(53.97m);
        invoice.TaxTotal.ShouldBe(12.41m); // 53.97 × 23% = 12.4131
        invoice.GrossTotal.ShouldBe(66.38m);
    }

    [Fact]
    public void Rounds_half_away_from_zero()
    {
        var invoice = TestData.Draft();
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 0.125m);

        // Banker's rounding would give 0.12.
        invoice.Lines[0].NetAmount.ShouldBe(0.13m);
    }

    [Fact]
    public void Rounds_vat_once_per_rate_not_per_line()
    {
        var invoice = TestData.Draft();
        for (var i = 0; i < 3; i++)
        {
            invoice.AddLine(TestData.Product(), TestData.Normal, 1, 0.10m);
        }

        // Per line: 0.023 → 0.02 × 3 = 0.06. Per rate: 0.30 × 23% = 0.069 → 0.07.
        invoice.TaxTotal.ShouldBe(0.07m);
    }

    [Fact]
    public void Exempt_lines_require_a_known_exemption_reason()
    {
        var invoice = TestData.Draft();

        invoice.AddLine(TestData.Product(), TestData.Exempt, 1, 100m).Error.ShouldBe(InvoiceErrors.ExemptionReasonRequired);
        invoice.AddLine(TestData.Product(), TestData.Exempt, 1, 100m, exemptionCode: "M00").Error.ShouldBe(InvoiceErrors.ExemptionReasonRequired);

        invoice.AddLine(TestData.Product(), TestData.Exempt, 1, 100m, exemptionCode: "M07").IsSuccess.ShouldBeTrue();
        invoice.Lines[0].ExemptionReason.ShouldBe("Isento artigo 9.º do CIVA");
        invoice.GrossTotal.ShouldBe(100m);
    }

    [Fact]
    public void Taxed_lines_cannot_have_an_exemption_reason() =>
        TestData.Draft().AddLine(TestData.Product(), TestData.Normal, 1, 100m, exemptionCode: "M07")
            .Error.ShouldBe(InvoiceErrors.ExemptionReasonNotAllowed);

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(-1, 10, 0)]
    [InlineData(1, -10, 0)]
    [InlineData(1, 10, 101)]
    [InlineData(1, 10, -1)]
    public void Rejects_invalid_line_values(decimal quantity, decimal unitPrice, decimal discount)
    {
        var invoice = TestData.Draft();

        invoice.AddLine(TestData.Product(), TestData.Normal, quantity, unitPrice, discount).IsFailure.ShouldBeTrue();
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void Removing_a_line_renumbers_and_recalculates()
    {
        var invoice = TestData.Draft();
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 10m);
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 20m);
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 30m);

        invoice.RemoveLine(2).IsSuccess.ShouldBeTrue();

        invoice.Lines.Select(line => line.LineNumber).ShouldBe([1, 2]);
        invoice.NetTotal.ShouldBe(40m);
    }

    [Fact]
    public void Line_snapshots_product_and_tax_data()
    {
        var product = TestData.Product();
        var invoice = TestData.Draft();
        invoice.AddLine(product, TestData.Normal, 1, 10m, description: "Consultoria março");

        var line = invoice.Lines[0];
        line.ProductCode.ShouldBe("CONS-01");
        line.Description.ShouldBe("Consultoria março");
        line.Unit.ShouldBe("H");
        line.TaxPercentage.ShouldBe(23m);
    }
}
