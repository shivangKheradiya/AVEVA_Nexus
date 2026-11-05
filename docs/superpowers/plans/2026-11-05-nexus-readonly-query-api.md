# Nexus Generic Query API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add generic, read-oriented endpoints to Nexus (Query, Get, Children, AttributeList, Session/Info, PML/Run), fix `Execute` so PML errors are reported, and add a read-only mode, so an MCP server (separate plan) can query E3D and test PML code.

**Architecture:** Same layering as the repo (Router -> Controller -> Service -> `IAvevaProvider`). Input validation and result shaping are pure C# with no AVEVA references, so they are unit-tested with a plain console runner. Everything that touches E3D lives in `AvevaProvider` and is serialised behind one lock. E3D-dependent behaviour is verified by integration tests (Python `pytest`) against a running local E3D.

**Tech Stack:** C# / .NET Framework 4.8, x86, PML.NET (E3D 3.1), Newtonsoft.Json 13.0.1 (already a dependency). Build with the VS BuildTools Roslyn `csc.exe`. Python 3.12 + `pytest` for integration tests only.

**Spec:** `C:\Users\atormos\OneDrive - TEN\Documents\Obsidian Vault\2 Projectes\AVEVA Nexus MCP\AVEVA Nexus MCP - Arquitectura i API.md` (sections "Endpoints actuals", "Endpoints proposats", "Seguretat"). Context and history: `...\AVEVA Nexus MCP - Bitàcola.md` and `...\AVEVA Nexus MCP - Desplegament i execució.md` in the same folder.

## Global Constraints

- Target .NET Framework 4.8, platform x86. No new NuGet dependencies (Newtonsoft.Json only).
- New code follows the existing layers; only `Integration/AvevaProvider.cs` may call AVEVA APIs. Files outside `Integration/` must not reference `Aveva.*` namespaces (needed so the unit-test runner can compile them).
- Every response is `ApiResponse` JSON (`Success`, `Data`, `Error`, `Message`, `RequestId`). Errors are thrown as `NexusException(message, statusCode)`; status codes used: 400 bad input, 401 token, 403 read-only, 404 not found, 500 internal.
- Routes are case-insensitive. Existing routes and their behaviour stay as they are, except `PML/Execute` (Task 4) and the array-order fix (Task 4).
- Token (`X-Nexus-Token`) and CORS behaviour in `SimpleHttpServer` stay unchanged. `GET /api/Health` stays unauthenticated.
- All access to E3D is serialised (one lock); PML is not thread-safe.
- Query defaults: `limit` default 100, maximum 1000; `offset` default 0.
- Read-only mode is controlled by env var `NEXUS_READONLY`; it is **on unless set to `0`**. When on, `PML/Execute` and `PML/Run` return 403.
- New source files must be added as `<Compile Include=...>` items in `AVEVA_Nexus/AVEVA_Nexus/AVEVA_Nexus.csproj` (old-style project).
- No secrets, tokens or credentials in the repo or in test files.

## Review Focus

Inputs and conditions the spec implies but no endpoint listing exercises (each has a test in the owning task):

1. `type` that does not exist (e.g. `FOO`) in Query: 400 with the PML error text, never 200 with empty rows. (Task 5)
2. `where` / `scope` carrying injection (`;`, newline, `$M`, `SAVEWORK`, `DELETE`, `NEW`): 400, nothing executed. (Task 2, Task 5)
3. Very large result (all PIPEs) and `offset` beyond `total`: bounded rows, correct `total`, empty `rows` for out-of-range offset. (Task 3, Task 5)
4. Unknown attribute or a UDA the element does not have: that cell is `null`, the request still succeeds. (Task 5, Task 6)
5. Invalid or non-existent dbref (`=0/0`, `garbage`) in Get/Children: 404/400, never 500 or a crash of the E3D session. (Task 6)
6. Concurrent requests: no interleaving of PML variables between two simultaneous Queries. (Task 4, Task 5)

---

### Task 0: Branch, baseline, build and test tooling

**Files:**
- Create: `scripts/build.ps1`, `scripts/run-tests.ps1`, `scripts/deploy-local.ps1`
- Create: `tests/Nexus.Tests/Check.cs`, `tests/Nexus.Tests/Program.cs`
- Create: `tests/integration/conftest.py`, `tests/integration/test_health.py`
- Modify: `.gitignore` (add `out/`, `tests/**/__pycache__/`, `.pytest_cache/`)

