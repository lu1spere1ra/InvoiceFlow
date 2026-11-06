# ADR 0004 — Hash chain, ATCUD and what is deliberately not compliant

- **Status:** Accepted
- **Date:** 2026-11-06

> InvoiceFlow is **not certified invoicing software**. This ADR explains a simplified model inspired by
> the Autoridade Tributária (AT) rules, built to show how such rules map to code.

## Context

Portuguese invoicing software must make issued documents tamper-evident and uniquely identifiable:

- **Hash chain** (Portaria 363/2010): every document signs
  `InvoiceDate;SystemEntryDate;InvoiceNo;GrossTotal;PreviousHash` with the producer's RSA private key,
  where `PreviousHash` is the signature of the previous document in the same series.
- **ATCUD** (Decreto-Lei 28/2019): `{series validation code}-{sequential number}`, the validation code
  being returned by AT when the series is communicated.
- **QR code** (Portaria 195/2020) with a fixed set of fields, and the first characters of the hash
  printed on the document.

## Decision

- **The domain decides what is signed; infrastructure decides how.** `Invoice.Issue` builds the message
  with `HashChain.Message` and calls an `IDocumentSigner` port. `RsaDocumentSigner` uses **RSA-SHA1,
  PKCS#1 v1.5, 1024-bit key, base64**, as the AT specification prescribes; the 172-character base64
  output is also what fits the SAF-T `Hash` field. Neither SHA-1 nor 1024-bit RSA would be chosen for a
  new design; the analyzer warnings are suppressed with that justification.
- **The series stores the last hash** next to the last number. Both are updated in the same save as the
  issued document and protected by the series `rowversion` (ADR 0003), so the chain cannot fork under
  concurrency.
- **System entry date** is signed in Portuguese local time (`yyyy-MM-ddTHH:mm:ss`), as SAF-T expects,
  not in the server's UTC.
- **Verification.** `HashChain.FindFirstBreak` replays a series: numbers must be `1..n` and every
  signature must verify against the recomputed message. `GET /api/series/{id}/verification` exposes it;
  an integration test edits a total directly in SQL and checks the break is detected at that document.
- **ATCUD** is assigned on issue. Series validation codes come from an `IAtSeriesRegistry` port whose
  only implementation **simulates** the AT web service with a random 8-character code.
- **QR payload** is a pure function (`AtQrCode.Build`) covered by unit tests; the QR image is drawn by
  QRCoder in the PDF renderer. Field R (certificate number) is `0`.
- **Key management:** the PEM key is read from `Signing:KeyPath`. In development and in the docker
  compose demo it is generated on first start (`Signing:GenerateIfMissing`) and kept in a volume; it is
  never committed. Rotating the key means bumping `Signing:KeyVersion` (SAF-T `HashControl`).

## What is not compliant (on purpose)

- No certification, no AT certificate number, no real series communication or document submission.
- The printed hash notice says "processed by a non-certified program" and the PDF states that it has no
  fiscal validity.
- Only mainland VAT (fiscal space PT); no Azores/Madeira rates, no withholding tax, no other currencies.
- Receipts (RG), transport documents and the other SAF-T source document types are out of scope.

## Consequences

- Altering, deleting or inserting an issued document outside the application is detectable.
- The private key becomes a critical secret: losing it does not break existing documents (verification
  needs only the public part) but a leaked key would allow forged chains.
