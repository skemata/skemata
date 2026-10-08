# Targets `csv`, `csv-semicolon` and `csv-tab`

Describe a **CSV file with a header row**, as defined by RFC 4180: records of fields separated by a
delimiter, fields optionally enclosed in double quotes, and a first record that names the columns. The three
targets differ only in the delimiter; everything else in this chapter applies to all of them.

| Target | Delimiter |
|---|---|
| `csv` | comma `,` |
| `csv-semicolon` | semicolon `;` |
| `csv-tab` | tab |

A file does not reliably reveal its delimiter, so the schema states it through its target
([8.2](../08-target-profiles.md#82-target-names)).

## 1 Native structure

```csv
username,display_name,email,quota_mb
jdoe,Jane Doe,jane.doe@example.org,2048
asmith,"Smith, Alex",alex.smith@example.org,
```

A CSV file is a table: one header record with the column names, followed by data records with one field per
column. All fields are text.

## 2 Mapping

| Data model | CSV |
|---|---|
| root array | the data records of the file |
| item of the root array (an object) | one data record |
| member of a record (a scalar) | a field; the member name is the column name from the header |

**[CSV-1]** The root schema MUST describe an array whose `items` subschema describes an object (the records).
Every member of a record object MUST be a scalar.

The root array may declare `identity` (for example a user name column), `ordered: false` (if the order of
the records carries no meaning), `minItems` and `maxItems`.

## 3 Syntax

**[CSV-2]** A document follows the syntax of RFC 4180, with the delimiter of its target. Fields that contain
the delimiter, double quotes or line breaks are enclosed in double quotes, and a double quote inside such a
field is written twice.

The file encoding is outside the data model.

## 4 Header and names

**[CSV-3]** The first record is the header. Column names are compared case-sensitively, after removing
enclosing quotes, and MUST NOT be empty. If a declared member's name occurs more than once in the header, a
data validator MUST report the data as ambiguous.

A declared member whose column is missing from the header is absent in every record. Columns that the schema
does not declare are undescribed content ([3.3.1](../03-data-model.md#331-open-content)).

## 5 Values

All values are text. Integers and numbers use the lexical forms of
[5.8](../05-types-and-formats.md#58-lexical-forms), with `.` as the decimal separator. There is no boolean and
no null; on/off values are labeled enums of their stored text, such as `"true"`/`"false"`
([TYP-3](../05-types-and-formats.md#54-booleans)).

**[CSV-4]** An empty field in a column of type `string` holds the empty string. An empty field in a column of
any other type means that the value is absent. For such a column, `required` therefore means that the field
must not be empty.

**[CSV-5]** Every data record MUST have as many fields as the header. A record with more or fewer fields makes
the data invalid.

## 6 Representability limits

- Files without a header record cannot be described, because their columns have no names.
- Nested values (objects or arrays inside a field) cannot be described.
- Locale-specific number forms, such as a decimal comma in spreadsheet exports, cannot be described in
  draft-01; they would need a separate target ([8.2](../08-target-profiles.md#82-target-names)).
- Comment lines and several tables in one file, which some tools support, are outside the data model.

## 7 Examples

| Example | Shows |
|---|---|
| [user-import](../examples/csv/user-import.skemata.yaml) | `csv`: keyed records, type inference, empty fields as absent values, a spelling-tolerant on/off column, a unit |
| [printer-mapping](../examples/csv/printer-mapping.skemata.yaml) | `csv-semicolon`: a two-column identity, unordered records |
| [invalid/](../examples/csv/invalid/) | a nested object in a record |
