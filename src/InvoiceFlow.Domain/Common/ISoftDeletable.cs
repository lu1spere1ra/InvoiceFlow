namespace InvoiceFlow.Domain.Common;

/// <summary>
/// Marks master data that is hidden instead of physically deleted, because issued documents and the
/// SAF-T export may still reference it. The mechanics live in the persistence layer.
/// </summary>
public interface ISoftDeletable;
