# AVEVA_Nexus

Universal HTTP Gateway for AVEVA Connecting Applications with other systems, applications, and AI.

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

Failure Response

```json
{
  "success": false,
  "error": "Command execution failed"
}
```

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

### Resource Not Found

```text
404 Not Found
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