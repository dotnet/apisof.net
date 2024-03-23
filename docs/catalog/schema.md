# API Catalog Schema

The binary catalog schema looks as follows:

| Offset | Length | Name                    |
| ------ | ------ | ----------------------- |
| 0      | 60     | Header                  |
| 60     | Rest   | Deflate compressed body |

## Header

| Offset | Length | Type      | Name        | Comment    |
| ------ | ------ | --------- | ----------- | ---------- |
| 0      | 8      | char8[8]  | Magic Value | 'APICATFB' |
| 4      | 4      | int32     | Version     |            |
| 8      | 48     | int32[12] | Table Sizes |            |

The table sizes are in the following order:

1. String Table
1. Platform Table
1. Framework Table
1. Package Table
1. Assembly Table
1. Usage Sources Table
1. API Table
1. Obsoletion Table
1. Platform Support Table
1. Preview Requirement Table
1. Experimental Table
1. Extension Methods Table

## String Table

Probably shouldn't be a table -- it's more of a blob. Users will have an offset
that points into the string table. They read a length-prefixed UTF8 string.

| Type       | Length | Comment             |
| ---------- | ------ | ------------------- |
| int32      | 4      | Number of bytes `N` |
| char8[`N`] | `N`    | UTF8 characters.    |

## Platform Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row Count |         |

Each row looks as follows:

| Offset | Length | Type                | Name          | Comment |
| ------ | ------ | ------------------- | ------------- | ------- |
| 0      | 4      | string table offset | Platform Name |         |

## Framework Table

The rows are of varying length, so the header first states how many rows there
are followed by an array with the row start offsets, relative to the table start
(before header):

| Offset | Length | Type       | Name          | Comment |
| ------ | ------ | ---------- | ------------- | ------- |
| 0      | 4      | int32      | Row count `R` |         |
| 4      | 4*`R`  | int32[`R`] | Row offsets   |         |

Each row looks as follows:

| Offset | Length | Type                | Name                     | Comment |
| ------ | ------ | ------------------- | ------------------------ | ------- |
| 0      | 4      | string table offset | Framework Name           |         |
| 4      | 4      | int32               | Number of assemblies `A` |         |
| 8      | 4*`A`  | int32[`A`]          | Assembly offsets         |         |

## Package Table

The rows are of varying length, so the header first states how many rows there
are followed by an array with the row start offsets, relative to the table start
(before header):

| Offset | Length | Type       | Name          | Comment |
| ------ | ------ | ---------- | ------------- | ------- |
| 0      | 4      | int32      | Row count `R` |         |
| 4      | 4*`R`  | int32[`R`] | Row offsets   |         |

Each row looks as follows:

| Offset | Length | Type                | Name                                            | Comment |
| ------ | ------ | ------------------- | ----------------------------------------------- | ------- |
| 0      | 4      | string table offset | Package Name                                    |         |
| 4      | 4      | string table offset | Package Version                                 |         |
| 8      | 4      | int32               | Number of assemblies `A`                        |         |
| 12     | 8*`A`  | (int32, int32)[`A`] | (Framework table offset, assembly table offset) |         |

## Assembly Table

The rows are of varying length, so the header first states how many rows there
are followed by an array with the row start offsets, relative to the table start
(before header):

| Offset | Length | Type       | Name          | Comment |
| ------ | ------ | ---------- | ------------- | ------- |
| 0      | 4      | int32      | Row count `R` |         |
| 4      | 4*`R`  | int32[`R`] | Row offsets   |         |

Each row looks as follows:

| Offset                     | Length | Type                   | Name                                           | Comment |
| -------------------------- | ------ | ---------------------- | ---------------------------------------------- | ------- |
| 0                          | 16     | GUID                   | Fingerprint                                    |         |
| 16                         | 4      | string table offset    | Name                                           |         |
| 20                         | 4      | string table offset    | PublicKeyToken                                 |         |
| 24                         | 4      | string table offset    | Version                                        |         |
| 28                         | 4      | int32                  | Number of root APIs `R`                        |         |
| 32                         | 4*`R`  | API table offset       | Root APIs                                      |         |
| 32 + 4*`R`                 | 4      | int32                  | Number of frameworks `F`                       |         |
| 32 + 4*`R` + 4             | 4*`F`  | framework table offset | Frameworks                                     |         |
| 32 + 4*`R` + 4 + 4*`F`     | 4      | int32                  | Number of packages `P`                         |         |
| 32 + 4*`R` + 4 + 4*`F` + 4 | 8*`P`  | (int32, int32)[`P`]    | (Package table offset, framework table offset) |         |

