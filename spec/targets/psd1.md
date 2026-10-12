# Target `psd1`

Describes a **PowerShell data file**: a `.psd1` file that contains a single hashtable literal, as read with
`Import-PowerShellDataFile`. PowerShell module manifests are the best-known example.

## 1 Native structure

```powershell
@{
    ModuleVersion     = '1.4.0'
    FunctionsToExport = @('Get-Example', 'Set-Example')
    PrivateData       = @{
        PSData = @{ Tags = @('example') }
    }
}
```

A data file holds literals only: hashtables `@{ key = value }`, arrays `@( … )` or comma-separated lists,
strings in single or double quotes, numbers, `$true`, `$false` and `$null`. Comments (`#` and `<# … #>`) are
allowed.

## 2 Mapping

| Data model | PowerShell data file |
|---|---|
| root object | the hashtable of the file |
| object | hashtable `@{ … }` |
| array, tuple (`prefixItems`), nested arrays | array `@( … )` or a comma-separated list |
| `string` | string, single- or double-quoted |
| `integer` | integer literal, decimal or hexadecimal (`0x1F`) |
| `number` | floating-point literal; integers are accepted, as in JSON Schema |
| `boolean` | `$true` / `$false` |
| null (nullable types) | `$null` |
| binary | base64 string |
| string formats | string |

**[PSD-1]** The root schema MUST describe an object.

`textual: true` declares an integer or number that is stored as a string in its lexical form.

## 3 Names

**[PSD-2]** Hashtable keys are compared case-insensitively ([3.3.2](../03-data-model.md#332-names)). Keys are
written as bare words or quoted strings; the quoting is syntax. The empty name is not allowed
([MOD-3](../03-data-model.md#332-names)).

## 4 Values

**[PSD-3]** A declared member whose value is an expression rather than a literal (a variable other than
`$true`, `$false` and `$null`, a subexpression `$( … )`, a variable reference inside a double-quoted string, a
command or a script block) makes the data invalid.

**[PSD-4]** Where the schema declares an array of scalars, a single scalar value is read as an array with one
item. Data files commonly write one-element lists that way, for example `FunctionsToExport = 'Get-Example'`.
PSD-4 does not apply within a type union: there, a single scalar selects a scalar branch.

A type union ([KW-32](../04-keywords.md#oneof)) tells its branches apart by the type of the literal: string,
integer, floating-point number, `$true`/`$false`, hashtable, array or `$null`. `integer` and `number` MUST NOT
both be branch types, and at most one branch MAY be nullable.

`format: uint64` is not allowed: Windows PowerShell 5.1 has no literal for unsigned 64-bit integers
([8.5](../08-target-profiles.md#85-type-and-format--target-matrix)).

## 5 Representability limits

- Keys that are not strings, such as numbers, cannot be described.
- Type casts (`[datetime]'2026-10-08'`, `[ordered]@{ … }`), expressions and script blocks are outside the
  data model; dates are described as strings with a `format`.
- Comments and layout are syntax.

## 6 Examples

| Example | Sample data | Shows |
|---|---|---|
| [module-manifest](../examples/psd1/module-manifest.skemata.yaml) | [ExampleModule.psd1](../examples/psd1/ExampleModule.psd1) | case-insensitive keys, nested hashtables, arrays, derived and explicit titles |

Invalid: [PSD-1](../examples/psd1/invalid/PSD-1-array-root.skemata.yaml) array as root.
