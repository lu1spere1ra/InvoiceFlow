# SAF-T (PT) 1.04_01 schema

`saftpt1.04_01.xsd` is the official schema published by the Autoridade Tributária e Aduaneira at
<https://info.portaldasfinancas.gov.pt/apps/saft-pt04/saftpt1.04_01.xsd> (version 1.04_01, modified
2022-05-17).

It was taken from [invopop/gobl.pt.saft](https://github.com/invopop/gobl.pt.saft/blob/main/schemas/saftpt1.04_01.xsd),
where a few XSD 1.1 assertions are commented out so that XSD 1.0 validators (including .NET's
`XmlSchemaSet`) can load it. The element structure and types are unchanged.

`SaftExportTests` validates every generated file against it.
