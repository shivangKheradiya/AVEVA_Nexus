# AVEVA_Nexus

Universal HTTP Gateway for Connecting AVEVA Applications with other systems, applications, and AI.

---

# Overview

AVEVA Nexus is a lightweight HTTP gateway designed to expose standard REST APIs for AVEVA products such as:

- AVEVA E3D
- AVEVA Engineering
- AVEVA Diagram
- AVEVA Unified Engineering
- Future AVEVA Products

The primary goal is to provide a common integration layer between AVEVA applications and external consumers such as:

- AI Agents
- Microsoft Copilot
- Custom Applications
- Web Applications
- Desktop Applications
- Automation Frameworks

---

# Vision

Provide a unified capability-based API that abstracts underlying AVEVA implementation details.

External consumers should never need to know whether data is being retrieved using:

- PML
- .NET APIs
- Engineering APIs
- Diagram APIs
- Future AVEVA SDKs

All communication should occur through a stable HTTP interface.

---

# Architecture

## Request Flow

```text
HTTP Request
      │
      ▼
Router
      │
      ▼
Controller
      │
      ▼
Service
      │
      ▼
IAvevaProvider
      │
      ▼
AVEVA Integration Logic
      │
      ▼
ApiResponse
```

---


# Layer Responsibilities

## Addin Layer

Purpose:

- Application startup
- Application shutdown
- HTTP server lifecycle management

Contains:

```text
AVEVA_Nexus_Addin.cs
```

Must NOT contain:

- PML Logic
- Element Logic
- Business Logic

---

## HTTP Layer

Purpose:

- Handle incoming requests
- Route requests
- Return HTTP responses
- Global exception handling

Contains:

```text
SimpleHttpServer.cs
Router.cs
```

Must NOT contain:

- AVEVA Logic

---

## Controller Layer

Purpose:

- Translate HTTP requests into service operations

Contains:

```text
PmlController.cs
ElementController.cs
```

Must NOT contain:

- AVEVA SDK calls

---

## Service Layer

Purpose:

- Application business logic
- Coordination between controllers and providers

Contains:

```text
PmlService.cs
ElementService.cs
```

---

## Provider Layer

Purpose:

- AVEVA product integration

Contains:

```text
AvevaProvider.cs
```

Only this layer should contain:

- PML Execution
- Element Queries
- Future E3D API Calls

---

# API Design

## PML APIs

### Execute PML Command

```http
POST /api/PML/Execute
```

Example Request

```json
{
  "command": "Q MDB"
}
```

Success Response

```json
{
  "success": true
}
```

Failure Response (HTTP 400; PML errors used to be reported as `success: true`)

```json
{
  "success": false,
  "error": "PML command failed: Q MDBX"
}
```