## Usage Sources Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type                | Name              | Comment |
| ------ | ------ | ------------------- | ----------------- | ------- |
| 0      | 4      | string table offset | Usage Source Name |         |
| 4      | 4      | int32               | Day number        |         |

> [!NOTE]
>
> The day number is used to construct the date indicating how recent the usage
> source is, via `DateOnly.FromDayNumber(int)`.

## API Table

The API table is a serialized tree of the APIs; generally speaking the rows are
of varying lengths and the consumer will have to have an offset to a particular
API.

The table starts with the roots:

| Offset | Length | Type       | Name             | Comment                          |
| ------ | ------ | ---------- | ---------------- | -------------------------------- |
| 0      | 4      | int32      | Node Count `N`   |                                  |
| 4      | 4*`N`  | int32[`N`] | API table offset | Points to all the top-level APIs |

Each API looks as follows:

| Offset                     | Length | Type                | Name                                         | Comment |
| -------------------------- | ------ | ------------------- | -------------------------------------------- | ------- |
| 0                          | 16     | GUID                | Fingerprint                                  |         |
| 16                         | 1      | byte                | API kind                                     |         |
| 17                         | 4      | API table offset    | Parent                                       |         |
| 21                         | 4      | string table offset | Name                                         |         |
| 25                         | 4      | int32               | Child count `C`                              |         |
| 29                         | 4*`C`  | API table offset[C] | Children                                     |         |
| 29 + 4*`C`                 | 4      | int32               | Declaration Count `D`                        |         |
| 29 + 4*`C` + 4             | 8*`D`  | (int32, int32)[`D`] | (Assembly table offset, string table offset) |         |
| 29 + 4*`C` + 4 + 8*`D`     | 4      | int32               | Usage data point count `U`                   |         |
| 29 + 4*`C` + 4 + 8*`D` + 4 | 8*`U`  | (int32, float)[`U`] | (Usage data source offset, percentage)       |         |

## Obsoletion Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type                  | Name          | Comment |
| ------ | ------ | --------------------- | ------------- | ------- |
| 0      | 4      | API table offset      | API           |         |
| 4      | 4      | assembly table offset | Assembly      |         |
| 8      | 4      | string table offset   | Message       |         |
| 12     | 1      | bool                  | Is Error      |         |
| 13     | 4      | string table offset   | Diagnostic ID |         |
| 17     | 4      | string table offset   | URL Format    |         |

## Platform Support Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type                  | Name          | Comment |
| ------ | ------ | --------------------- | ------------- | ------- |
| 0      | 4      | API table offset      | API           |         |
| 4      | 4      | assembly table offset | Assembly      |         |
| 8      | 4      | platform table offset | Platform      |         |
| 12     | 1      | bool                  | Is Supported  |         |

## Preview Requirement Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type                  | Name     | Comment |
| ------ | ------ | --------------------- | -------- | ------- |
| 0      | 4      | API table offset      | API      |         |
| 4      | 4      | assembly table offset | Assembly |         |
| 8      | 4      | string table offset   | Message  |         |
| 12     | 4      | string table offset   | URL      |         |

## Experimental Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type                  | Name          | Comment |
| ------ | ------ | --------------------- | ------------- | ------- |
| 0      | 4      | API table offset      | API           |         |
| 4      | 4      | assembly table offset | Assembly      |         |
| 8      | 4      | string table offset   | Diagnostic ID |         |
| 12     | 4      | string table offset   | URL Format    |         |

## Extension Methods Table

The table starts with a header that states how many rows are in the table:

| Offset | Length | Type  | Name      | Comment |
| ------ | ------ | ----- | --------- | ------- |
| 0      | 4      | int32 | Row count |         |

Each row looks as follows:

| Offset | Length | Type             | Name                  | Comment |
| ------ | ------ | ---------------- | --------------------- | ------- |
| 0      | 4      | GUID             | Extension Method GUID |         |
| 4      | 4      | API table offset | Extended Type         |         |
| 8      | 4      | API table offset | Extension Method      |         |