**Interfaces:**
- Produces: `scripts/build.ps1 [-E3dDir <path>] [-Out <dir>]` compiles `AVEVA_Nexus/AVEVA_Nexus/**/*.cs` to `<Out>\AVEVA_Nexux_Addin.dll` (default `out\`, default E3dDir `C:\Program Files (x86)\AVEVA\Everything3D3.1`) and exits non-zero on compile errors.
- Produces: `scripts/run-tests.ps1` compiles `tests/Nexus.Tests/*.cs` plus the AVEVA-free sources (`Exceptions/*.cs`, `Validation/*.cs`, `Models/*.cs`, `Query/*.cs` as they appear) into `out\Nexus.Tests.exe`, runs it, exits non-zero if any check fails.
- Produces: `Check` helper: `Check.Equal<T>(T expected, T actual, string name)`, `Check.Throws<TEx>(Action act, string name)` returning a failing count surfaced by `Program.Main` as the exit code. Tests register themselves with `Program.Run(string name, Action test)`.
- Produces: `scripts/deploy-local.ps1 -Target <dir>` : builds, stops the local E3D session via `<Target>\Stop-Nexus.ps1 -Project CAR`, copies the DLL and `Newtonsoft.Json.dll` to `<Target>\bin`, starts with `<Target>\Start-Nexus.ps1 -Project CAR -NoWait` (plus `-Writable` when the deploy script is called with `-Writable`, available from Task 8), then polls `http://localhost:5001/api/Health` up to 120 s.
- Produces: `tests/integration/conftest.py` exposing fixtures `base_url` (env `NEXUS_URL`, default `http://localhost:5001`) and `api(method, path, body=None) -> (status:int, json:dict)` using `urllib`, plus auto-skip of the whole suite when `/api/Health` is unreachable.

- [ ] **Step 1: Create the branch and commit the current baseline.** `git checkout -b feat/generic-query-api` in `D:\ProjectesRandom\AVEVA_Nexus`; review `git diff` for secrets (there must be none); commit the existing working-tree changes (Newtonsoft instead of System.Text.Json, `Stop()` fix, token/CORS/Health in `SimpleHttpServer`/`Router`, `NEXUS_PORT` auto-start in the addin, csproj reference changes) with message `fix: build on .NET Framework 4.8, add token auth, CORS, health and addin autostart`.
- [ ] **Step 2: Write `scripts/build.ps1`** (Roslyn csc from `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe`, `-target:library -platform:x86`, references: the five E3D DLLs from `-E3dDir`, `packages\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll`, `System.Configuration.dll`, `System.Net.Http.dll`, `-recurse:*.cs` under the project folder).
- [ ] **Step 3: Run it.** `powershell -File scripts\build.ps1` -> Expected: produces `out\AVEVA_Nexux_Addin.dll`, no errors.
- [ ] **Step 4: Write `tests/Nexus.Tests/Check.cs`, `Program.cs` and `scripts/run-tests.ps1`**, with one trivial self-test (`Program.Run("runner works", () => Check.Equal(1, 1, "one"))`). Run `powershell -File scripts\run-tests.ps1` -> Expected: prints `1 passed, 0 failed`, exit code 0. Temporarily change the expectation to confirm it prints a failure and exits non-zero, then restore.
- [ ] **Step 5: Write `tests/integration/conftest.py` and `test_health.py`** (`test_health_ok`: `GET /api/Health` -> status 200 and `json["Success"] is True`). Run `pip install pytest` once, then `python -m pytest tests/integration -v` with the local E3D running -> Expected: 1 passed; with E3D stopped -> Expected: skipped.
- [ ] **Step 6: Write `scripts/deploy-local.ps1`**, run it against `D:\ProjectesRandom\AVEVA_Nexus_local` -> Expected: ends with `UP` and `curl http://localhost:5001/api/Health` returns `"Data": "ok"`.
- [ ] **Step 7: Commit** `chore: add build, unit-test and local deploy scripts, integration test scaffold`, then `git push -u origin feat/generic-query-api`.

---

### Task 1: Verify the E3D facts the design relies on (spike)

Unknowns that decide later tasks. Resolve them on the running local E3D and record the answers; do not build features here.

**Files:**
- Create: `docs/superpowers/notes/2026-11-05-pml-api-findings.md`
- Create (throwaway, not committed): `scratch/Probe.cs` or use the existing `PML/Execute` + `Variable` endpoints

**Interfaces:**
- Produces: `findings.md` answering, each with the exact PML or C# call that works (or "not possible" plus the fallback): 
  1. `Command.Error` (`PdmsMessage`): which member holds the error text/number; is it set after an invalid command such as `var !!x current MDB`; how it is cleared (`OutputAndClearError`).
  2. `Command.GetStringArrayFromPML("!!arrayName")`: exact name form (`"!!sites"` vs `"SITES"`), returns elements in order for a `var !!sites collect all SITE` result.
  3. Attribute read in C#: `DbElement.GetElement(string)` for `=123/45`; `DbAttribute.GetDbAttribute(":UDA")` for a UDA and for an unknown name (exception or null); `GetAsString` result for an unset attribute; how to get the element type name.
  4. Children and owner in C#: `DbElement.Members()` / `Owner` (or the PML equivalent) and filtering by type.
  5. Valid attribute names for a type (for `AttributeList`): `DbElementType.GetElementType("PIPE")` and its attribute collection, or PML `q ATTLIST`.
  6. Session info: current project, MDB, user, module via C# (`Project.CurrentProject`, `MDB.CurrentMDB`, ...) or PML.
  7. Whether `collect all <TYPE> with (<expr>) for <ref>` raises or silently yields an empty array on an unknown TYPE (observed: invalid commands return `Success: true` with no variable).
- Consumes: local E3D running (Task 0).

- [ ] **Step 1:** For each unknown, run the probe and capture the observed output verbatim in `findings.md` (call, input, result).
- [ ] **Step 2:** Where an unknown has no working answer, write the fallback chosen (e.g. "use PML `q ATTLIST` and parse") so later tasks need no further decision.
- [ ] **Step 3: Commit** `docs: record E3D PML/API findings for query endpoints`.

---

### Task 2: Input validation (pure logic)

**Files:**
- Create: `AVEVA_Nexus/AVEVA_Nexus/Validation/PmlInputValidator.cs`
- Test: `tests/Nexus.Tests/PmlInputValidatorTests.cs`
- Modify: `AVEVA_Nexus.csproj` (Compile Include)

**Interfaces:**
- Produces (namespace `AVEVA_Nexus.Validation`, all `static`, all throw `NexusException(msg, 400)` on bad input):
  - `string Type(string value)` -> trimmed, upper-cased; allows `[A-Za-z0-9_]{1,32}`.
  - `string Attribute(string value)` -> trimmed, upper-cased; allows `[A-Za-z0-9_:]{1,64}` (UDAs start with `:`).
  - `string DbRef(string value)` -> `=<digits>/<digits>` only.
  - `string Scope(string value)` -> `null` if null/blank; else a `DbRef` or a name starting with `/` (`[A-Za-z0-9_\-/.]`).
  - `string Where(string value)` -> `null` if null/blank; else the trimmed expression, rejected if it contains `;`, newline, `$`, a backtick, or (case-insensitive, as whole words) any of `SAVEWORK`, `DELETE`, `REMOVE`, `NEW`, `UNDO`, `STORE`, `MACRO`, `FINISH`, `QUIT`, `ONERROR`, `HANDLE`; also rejected if parentheses are unbalanced or length > 500.

- [ ] **Step 1: Write failing tests** with these names and assertions: `Type_accepts_PIPE_and_uppercases("pipe" -> "PIPE")`, `Type_rejects_space_and_semicolon("PIPE;x", "A B")`, `Attribute_accepts_UDA(":TPSPEC")`, `DbRef_accepts("=201346360/1")` and `DbRef_rejects("garbage", "=1", "=1/2;x")`, `Scope_null_is_null`, `Scope_accepts_name_and_dbref("/CAR-480-PI", "=1/2")`, `Where_accepts("(:TPSPEC EQ 'A') AND (BORE GT 50)")`, `Where_rejects_each_forbidden("a; b", "a\nb", "$M/x", "SAVEWORK", "delete all", "NEW PIPE", "(unbalanced")`, `Where_does_not_reject_substrings("RENEWAL EQ 1")` (whole-word match only). Each rejection asserts `NexusException` with `StatusCode == 400`.
- [ ] **Step 2: Run `scripts\run-tests.ps1`** -> Expected: FAIL (type not defined / compile error).
- [ ] **Step 3: Implement `PmlInputValidator`** with `System.Text.RegularExpressions`; keep the forbidden-word list in one `static readonly string[]`.
- [ ] **Step 4: Run `scripts\run-tests.ps1`** -> Expected: all passed.
- [ ] **Step 5: Commit** `feat: add PML input validation for generic queries`.

---

### Task 3: Request/response models and paging (pure logic)

**Files:**
- Create: `AVEVA_Nexus/AVEVA_Nexus/Models/QueryRequest.cs`, `GetRequest.cs`, `TableResult.cs`
- Create: `AVEVA_Nexus/AVEVA_Nexus/Query/Paging.cs`
- Test: `tests/Nexus.Tests/PagingTests.cs`, `tests/Nexus.Tests/RequestParsingTests.cs`
- Modify: `AVEVA_Nexus.csproj`

**Interfaces:**
- Produces (namespace `AVEVA_Nexus.Models`):
  - `class QueryRequest { string Type; string Scope; string Where; List<string> Attributes; int? Limit; int? Offset; }`
  - `class GetRequest { List<string> Refs; List<string> Attributes; }`
  - `class TableResult { int Total; int Offset; int Limit; List<string> Columns; List<Dictionary<string,object>> Rows; }` where each row has key `"dbref"` plus one key per requested attribute.
- Produces (namespace `AVEVA_Nexus.Query`): `static class Paging { static (int offset, int limit) Normalize(int? offset, int? limit); static List<T> Slice<T>(IList<T> all, int offset, int limit); }` — `Normalize` applies defaults (offset 0, limit 100), clamps `limit` to 1..1000 and `offset` to >= 0.

- [ ] **Step 1: Write failing tests:** `Normalize_defaults(null,null -> 0,100)`, `Normalize_clamps(limit 5000 -> 1000, limit 0 -> 1, offset -5 -> 0)`, `Slice_in_range`, `Slice_offset_beyond_total_returns_empty` (Review Focus 3), `Slice_limit_beyond_end`, and request parsing `QueryRequest_deserializes_camel_and_pascal_case` via `JsonConvert.DeserializeObject<QueryRequest>("{\"type\":\"PIPE\",\"attributes\":[\"NAME\"]}")` (the unit-test project references Newtonsoft.Json).
- [ ] **Step 2: Run tests** -> Expected: FAIL.
- [ ] **Step 3: Implement the models and `Paging`.**
- [ ] **Step 4: Run tests** -> Expected: all passed.
- [ ] **Step 5: Commit** `feat: add query request/result models and paging`.

---

### Task 4: Provider hardening (serialisation lock, array order, PML error reporting)

**Files:**
- Create: `AVEVA_Nexus/AVEVA_Nexus/Integration/AvevaGate.cs`
- Modify: `AVEVA_Nexus/AVEVA_Nexus/Integration/AvevaProvider.cs`, `IAvevaProvider.cs`, `Services/PmlService.cs`, `Controllers/PmlController.cs`
- Test: `tests/integration/test_pml.py`

**Interfaces:**
- Produces: `static class AvevaGate { static T Run<T>(Func<T> work); }` — executes `work` under one process-wide lock. Every public `AvevaProvider` method wraps its body in `AvevaGate.Run`.
- Changes: `AvevaProvider.ExecuteCommand(string)` now returns `bool` = true only if `Command.Error` is empty after `RunInPdms()`; otherwise throws `NexusException(<PML error text>, 400)` (error text taken from `PdmsMessage` per the Task 1 findings, then cleared with `OutputAndClearError`).
- Changes: `GetArrayVariable(string)` returns the elements in PML order using `Command.GetStringArrayFromPML` (Task 1 findings give the exact name form) instead of iterating a `Hashtable`.
- Consumes: Task 1 findings.

- [ ] **Step 1: Write failing integration tests** (`pytest`, local E3D with `NEXUS_READONLY=0` for these): `test_execute_reports_pml_error` (POST `Execute` `{"command":"var !!t current MDB"}` -> status 400 and `Error` non-empty), `test_execute_valid_ok` (`!!nxA = |x|` -> 200), `test_array_keeps_order` (`var !!nxArr collect all SITE`, GET `Variable/Array/nxArr` equals the order of `!!nxArr[1]`, `!!nxArr[2]` read back through `!!x = !!nxArr[n]` string variables for the first three), `test_parallel_executes_all_succeed` (20 threads each POST `Execute` `!!nxP<i> = |<i>|`; all return 200, `GET Variable/String/nxP<i>` returns `<i>` for every thread, and `/api/Health` still answers).
- [ ] **Step 2: Run** `python -m pytest tests/integration/test_pml.py -v` -> Expected: the error and order tests FAIL.
- [ ] **Step 3: Implement** `AvevaGate`, wrap provider methods, add error capture and ordered arrays. Note in the commit that `Execute` now returns 400 on PML errors (behaviour change). If Task 1 shows `Command.Error` is not populated for some failing commands (e.g. assignments that silently create nothing), the fallback decided there applies; the test above must still pass.
- [ ] **Step 4: Deploy locally** (`scripts\deploy-local.ps1 -Target D:\ProjectesRandom\AVEVA_Nexus_local`) and re-run the tests -> Expected: all passed.
- [ ] **Step 5: Commit** `fix: report PML errors from Execute, keep array order, serialise E3D access`.

---

### Task 5: `POST /api/Element/Query`

**Files:**
- Create: `AVEVA_Nexus/AVEVA_Nexus/Services/QueryService.cs`
- Modify: `Integration/IAvevaProvider.cs`, `Integration/AvevaProvider.cs`, `Controllers/ElementController.cs`, `Http/Router.cs`
- Test: `tests/integration/test_query.py`

**Interfaces:**
- Produces on `IAvevaProvider`: `string[] CollectRefs(string type, string scope, string where)` — runs `var !!nexusQ collect all <type> [with (<where>)] [for <scope>]` (inputs already validated), reads `!!nexusQ` with `GetStringArrayFromPML`, then removes the variable; throws `NexusException(<PML error>, 400)` if `Command.Error` is set; `Dictionary<string,object> ReadAttributes(string dbref, IList<string> attributes)` — C# `DbElement`/`DbAttribute` per Task 1 findings; unknown attribute or unset value -> `null`.
- Produces: `QueryService.Run(QueryRequest request) -> TableResult` (validates with `PmlInputValidator`, collects, pages with `Paging`, reads attributes only for the page rows, `Total` = number of refs before paging). `attributes` empty -> columns `["dbref"]` only.
- Produces: route `POST /api/Element/Query` -> `ElementController.Query(string json) -> ApiResponse` (`Data` = `TableResult`).

- [ ] **Step 1: Write failing integration tests:** `test_query_all_sites_has_total_and_rows` (`{"type":"SITE","attributes":["NAME"],"limit":5}` -> 200, `Data.Total >= 5`, 5 rows each with `dbref` and `NAME` starting with `/`), `test_query_scope_and_where` (`type PIPE`, `scope` a known site dbref taken from the previous query -> rows all non-empty `dbref`; `where` `"(NAME EQ '/DOES-NOT-EXIST')"` -> `Total == 0`), `test_query_unknown_type_is_400` (`FOO` -> 400, `Error` mentions the PML failure; Review Focus 1), `test_query_injection_is_400` (`where` `"1 EQ 1; SAVEWORK"` -> 400; Review Focus 2), `test_query_offset_beyond_total` (offset 100000 -> 200, `Rows == []`, `Total` unchanged; Review Focus 3), `test_query_unknown_attribute_is_null` (attributes `["NAME", "NOSUCHATTR"]` -> 200 and the second column `null`; Review Focus 4), `test_query_limit_is_capped` (`limit 99999` -> `Data.Limit == 1000`).
- [ ] **Step 2: Run** -> Expected: FAIL (404 route not found).
- [ ] **Step 3: Implement** provider methods, `QueryService`, controller method and the router branch (case-insensitive `Element/Query`, POST only).
- [ ] **Step 4: Deploy locally, run tests** -> Expected: all passed. Also `python -m pytest tests/integration -v` stays green for earlier tests.
- [ ] **Step 5: Commit** `feat: add generic Element/Query endpoint`.

---

### Task 6: `Element/Get`, `Element/Children`, `Element/AttributeList`

**Files:**
- Modify: `Integration/IAvevaProvider.cs`, `Integration/AvevaProvider.cs`, `Services/ElementService.cs`, `Controllers/ElementController.cs`, `Http/Router.cs`
- Test: `tests/integration/test_element.py`

**Interfaces:**
- Produces on `IAvevaProvider`: `string[] GetChildRefs(string dbref, string type)` (owner's members, optionally filtered by type), `string GetOwnerRef(string dbref)`, `string[] GetAttributeNames(string type)` (per Task 1 findings).
- Produces routes: `POST /api/Element/Get` (body `GetRequest`) -> `Data` = `TableResult`-shaped rows, one per ref, in input order; `GET /api/Element/Children?dbref=&type=` -> `Data` = `{ owner: string, children: [{dbref, type, name}] }`; `GET /api/Element/AttributeList?type=` -> `Data` = `string[]`.
- Behaviour: a dbref that does not resolve -> `NexusException(..., 404)`; malformed dbref -> 400 (via `PmlInputValidator.DbRef`). Existing `GET /api/Element/Attribute` is unchanged.

- [ ] **Step 1: Write failing tests:** `test_get_batch_returns_rows_in_input_order` (two refs from a Query, attributes `["NAME","TYPE"]`), `test_get_invalid_ref_is_404_not_500` (`=0/0` -> 404; `garbage` -> 400; Review Focus 5), `test_children_of_site_are_zones` (a site ref -> children non-empty, every `type == "ZONE"`; with `type=PIPE` on a site -> empty list, 200), `test_children_invalid_ref` (`=0/0` -> 404), `test_attributelist_pipe_contains_NAME` (`type=PIPE` -> list includes `"NAME"`), `test_attributelist_unknown_type_is_400`.
- [ ] **Step 2: Run** -> Expected: FAIL.
- [ ] **Step 3: Implement** provider, service, controller, router.
- [ ] **Step 4: Deploy locally, run all integration tests** -> Expected: all passed.
- [ ] **Step 5: Commit** `feat: add Element/Get, Children and AttributeList endpoints`.

---

### Task 7: `GET /api/Session/Info`

**Files:**
- Create: `AVEVA_Nexus/AVEVA_Nexus/Controllers/SessionController.cs`, `Services/SessionService.cs`
- Modify: `Integration/IAvevaProvider.cs`, `Integration/AvevaProvider.cs`, `Http/Router.cs`
- Test: `tests/integration/test_session.py`

**Interfaces:**
- Produces on `IAvevaProvider`: `SessionInfo GetSessionInfo()`; model `SessionInfo { string Project; string Mdb; string User; string Module; string E3dVersion; bool ReadOnly; }` in `Models/SessionInfo.cs`. Fields that Task 1 finds unobtainable are `null`, never an exception.
- `ReadOnly` reflects `NEXUS_READONLY` (see Task 8; until then, computed by the same helper `ReadOnlyMode.IsOn`).
- Produces: `static class ReadOnlyMode { static bool IsOn { get; } }` in `Config/ReadOnlyMode.cs` (true unless env `NEXUS_READONLY == "0"`).

- [ ] **Step 1: Write failing tests:** `test_session_info_has_project_and_module` (`Data.Project == "CAR"`, `Data.Module` non-empty, `Data.ReadOnly` is a bool), and a unit test `ReadOnlyMode_defaults_on_and_off_with_0` in `tests/Nexus.Tests` (set/unset the env var inside the test and restore it).
- [ ] **Step 2: Run both suites** -> Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Deploy locally, run both suites** -> Expected: all passed.
- [ ] **Step 5: Commit** `feat: add Session/Info and read-only mode flag`.

---

### Task 8: Read-only enforcement and `POST /api/PML/Run`

**Files:**
- Modify: `Http/Router.cs`, `Controllers/PmlController.cs`, `Services/PmlService.cs`, `Integration/IAvevaProvider.cs`, `Integration/AvevaProvider.cs`
- Create: `Models/RunRequest.cs`, `Models/RunResult.cs`
- Modify (outside repo, same task): `D:\ProjectesRandom\AVEVA_Nexus_local\Start-Nexus.ps1` adds a `-Writable` switch that sets env `NEXUS_READONLY=0` in the E3D launch command (`set NEXUS_READONLY=0&&` before `mon.exe`).
- Test: `tests/integration/test_run.py`

**Interfaces:**
- Produces: `RunRequest { string Script; List<string> Return; }` and `RunResult { bool Ok; List<string> Errors; Dictionary<string,object> Values; }`.
- Produces on `IAvevaProvider`: `RunResult RunScript(IList<string> lines, IList<string> returnVariables)` — runs each non-empty line in order, collecting `Command.Error` text per line into `Errors` (continues after an error); after the last line reads every name in `Return` (global, name without `!!`): arrays via `GetStringArrayFromPML`, everything else via `GetStringFromPML`; a name that is not defined -> `null` in `Values`. `Ok == (Errors.Count == 0)`.
- Behaviour: `POST /api/PML/Run` returns 200 with `RunResult` even when `Ok` is false (errors are data for the caller). When `ReadOnlyMode.IsOn`, `POST /api/PML/Execute` and `POST /api/PML/Run` return 403 with `Error` = `"Nexus is in read-only mode (set NEXUS_READONLY=0 to allow PML execution)"`; the read endpoints and `Variable/*` are unaffected.
- Consumes: Task 4 error capture, Task 7 `ReadOnlyMode`.

- [ ] **Step 1: Write failing tests (run with the session started `-Writable`):** `test_run_returns_requested_variables` (`script` `"!!nxT = |abc|\n!!nxN = 42"`, `return ["nxT","nxN"]` -> `Ok`, `Values.nxT == "abc"`), `test_run_collects_array`, `test_run_reports_errors_and_continues` (`"var !!nxE current MDB\n!!nxOk = |y|"`, `return ["nxOk"]` -> 200, `Ok == false`, `Errors` length 1, `Values.nxOk == "y"`), `test_run_undefined_return_is_null`. And a separate read-only check, run against a session started without `-Writable`: `test_readonly_blocks_execute_and_run` (both -> 403 with the message above) and `test_readonly_allows_queries` (`Element/Query` -> 200).
- [ ] **Step 2: Run** -> Expected: FAIL.
- [ ] **Step 3: Implement.** Mark in the test file which tests need `-Writable` via a pytest marker (`@pytest.mark.writable`) and select with `-m`.
- [ ] **Step 4: Deploy locally in both modes and run** `python -m pytest tests/integration -m writable -v` (writable session) and `python -m pytest tests/integration -m "not writable" -v` (read-only session) -> Expected: all passed in both.
- [ ] **Step 5: Commit** `feat: add PML/Run and enforce read-only mode on Execute and Run`.

---

### Task 9: Documentation, final verification, publish

**Files:**
- Modify: `README.md` (API section: new endpoints, `NEXUS_READONLY`, `NEXUS_TOKEN`, `NEXUS_PORT`, error behaviour of `Execute`, how to build/test with `scripts/`)
- Modify (vault, per the `nexus-docs` skill): `Arquitectura i API.md` (move implemented endpoints from "proposats" to "actuals"), `Desplegament i execució.md` (`-Writable`, `NEXUS_READONLY`, build/test scripts), bitácora entry, ficha tasks.

- [ ] **Step 1:** Run both unit and integration suites from a clean local deploy -> Expected: all green in both modes (Task 8 step 4 commands).
- [ ] **Step 2:** Update `README.md` and the vault notes; verify every endpoint in the README is covered by at least one test file.
- [ ] **Step 3: Commit** `docs: document generic query API, read-only mode and scripts`; `git push origin feat/generic-query-api`.
- [ ] **Step 4:** Open nothing automatically (no PR) — report the branch URL `https://github.com/TEN-SPIT/AVEVA_Nexus/tree/feat/generic-query-api` to the user.

---

## Out of scope (next plan)

The Python MCP server (`mcp/`): FastMCP over stdio, tools wrapping these endpoints, session start/stop via the local launcher. It is planned separately once the endpoint shapes above are implemented and verified.
