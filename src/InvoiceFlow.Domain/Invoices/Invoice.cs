using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Domain.Invoices;

/// <summary>
/// A sales document (invoice, invoice-receipt or credit note) and the aggregate root that enforces
/// its lifecycle: drafts can be edited freely, issued documents are immutable.
/// </summary>
public sealed class Invoice : Entity
{
    /// <summary>Maximum gross total for a document addressed to an anonymous final consumer.</summary>
    public const decimal FinalConsumerLimit = 1000m;

    private readonly List<InvoiceLine> _lines = [];

    private Invoice(DocumentType type, Customer customer, int paymentTermDays)
    {
        Type = type;
        Status = InvoiceStatus.Draft;
        CustomerId = customer.Id;
        Customer = CustomerSnapshot.From(customer);
        PaymentTermDays = paymentTermDays;
    }

    private Invoice(Invoice original, string reason)
    {
        Type = DocumentType.CreditNote;
        Status = InvoiceStatus.Draft;
        CustomerId = original.CustomerId;
        Customer = original.Customer;
        PaymentTermDays = 0;
        OriginalInvoiceId = original.Id;
        OriginalDocumentNumber = original.DocumentNumber;
        CreditReason = reason;
    }

    // Required by EF Core.
    private Invoice() => Customer = null!;

