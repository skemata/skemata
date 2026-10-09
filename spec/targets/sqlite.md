# Target `sqlite`

Describes **rows of tables in one SQLite database**.

## 1 Native structure

A SQLite database contains tables, and each table contains rows with columns. Every value has one of five
**storage classes**: NULL, INTEGER (signed, up to 8 bytes), REAL (8-byte IEEE floating point), TEXT and BLOB.
SQLite has no Boolean and no date/time storage class. Applications store booleans as integers and dates as
TEXT, REAL or INTEGER.

A column's declared type gives it a **type affinity**, which may convert values when they are stored (for
example, a TEXT-affinity column stores numbers as text). STRICT tables enforce declared types.

## 2 Mapping

| Data model | SQLite |
|---|---|
| root object | the database |
| member of the root (an array) | a table |
| item of a table array (an object) | a row |
| member of a row (a scalar) | a column value |

**[SQL-1]** The root schema MUST describe an object. Tables MUST be declared in `properties`. The root
MAY declare `additionalProperties: false` (no other tables) or `true` (other tables are undescribed content),
but no subschema, and no `patternProperties` or `propertyNames`.

**[SQL-2]** Every table member MUST describe an array whose `items` subschema describes an object (the
rows). Every member of a row object MUST be a scalar (a column).

**[SQL-3]** `required` and `dependentRequired` MUST NOT be used on row objects. Every declared column exists
in every row. Whether a column may be NULL is declared with a nullable type such as `type: [string, "null"]`.
If a table exists but lacks a declared column, the data is invalid. A declared table that does not exist in
the database is absent.

**[SQL-4]** Every table array MUST declare `identity`. The identity columns SHOULD be the table's primary key
or a unique constraint. As with all keyed arrays, no two rows may have the same identity
([KW-13](../04-keywords.md#identity)).

Rows have no meaningful order in SQL. Table arrays are therefore implicitly `ordered: false`, and `ordered`
MUST NOT be declared on them ([8.4](../08-target-profiles.md#84-keyword--target-matrix)).

## 3 Names

**[SQL-5]** Table and column names are compared case-insensitively for ASCII letters, as SQLite does. The
empty name is not allowed ([MOD-3](../03-data-model.md#332-names)).

## 4 Values

| Schema | Storage class |
|---|---|
| `type: string` (with or without `format`) | TEXT |
| `type: integer` | INTEGER |
| `type: number` | REAL (INTEGER also accepted, SQL-7) |
| `textual: true` on `integer` or `number` | TEXT in the lexical form |
| `contentEncoding: base64` | BLOB |
| nullable type | additionally NULL |

**[SQL-6]** A data validator MUST determine each value's storage class (as reported by SQLite's `typeof()`)
and MUST report a value whose storage class does not match the schema.

**[SQL-7]** For `type: number`, a data validator MUST accept both REAL and INTEGER storage classes, because
SQLite may store integral numeric values as INTEGER depending on the column's affinity.

There is no Boolean storage class. On/off columns are labeled enums of their stored integers, typically `1`
and `0` ([TYP-3](../05-types-and-formats.md#54-booleans)). INTEGER is signed, so `format: uint64` is not
allowed.

Authors should choose types that match the columns' declared types. A schema that describes text in an
INTEGER-affinity column, for example, describes data that SQLite may convert when it is stored.

## 5 Representability limits

- Views, indexes, triggers, column defaults, constraints and the table definitions themselves (DDL) are
  outside the data model. A Skemata schema does not create tables.
- The implicit `rowid` is not a column of the data model unless the table declares it as one.
- Tables without a natural or declared key cannot be described (SQL-4).
- Dates stored as REAL (Julian day) or INTEGER (Unix time) are described as numbers or integers. Skemata
  does not interpret them as dates.

## 6 Examples

| Example | Sample data | Shows |
|---|---|---|
| [app-settings-db](../examples/sqlite/app-settings-db.skemata.yaml) | [.sql](../examples/sqlite/app-settings-db.sql) | a key/value table, a table with a two-column key, nullable and textual columns |

Invalid: [TYP-3](../examples/sqlite/invalid/TYP-3-boolean-column.skemata.yaml) boolean column,
[SQL-3](../examples/sqlite/invalid/SQL-3-required-on-rows.skemata.yaml) `required` on rows,
[SQL-4](../examples/sqlite/invalid/SQL-4-table-without-identity.skemata.yaml) table without identity.
