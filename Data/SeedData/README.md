# WHO ICD-10 2019 metadata

`icd102019enMeta.zip` is the official WHO ICD-10 2019 English metadata package downloaded from:

https://icdcdn.who.int/icd10/index.html

SHA-256: `A9FA787FA79076E199E46CA645BBC076449853E93E62CD3B2050B96DC96BBC1D`

The package contains the chapter, block, and three-to-five-character code hierarchy. WHO notes that this metadata is suitable for translating codes to titles, grouping, and formal validation, but it omits inclusion and exclusion notes and therefore must not be treated as standalone coding software.

The original package is redistributed unchanged. ICD-10 2019 is published by the World Health Organization under CC BY-ND 3.0 IGO. Existing Thai labels in SmartClinic are retained as local overlays and are not represented as WHO translations.

## Thai Modification terminal-code list

`icd10tm2019-update-202405.xlsx` is the unmodified machine-readable ICD-10-TM 2019 list published by the Thai Health Coding Center and updated in May 2024:

http://thcc.or.th/download/icd/Data%20TM2019-update_052024.xlsx

SHA-256: `54678941286CD140366597F07EE39B576D8A1A964F3088EC0E29B2D7D4F69DCA`

The workbook contains 39,236 unique terminal codes with non-empty English descriptions. SmartClinic validates the workbook row count, code syntax, uniqueness, descriptions, and parent category before importing it. The original workbook contains English descriptions; Thai search labels are retained only where a separately reviewed Thai overlay exists.