    public DocumentType Type { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public Guid CustomerId { get; private set; }

    public CustomerSnapshot Customer { get; private set; }

    public int PaymentTermDays { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines => _lines;

    public decimal NetTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal GrossTotal { get; private set; }

    // Set when the document is issued.
    public Guid? SeriesId { get; private set; }

    public int? Number { get; private set; }

    public string? DocumentNumber { get; private set; }

    public string? Atcud { get; private set; }

    public DateOnly? IssueDate { get; private set; }

    public DateOnly? DueDate { get; private set; }

    /// <summary>Moment the document was recorded (SAF-T <c>SystemEntryDate</c>).</summary>
    public DateTimeOffset? SystemEntryDate { get; private set; }

    public DateOnly? PaidOn { get; private set; }

    public string? VoidReason { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    // Credit notes only: the document being corrected and why (SAF-T References).
    public Guid? OriginalInvoiceId { get; private set; }

    public string? OriginalDocumentNumber { get; private set; }

    public string? CreditReason { get; private set; }

    public bool IsDraft => Status == InvoiceStatus.Draft;

    /// <summary>Issued or paid: a valid fiscal document (drafts and voided documents are not).</summary>
    public bool IsEffective => Status is InvoiceStatus.Issued or InvoiceStatus.Paid;

    /// <summary>VAT grouped by rate. Each group is rounded once, which avoids per-line rounding drift.</summary>
    public IReadOnlyList<TaxSummaryLine> TaxSummary =>
        _lines
            .GroupBy(line => (line.TaxCode, line.TaxPercentage))
            .Select(group =>
            {
                var taxable = group.Sum(line => line.NetAmount);
                return new TaxSummaryLine(group.Key.TaxCode, group.Key.TaxPercentage, taxable, Money.Round(taxable * group.Key.TaxPercentage / 100m));
            })
            .OrderByDescending(summary => summary.Percentage)
            .ToList();

    public static Result<Invoice> CreateDraft(DocumentType type, Customer customer, int paymentTermDays = 30)
    {
        if (type == DocumentType.CreditNote)
        {
            return InvoiceErrors.CreditNoteRequiresOriginal;
        }

        if (paymentTermDays is < 0 or > 365)
        {
            return InvoiceErrors.InvalidPaymentTerms;
        }

        return new Invoice(type, customer, paymentTermDays);
    }

    /// <summary>
    /// Creates a draft credit note that reverses some or all of an issued invoice. Lines are copied from
    /// the original (same price, VAT and exemption) with the quantities to credit. The total can never
    /// exceed what is still creditable: the original gross minus earlier, non-voided credit notes.
    /// </summary>
    /// <param name="alreadyCredited">Gross total of effective credit notes already issued against <paramref name="original"/>.</param>
    public static Result<Invoice> CreateCreditNote(
        Invoice original, string reason, IReadOnlyList<CreditNoteLine> lines, decimal alreadyCredited)
    {
        if (original.Type == DocumentType.CreditNote || !original.IsEffective)
        {
            return InvoiceErrors.NotCreditable;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return InvoiceErrors.CreditReasonRequired;
        }

        if (lines.Count == 0)
        {
            return InvoiceErrors.NoLines;
        }

        var creditNote = new Invoice(original, reason.Trim());
        foreach (var requested in lines)
        {
            var source = original._lines.FirstOrDefault(line => line.LineNumber == requested.LineNumber);
            if (source is null)
            {
                return InvoiceErrors.LineNotFound;
            }

            if (requested.Quantity <= 0 || requested.Quantity > source.Quantity)
            {
                return InvoiceErrors.CreditQuantityInvalid;
            }

            creditNote._lines.Add(source.CopyFor(creditNote._lines.Count + 1, requested.Quantity));
        }

        creditNote.RecalculateTotals();
        if (creditNote.GrossTotal > original.GrossTotal - alreadyCredited)
        {
            return InvoiceErrors.CreditExceedsRemaining;
        }

        return creditNote;
    }

    public Result ChangeCustomer(Customer customer)
    {
        if (!IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        if (Type == DocumentType.CreditNote)
        {
            return InvoiceErrors.CreditNoteIsFixed;
        }

        CustomerId = customer.Id;
        Customer = CustomerSnapshot.From(customer);
        return Result.Success();
    }

    public Result ChangePaymentTerms(int paymentTermDays)
    {
        if (!IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        if (paymentTermDays is < 0 or > 365)
        {
            return InvoiceErrors.InvalidPaymentTerms;
        }

        PaymentTermDays = paymentTermDays;
        return Result.Success();
    }

    public Result AddLine(
        Product product,
        TaxRate taxRate,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent = 0,
        string? exemptionCode = null,
        string? description = null)
    {
        if (!IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        if (Type == DocumentType.CreditNote)
        {
            return InvoiceErrors.CreditNoteIsFixed;
        }

        if (quantity <= 0)
        {
            return InvoiceErrors.QuantityMustBePositive;
        }

        if (unitPrice < 0)
        {
            return InvoiceErrors.NegativeUnitPrice;
        }

        if (discountPercent is < 0 or > 100)
        {
            return InvoiceErrors.InvalidDiscount;
        }

        var exemption = ExemptionReason.Find(exemptionCode);
        if (taxRate.IsExempt && exemption is null)
        {
            return InvoiceErrors.ExemptionReasonRequired;
        }

        if (!taxRate.IsExempt && exemptionCode is not null)
        {
            return InvoiceErrors.ExemptionReasonNotAllowed;
        }

        var lineDescription = string.IsNullOrWhiteSpace(description) ? product.Description : description.Trim();
        _lines.Add(new InvoiceLine(
            _lines.Count + 1, product, lineDescription, quantity, unitPrice, discountPercent, taxRate, exemption));

        RecalculateTotals();
        return Result.Success();
    }

    public Result RemoveLine(int lineNumber)
    {
        if (!IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        if (Type == DocumentType.CreditNote)
        {
            return InvoiceErrors.CreditNoteIsFixed;
        }

        if (_lines.RemoveAll(line => line.LineNumber == lineNumber) == 0)
        {
            return InvoiceErrors.LineNotFound;
        }

        Renumber();
        RecalculateTotals();
        return Result.Success();
    }

    public Result ClearLines()
    {
        if (!IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        if (Type == DocumentType.CreditNote)
        {
            return InvoiceErrors.CreditNoteIsFixed;
        }

        _lines.Clear();
        RecalculateTotals();
        return Result.Success();
    }

    /// <summary>
    /// Issues the draft: takes the next number from <paramref name="series"/>, stamps the ATCUD and
    /// freezes the document. All checks run before the series is touched, so a failed issue never
    /// consumes a number.
    /// </summary>
    public Result Issue(DocumentSeries series, DateOnly issueDate, DateTimeOffset systemEntryDate)
    {
        var error =
            !IsDraft ? InvoiceErrors.NotDraft
            : _lines.Count == 0 ? InvoiceErrors.NoLines
            : series.DocumentType != Type ? InvoiceErrors.SeriesTypeMismatch
            : Customer.IsFinalConsumer && GrossTotal > FinalConsumerLimit ? InvoiceErrors.FinalConsumerLimitExceeded
            : series.CanIssue(issueDate);

        if (error is not null)
        {
            return error;
        }

        var number = series.ReserveNextNumber(issueDate).Value;

        SeriesId = series.Id;
        Number = number;
        DocumentNumber = series.FormatDocumentNumber(number);
        Atcud = series.FormatAtcud(number);
        IssueDate = issueDate;
        DueDate = issueDate.AddDays(PaymentTermDays);
        SystemEntryDate = systemEntryDate;

        // An invoice-receipt documents a payment received at the moment of sale.
        if (Type == DocumentType.InvoiceReceipt)
        {
            Status = InvoiceStatus.Paid;
            PaidOn = issueDate;
        }
        else
        {
            Status = InvoiceStatus.Issued;
        }

        return Result.Success();
    }

    public Result MarkAsPaid(DateOnly paidOn)
    {
        var error =
            Type != DocumentType.Invoice ? InvoiceErrors.OnlyInvoicesArePaid
            : Status != InvoiceStatus.Issued ? InvoiceErrors.NotIssued
            : paidOn < IssueDate ? InvoiceErrors.PaymentBeforeIssue
            : null;

        if (error is not null)
        {
            return error;
        }

        Status = InvoiceStatus.Paid;
        PaidOn = paidOn;
        return Result.Success();
    }

    /// <summary>
    /// Voids (anula) an issued document. It keeps its number, it is only marked as not effective, so the
    /// sequence stays intact. A document already corrected by credit notes cannot be voided.
    /// </summary>
    public Result Void(string reason, DateTimeOffset voidedAt, decimal creditedAmount = 0)
    {
        var error =
            !IsEffective ? InvoiceErrors.NotIssued
            : string.IsNullOrWhiteSpace(reason) ? InvoiceErrors.VoidReasonRequired
            : creditedAmount > 0 ? InvoiceErrors.HasCreditNotes
            : null;

        if (error is not null)
        {
            return error;
        }

        Status = InvoiceStatus.Voided;
        VoidReason = reason.Trim();
        VoidedAt = voidedAt;
        return Result.Success();
    }

    private void Renumber()
    {
        for (var i = 0; i < _lines.Count; i++)
        {
            _lines[i].Renumber(i + 1);
        }
    }

    private void RecalculateTotals()
    {
        NetTotal = _lines.Sum(line => line.NetAmount);
        TaxTotal = TaxSummary.Sum(summary => summary.TaxAmount);
        GrossTotal = NetTotal + TaxTotal;
    }
}