`Execute` is blocked (HTTP 403) while Nexus is in read-only mode, which is the default; see [Environment variables](#environment-variables).

---

### Get String Variable

```http
GET /api/PML/Variable/String/{Name}
```

Example

```http
GET /api/PML/Variable/String/MyVariable
```

Response

```json
{
  "success": true,
  "data": "Sample String"
}
```

---

### Get Real Variable

```http
GET /api/PML/Variable/Real/{Name}
```

Example

```http
GET /api/PML/Variable/Real/MyVariable
```

Response

```json
{
  "success": true,
  "data": 123.45
}
```

---

### Get Boolean Variable

```http
GET /api/PML/Variable/Boolean/{Name}
```

Example

```http
GET /api/PML/Variable/Boolean/MyVariable
```

Response

```json
{
  "success": true,
  "data": true
}
```

---

### Get Array Variable

```http
GET /api/PML/Variable/Array/{Name}
```

Example

```http
GET /api/PML/Variable/Array/MyArray
```

Response

```json
{
  "success": true,
  "data":
  [
    "Item1",
    "Item2",
    "Item3"
  ]
}
```

---

## Element APIs

### Get Element Attribute

Recommended format:

```http
GET /api/Element/Attribute
```

Query Parameters

```text
dbref==1234/5678
attribute=NAME
```

Example

```http
GET /api/Element/Attribute?dbref==1234/5678&attribute=NAME
```

Response

```json
{
  "success": true,
  "data": "/PIPE100"
}
```

---

## Generic query APIs

All responses use the standard `ApiResponse` envelope. Property names in `Data` are as shown. `dbref` values look
like `=123/45`. Unknown element types return 400 (they are checked against the real E3D types first). Attribute
values are E3D text (`350mm`, `unset`); an attribute that does not exist is `null`.

### Health

```http
GET /api/Health
```

No token required. `Data` is `"ok"`.

### Run a PML script

```http
POST /api/PML/Run
```

```json
{ "script": "!!a = |abc|\n!!n = 42", "return": ["a", "n"] }
```

Runs the lines in order and then reads the listed global variables (names without `!!`). A failing line is
recorded in `Errors` and the rest still run; the HTTP status stays 200. Each line is a separate PML command, so
multi-line blocks (`if`/`do`/`define function`) are not supported yet.

```json
{ "Success": true, "Data": { "Ok": true, "Errors": [], "Values": { "a": "abc", "n": 42.0 } } }
```

`Values` are typed: text, number, boolean or a list (arrays); `null` for a variable that is not defined.
Blocked (403) in read-only mode.

### Query elements

```http
POST /api/Element/Query
```

```json
{
  "type": "PIPE",
  "scope": "=201346360/2",
  "where": "(BORE GT 300)",
  "attributes": ["NAME", "BORE", ":MY-UDA"],
  "limit": 100,
  "offset": 0
}
```

`type` is required. `scope` is a dbref or a name starting with `/` (optional; 404 if it does not exist). `where` is
a PML condition (optional). It is checked, not trusted: the text inside `'...'` and `|...|` strings is treated as
data, and outside strings `;`, line breaks, `$`, `!` (PML variables), method calls and words such as `SAVEWORK`,
`DELETE`, `NEW`, `OBJECT` are rejected, as are unbalanced parentheses and unterminated strings. This is a
reasonable safeguard, not a guarantee; read-only mode should not be the only thing between an untrusted client
and the model. `limit` defaults to 100 and is capped at 1000. The result is a table:

```json
{ "Total": 4, "Offset": 0, "Limit": 100, "Columns": ["dbref", "NAME", "BORE"],
  "Rows": [ { "dbref": "=201346360/562", "NAME": "/14\"-S.C.I.-...", "BORE": "350mm" } ] }
```

### Read attributes of several elements

```http
POST /api/Element/Get
```

```json
{ "refs": ["=201346360/562", "=201346360/546"], "attributes": ["NAME", "TYPE"] }
```

Same table shape, rows in the order of `refs` (max 1000). A dbref that does not exist gives 404.

### Children of an element

```http
GET /api/Element/Children?dbref==201346360/1&type=ZONE
```

`type` is optional. `Data`: `{ "Owner": "=...", "Children": [ { "dbref", "type", "name" } ] }`.

### Attribute names of a type

```http
GET /api/Element/AttributeList?type=PIPE
```

`Data` is a list: system attributes first, then UDAs (`:NAME`).

### Session info

```http
GET /api/Session/Info
```

`Data`: `Project`, `Mdb`, `User`, `Module`, `E3dVersion`, `ReadOnly`. Fields E3D cannot report are `null`.

---

# Standard API Response

## Success

```json
{
  "success": true,
  "data": {},
  "error": null
}
```

---

## Failure

```json
{
  "success": false,
  "data": null,
  "error": "Detailed error message"
}
```

---

# Error Handling Strategy

All exceptions should be handled centrally at the HTTP layer.

Controllers and Services should avoid local exception handling unless absolutely necessary.

---

## Exception Hierarchy

```text
Exception
│
└── NexusException
     │
     ├── VariableNotFoundException
     │
     └── ElementNotFoundException
```

---

## HTTP Status Codes

### Success

```text
200 OK
```

### Bad Request

```text
400 Bad Request
```

### Unauthorized

```text
401 Unauthorized        missing or wrong X-Nexus-Token (when NEXUS_TOKEN is set)
```

### Forbidden

```text
403 Forbidden           PML execution while Nexus is in read-only mode
```

### Resource Not Found

```text
404 Not Found
```

### Method Not Allowed

```text
405 Method Not Allowed  e.g. GET on a POST-only endpoint
```

### Internal Error

```text
500 Internal Server Error
```

---

# Configuration

Follow the steps below to configure and start the AVEVA HTTP server.

## Prerequisites

- Ensure the AVEVA application is installed and running.
- Verify that you have access to the required PML macro files.

## Steps

### 1. Launch AVEVA

Open the AVEVA application and log in to the required project environment.

### 2. Start the HTTP Server

Load and execute the following PML macro:

```text
.\AVEVA_Nexus\AVEVA_Nexus\AVEVA_Nexus\E3DInstallationDir\pmllib\RunServer.pmlmac
```

This macro starts a lightweight HTTP server used for receiving and processing requests.

### 3. Configure Server Ports (Optional)

If required, you can modify the server port settings directly within the RunServer.pmlmac macro.

Important considerations:

- Multiple HTTP servers can be hosted on different ports.
- Only one HTTP server instance should run per AVEVA session.
- Running multiple HTTP servers within the same AVEVA session may lead to database corruption, unexpected behavior, or data integrity issues unless appropriate safeguards are implemented within this solution as a part of enhancement(Thread safety).
- If multiple server instances are required, it is recommended to use separate AVEVA sessions.

### 4. Send Requests

Once the server is running, send HTTP requests to the configured endpoint and port.

Notes:
- Ensure the configured port is available and not being used by another application.
- Verify firewall and network settings if remote access is required.
- Review server logs for troubleshooting and diagnostics.

---

## Environment variables

Read from the environment of the E3D process.

| Variable | Effect |
|---|---|
| `NEXUS_READONLY` | Read-only is **on unless this is exactly `0`**. In read-only mode `PML/Execute` and `PML/Run` answer 403; queries and variable reads keep working. |
| `NEXUS_TOKEN` | If set, every request except `GET /api/Health` must send it in the `X-Nexus-Token` header, and browser clients are allowed (CORS `*`). Required when the listener is not on `localhost`. If **not** set, any request that carries an `Origin` header (a web page in a browser) is refused with 403 and no CORS headers are sent, so other web pages cannot read the model through your browser. The token travels in clear over plain HTTP. |
| `NEXUS_PORT` | If set, the addin starts the HTTP server by itself on `http://+:<port>/` when it loads (no macro needed). Not covered by the tests yet; the supported way to start a session is still the launcher/macro. |

All calls into E3D are serialised through one lock: simultaneous requests used to crash the E3D session.

## Breaking changes (compared with the original project)

- `POST /api/PML/Execute` and `POST /api/PML/Run` return 403 unless `NEXUS_READONLY=0`.
- A failing `Execute` returns 400 (it used to answer `success: true`).
- `GET /api/PML/Variable/Array/{name}` keeps the PML order and returns every element as text.
- Variable names in the URL must match `[A-Za-z0-9_.]{1,64}`.
- Without `NEXUS_TOKEN`, requests with an `Origin` header are refused.

## Build and test

Windows, PowerShell, E3D 3.1 installed (its DLLs are used as references) and Visual Studio 2022 Build Tools
(Roslyn `csc.exe`; the .NET Framework compiler is C# 5 and cannot build this project).

```powershell
scripts\build.ps1                    # -> out\AVEVA_Nexux_Addin.dll
scripts\run-tests.ps1                # unit tests, no E3D needed
scripts\deploy-local.ps1 -Target <runtime folder> [-Writable]   # rebuild and restart a local E3D TTY session
python -m pytest tests/integration -m "not readonly"   # against a session started with -Writable
python -m pytest tests/integration -m "not writable"   # against a session started in the default read-only mode
```

The integration tests expect a running Nexus on `http://localhost:5001` (override with `NEXUS_URL` / `NEXUS_TOKEN`)
and the CAR project; they skip themselves when it is not reachable. `<runtime folder>` is the folder that holds the
launcher scripts for your E3D project (`Start-Nexus.ps1`, `Stop-Nexus.ps1`, `config\`, `bin\`).

Findings about the E3D APIs that shaped this design are in `docs/superpowers/notes/2026-11-05-pml-api-findings.md`.

---

# 🤝 Contributing

Contributions are welcome!
If you have ideas, improvements, or additional Add-In examples to share:

1. **Fork** the repository
2. **Create** a new feature branch
3. **Submit** a pull request

Together, we can build a stronger and more collaborative **AVEVA customization community**.

---

## 📬 Contact

For questions, feedback, or collaboration opportunities,
please **open an issue** or start a **GitHub discussion**.

---