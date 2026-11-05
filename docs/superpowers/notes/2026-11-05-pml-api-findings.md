# E3D PML / API findings (Task 1 spike)

Verified on 2026-11-05 against a running local E3D 3.1 (Design Mk3.1.8.0, project CAR, MDB `001-REFINERIA-CAR`), through a
throwaway probe endpoint (not committed). Each answer is what later tasks rely on.

## 1. Detecting PML command errors
- `Command.RunInPdms()` **returns `false` for an invalid command** (e.g. `var !!x current MDB`, `collect all FOO`,
  `collect ... with (FOO EQ 1)`) and `true` for valid ones. This is the reliable error signal.
- `Command.Error` is a `PdmsMessageImpl` with only `MessageNumber` and `ModuleNumber` (both `0` in every case tried);
  **no message text**. `Command.Result` is empty. The real error text goes to the E3D console only.
- `Command.OutputAndClearError()` is a **static** method (`ac.Command.OutputAndClearError()`); call it after a failure so the
  next command starts clean.
- Decision: error message returned to callers is generic: `PML command failed: <command>`. Valid-but-empty cases
  (e.g. `for =0/0`, a `with` that matches nothing) return `true` with an empty array.

## 2. Reading arrays in order
- `ac.Command.GetStringArrayFromPML("!!name")` is **static**, needs the `!!` prefix (`"name"` returns an empty array), and
  returns the elements **in PML order** (295 sites; first element equals `!!name[1]`).
- The existing `GetArrayVariable` (Hashtable) loses the order: confirmed (its first element differed from `!!sites[1]`).

## 3. Attributes in C#
- `DbElement.GetElement("=123/45")` works for refs; for an invalid ref (`=0/0`, `garbage`) and for **names** (`/SITE-NAME`)
  it returns an element with `IsValid == false` (no exception, not null). So Get/Children accept dbrefs only; names must
  be resolved through PML.
- `DbAttribute.GetDbAttribute("NAME")` works, is case-insensitive (`:tp-mto-area` -> `:TP-MTO-AREA`), and returns **`null`
  for an unknown attribute** (including an unknown UDA); no exception. UDAs (`:TP-MTO-AREA`) work with the leading colon.
- `element.GetAsString(attribute)` returns strings with units (`BORE` -> `350mm`) and the literal `unset` for unset
  values (`DESC`, unset UDAs). `REF` returns `=123/45`; `TYPE` returns `PIPE`.
- `element.GetElementType()` gives the type (`ToString()` = `PIPE`).

## 4. Children and owner
- `element.Members()` -> `DbElement[]` (in database order); `element.Owner` -> `DbElement` (`WORLD` for a SITE).
  A SITE with no zones gives `members=0`. Type and name come from `GetElementType()` / `GetAsString(NAME)`.

## 5. Attribute names of a type
- `DbElementType.GetElementType("PIPE")` then `SystemAttributes()` (58 for PIPE) + `GetAllUdas()` (133) -> `DbAttribute[]`;
  `DbAttribute.Name` gives the name (`NAME`, `DESC`, ..., `:TP-MTO-AREA`). `getProperties()` (16) are internal-ish
  properties; not used.
- **`DbElementType.GetElementType("FOO")` with an unknown name crashed the whole E3D session.** Always validate the type
  name first against `DbElementType.GetAllElementTypes()` (1884 types; `.Name` is the type name) and only then call
  `GetElementType`. Cache the name set.

## 6. Session info
- `Project.CurrentProject`: `Code` = `CAR`, `Name` = `CAR`, `UserName` = `SYSTEM` (E3D user), `LoginUser` = Windows user.
- `MDB.CurrentMDB.Name` = `001-REFINERIA-CAR`.
- E3D version: `FileVersionInfo` of `Aveva.Core.Database` gives the monitor `1.3.12.0`; the module banner says
  `Design Mk3.1.8.0`. Use `Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion` for `E3dVersion` and
  report `null` if it throws.
- Current module: no PML command found (`module`, `current module`, `modname` are invalid). Fallback: parse
  `-modulename=<X>` from `Environment.CommandLine` (the TTY `des.exe` runs with `-modulename=Design`); `null` if absent.

## 7. `collect` behaviour summary

| Command | RunInPdms |
|---|---|
| `var !!x collect all SITE` | true (295) |
| `var !!x collect all SITE with (NAME EQ \|/NOPE\|)` | true, empty |
| `var !!x collect all SITE for =0/0` | true, empty |
| `var !!x collect all FOO` | **false** |
| `var !!x collect all SITE with (FOO EQ 1)` | **false** |

So an unknown type or an unknown attribute inside `with` is detected through the `false` return; an unknown scope is not.
