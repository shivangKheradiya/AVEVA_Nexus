Save the following as README.md in the root of your solution.

# AVEVA Nexus

Universal HTTP Gateway for AVEVA Applications

---

## Overview

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

# Current Development Status

## Phase 1 ✅ Completed

Implemented:

- AVEVA Addin
- HTTP Server
- Background Thread Hosting
- Basic Request Routing
- JSON Responses

---

## Phase 2 ✅ Completed

Implemented:

- Controller Layer
- Service Layer
- Provider Layer
- Skeleton API Design
- Hardcoded Sample Data

---

## Phase 2.5 ✅ Completed

Implemented:

- Standard Response Contract
- Exception Architecture
- Logging Design
- Centralized Error Handling Design

---

## Phase 3 🚧 Planned

Implement:

- Dynamic Route Parsing
- Query Parameter Parsing
- POST Body Parsing
- Request Validation

---

## Phase 4 🚧 Planned

Implement:

- PML Integration

---

## Phase 5 🚧 Planned

Implement:

- Element Integration

---

## Phase 6 🚧 Planned

Implement:

- Authentication
- Authorization
- OpenAPI / Swagger
- API Documentation

---

# Solution Structure

```text
AVEVA_Nexus
│
├── Addin
│   └── AVEVA_Nexus_Addin.cs
│
├── Http
│   ├── SimpleHttpServer.cs
│   └── Router.cs
│
├── Controllers
│   ├── PmlController.cs
│   └── ElementController.cs
│
├── Services
│   ├── PmlService.cs
│   └── ElementService.cs
│
├── Integration
│   ├── IAvevaProvider.cs
│   └── AvevaProvider.cs
│
├── Models
│   ├── ApiResponse.cs
│   └── ExecuteRequest.cs
│
├── Exceptions
│   ├── NexusException.cs
│   ├── VariableNotFoundException.cs
│   └── ElementNotFoundException.cs
│
├── Logging
│   └── NexusLogger.cs
│
└── Configuration
    └── NexusConfiguration.cs
```

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

Configuration is stored in:

```text
App.config
```

Example:

```xml
<configuration>

  <appSettings>

    <add key="AutoStart" value="true" />
    <add key="Port" value="5000" />

  </appSettings>

</configuration>
```

---

## AutoStart

Controls automatic startup of HTTP listener.

```xml
<add key="AutoStart" value="true" />
```

Valid Values

```text
true
false
```

---

## Port

HTTP listener port.

```xml
<add key="Port" value="5000" />
```

---

# Logging

Current logging target:

```text
Console
```

Future logging targets:

- File Logging
- Windows Event Log
- AVEVA Log
- Centralized Logging Systems

---

# Development Rules

## Rule 1

Only `AvevaProvider.cs` may contain AVEVA integration logic.

---

## Rule 2

Controllers must remain thin.

---

## Rule 3

All responses must use `ApiResponse`.

---

## Rule 4

Do not call AVEVA APIs directly from:

```text
Router
Controllers
Services
```

---

## Rule 5

Prefer exceptions over null returns.

Avoid:

```csharp
return null;
```

Use:

```csharp
throw new VariableNotFoundException(variableName);
```

---

# Future Roadmap

```text
AI Agents
    │
    ▼
AVEVA Nexus
    │
    ├── E3D
    ├── Engineering
    ├── Diagram
    ├── Unified Engineering
    └── Future AVEVA Products
```

The objective is to establish AVEVA Nexus as the standard integration gateway for all future AVEVA and AI-driven workflows.

---

## Current Milestone

✅ Architecture Approved

✅ HTTP Listener Working

✅ Layered Design Implemented

🚧 Next Phase: Dynamic Route Parsing & Request Processing