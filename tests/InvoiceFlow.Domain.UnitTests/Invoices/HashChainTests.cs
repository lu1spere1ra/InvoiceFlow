using System.Reflection;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Domain.UnitTests.Invoices;

public sealed class HashChainTests
{
    [Fact]
    public void Message_uses_the_AT_field_layout()
    {
        var message = HashChain.Message(
            new DateOnly(2026, 3, 10), new DateTimeOffset(2026, 3, 10, 14, 30, 5, TimeSpan.Zero), "FT 2026A/7", 1234.5m, "abc==");

        message.ShouldBe("2026-03-10;2026-03-10T14:30:05;FT 2026A/7;1234.50;abc==");
    }

    [Fact]
    public void First_document_of_a_series_signs_an_empty_previous_hash()
    {
        var invoice = TestData.DraftWithLine();

        invoice.Issue(TestData.Series(), TestData.Today, TestData.Now, TestData.Signer);

        var expected = TestData.Signer.Sign(HashChain.Message(TestData.Today, TestData.Now, "FT 2026A/1", 123m, null));
        invoice.Hash.ShouldBe(expected);
        invoice.HashControl.ShouldBe("1");
    }

    [Fact]
    public void Each_document_chains_to_the_previous_one()
    {
        var series = TestData.Series();
        var first = TestData.DraftWithLine();
        var second = TestData.DraftWithLine(unitPrice: 50m);

        first.Issue(series, TestData.Today, TestData.Now, TestData.Signer);
        second.Issue(series, TestData.Today, TestData.Now, TestData.Signer);

        var expected = TestData.Signer.Sign(HashChain.Message(TestData.Today, TestData.Now, "FT 2026A/2", 61.50m, first.Hash));
        second.Hash.ShouldBe(expected);
        series.LastHash.ShouldBe(second.Hash);
    }

    [Fact]
    public void A_failed_issue_does_not_move_the_chain()
    {
        var series = TestData.Series();

        TestData.Draft().Issue(series, TestData.Today, TestData.Now, TestData.Signer).IsFailure.ShouldBeTrue();

        series.LastHash.ShouldBeNull();
    }

    [Fact]
    public void Verification_accepts_an_intact_series() =>
        HashChain.FindFirstBreak(IssueThree(), TestData.Signer).ShouldBeNull();

    [Fact]
    public void Verification_detects_a_tampered_total()
    {
        var documents = IssueThree();
        SetPrivate(documents[1], nameof(Invoice.GrossTotal), 1m); // what a direct UPDATE in the database would do

        HashChain.FindFirstBreak(documents, TestData.Signer).ShouldBe(2);
    }

    [Fact]
    public void Verification_detects_a_removed_document()
    {
        var documents = IssueThree();
        documents.RemoveAt(1);

        HashChain.FindFirstBreak(documents, TestData.Signer).ShouldBe(2);
    }

    [Theory]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "AKU4")]
    [InlineData("short", "")]
    [InlineData(null, "")]
    public void Excerpt_takes_characters_1_11_21_and_31(string? hash, string expected) =>
        HashChain.Excerpt(hash).ShouldBe(expected);

    private static List<Invoice> IssueThree()
    {
        var series = TestData.Series();
        var documents = Enumerable.Range(1, 3).Select(i => TestData.DraftWithLine(unitPrice: i * 10m)).ToList();
        foreach (var document in documents)
        {
            document.Issue(series, TestData.Today, TestData.Now, TestData.Signer).IsSuccess.ShouldBeTrue();
        }

        return documents;
    }

    private static void SetPrivate(Invoice invoice, string property, object value) =>
        typeof(Invoice).GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.SetValue(invoice, value);
}
