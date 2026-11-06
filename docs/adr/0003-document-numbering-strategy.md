# ADR 0003 — Document numbering strategy

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

Portuguese rules require documents of each series to be numbered sequentially, starting at 1, with no
gaps and no duplicates, and with dates that never go backwards within the series. Two clerks can click
"Issue" at the same moment.

Common approaches and why they do not fit:

| Approach | Problem |
|---|---|
| Database `IDENTITY` / sequence | Gaps on rollback; one sequence per series would need DDL at run time |
| `MAX(Number) + 1` at insert time | Race condition: two transactions read the same max |
| Number assigned when the draft is created | Deleted or abandoned drafts leave gaps |

## Decision

1. **Drafts have no number.** A number is assigned only when the document is issued
   (`Invoice.Issue`), so deleting drafts never creates gaps.
2. **The series owns the counter.** `DocumentSeries.LastNumber` (and `LastIssueDate`) live on the series
   row. `Invoice.Issue` runs every check first (status, lines, series type, final-consumer limit, date
   order) and only then calls `series.ReserveNextNumber`, so a failed issue never consumes a number.
3. **Optimistic concurrency on the series row.** The series has a SQL Server `rowversion` column
   (mapped as an EF Core shadow property, so it stays out of the domain model). The updated series and
   the issued document are saved in the same `SaveChanges` call, i.e. one transaction. If another request
   issued from the same series in the meantime, the `rowversion` check fails, nothing is written, and the
   API answers **409 Conflict**. The client can simply retry.
4. **Unique index as the last line of defence:** `(SeriesId, Number)` is unique, so even a bug elsewhere
   cannot persist a duplicate.

An integration test fires several concurrent issue requests against a real SQL Server and asserts that
the issued numbers are exactly `1..n`.

## Alternatives considered

- **Pessimistic lock** (`SELECT … WITH (UPDLOCK)` on the series inside an explicit transaction): no
  409s for users, but needs explicit transaction handling in the use case and holds locks while the rest
  of the request runs. Worth switching to if conflicts become frequent (many cashiers on one series).
- **Automatic retry on conflict** inside the use case: hides the conflict from the user, but requires
  discarding and reloading tracked entities. Not worth it at the expected volume of an SME.

## Consequences

- Correct under concurrency with no locks held between reads and the final save.
- Under heavy contention on a single series, some requests get a 409 and must be retried.
- One series per document type is active at a time; opening a new series (e.g. at year end) means
  closing the old one.
