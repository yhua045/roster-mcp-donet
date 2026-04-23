# MCP Adapter Architecture

## Purpose

Build a .NET MCP server that acts as a thin adapter in front of the existing Roster API documented in `design/api.md`.

The goal for v1 is simplicity. The MCP layer should mirror the downstream API as closely as practical so implementation stays small, behavior stays predictable, and the adapter does not introduce new domain behavior unless MCP requires it.

This document is design-only. No implementation should proceed until this design is approved.

## Decision summary

The chosen direction is a mirror-first MCP adapter.

That means:

- MCP tools mirror downstream Roster API operations
- MCP tool input shapes stay close to downstream request shapes
- MCP tool output shapes largely preserve the downstream response envelope: `result`, `error`, `data`
- the adapter adds only thin validation, HTTP transport, logging, and resilience behavior

This is intentionally simpler than building a normalized domain abstraction in front of the downstream API.

## Why this design

This approach is easier because it reduces:

- custom translation logic
- adapter-specific DTOs
- normalization rules that need tests
- risk of drifting away from actual downstream behavior

It also makes troubleshooting easier because each MCP call should map to one downstream API call.

## Non-goals

- redesigning the downstream Roster API
- normalizing every downstream inconsistency in v1
- adding preview/confirm workflows for writes in v1
- creating a generic pass-through tool like `call_api`
- implementing anything before design approval

## Downstream API assumptions

Based on `design/api.md`:

- base URL: `https://roster.efcsydney.org`
- verified endpoint: `GET /api/events`
- observed response envelope: `result`, `error`, `data`
- no authentication observed
- `POST /api/events` and `PUT /api/events/{id}` are documented but still need live verification
- `GET /api/events/{id}` is documented but not verified live

The downstream Roster API remains the system of record.

## Proposed MCP surface

### Tool set

Recommended tools for v1:

- `list_events`
- `create_event`
- `update_event`

Deferred until downstream verification:

- `get_event`

These names are MCP-friendly, but each tool maps directly to one downstream REST operation.

### Tool mapping

- `list_events` -> `GET /api/events`
- `create_event` -> `POST /api/events`
- `update_event` -> `PUT /api/events/{id}`
- `get_event` -> `GET /api/events/{id}` only after live verification

## Architecture overview

The adapter should stay thin and use only the minimum layers needed for clean testing.

### 1. MCP host layer

Responsibilities:

- start the MCP server
- register tools
- bind schemas
- manage request lifetime and cancellation

This layer knows MCP, but not downstream HTTP details.

### 2. Tool handler layer

Responsibilities:

- receive typed MCP arguments
- run lightweight validation
- call the downstream Roster API client
- return the downstream-shaped result

There should be one handler per tool.

### 3. Downstream Roster API client

Responsibilities:

- build URLs and query strings
- serialize request bodies
- send HTTP requests
- parse downstream JSON
- return typed downstream responses

This is the main abstraction boundary in the mirror-first design.

### 4. Shared infrastructure

Responsibilities:

- configuration
- logging
- request IDs / correlation IDs
- timeout and retry policy

## Project structure

Minimal logical structure:

- `Roster.MCP.Api`
   - MCP host
   - DI wiring
   - tool registration
- `Roster.MCP.RosterApi`
   - typed downstream client
   - downstream DTOs
   - HTTP transport code
- `Roster.MCP.Tests`
   - handler tests
   - downstream client tests
   - integration tests

This can start in fewer physical projects if needed, but these boundaries should still exist in the code.

## Request and response design

### Core rule

For v1, the MCP contract should stay close to the downstream contract.

That means:

- do not introduce a custom top-level envelope like `meta/data/errors`
- do not reshape downstream success payloads unless MCP requires it
- do not move fields around unless there is a strong technical reason

The default response shape should remain:

```json
{
   "result": "OK",
   "error": { "message": "" },
   "data": []
}
```

### `list_events`

Arguments:

- `category`: optional string
- `from`: optional ISO date string
- `to`: optional ISO date string

Validation:

- accept documented category values case-insensitively
- require `YYYY-MM-DD` if dates are provided
- require `from <= to` when both are present

Translation:

- pass the same query parameters downstream
- lowercase `category` before sending

Return value:

- preserve the downstream envelope and event payload shape

### `create_event`

Arguments should mirror the downstream body as closely as possible:

- `date`
- `category`
- `serviceInfo`
   - `footnote`
   - `skipService`
   - `skipReason`
- `members[]`
   - `role`
   - `name`
   - `personId`

Return value:

- preserve the downstream write response shape as observed live

### `update_event`

Arguments:

- `id`
- same body shape as `create_event`

Return value:

- preserve the downstream write response shape as observed live

## Error handling

### Principle

Preserve downstream business errors when possible. Only generate adapter-owned errors for transport or parsing failures.

There are two classes of errors.

### 1. Downstream business or validation errors

If the downstream API returns a valid application-level error payload, return that payload to the MCP caller with minimal changes.

Examples:

- invalid category
- invalid date format
- invalid date range

### 2. Adapter or transport errors

If the adapter cannot successfully talk to the downstream service, return a small adapter-generated error payload.

Recommended codes:

- `downstream_unavailable`
- `downstream_timeout`
- `downstream_invalid_response`

Recommended shape:

```json
{
   "result": "Error",
   "error": {
      "code": "downstream_timeout",
      "message": "The downstream Roster API did not respond in time."
   },
   "data": null
}
```

## Write behavior

### v1 choice

For simplicity, v1 should not add a preview/confirm protocol for writes.

Instead:

- `create_event` performs one downstream POST
- `update_event` performs one downstream PUT

This is the easiest design and keeps the MCP adapter close to the downstream API.

### Risk note

If stronger write safety is needed later, we can add:

- environment-based write protection
- optional confirmation flags
- additional audit logging

Those are later extensions, not v1 requirements.

## Validation strategy

The adapter should still validate obvious input errors before making downstream calls.

Recommended validations:

- category allowed values
- ISO date format
- date range ordering
- required fields for create and update requests
- required `id` for update

This gives faster feedback and keeps tests deterministic.

## Resilience and observability

### Timeouts

Use explicit HTTP timeouts for all downstream requests.

Guidance:

- reads: short timeout
- writes: moderate timeout

### Retries

Retry only transient read failures.

Do not automatically retry writes in v1 unless idempotency is explicitly designed.

### Logging

Each tool invocation should log:

- MCP tool name
- downstream method and path
- request ID
- downstream HTTP status
- duration
- success/failure classification

### Configuration

Centralize:

- downstream base URL
- timeout values
- retry policy

## TDD seams

The mirror-first design reduces the number of seams, but the important ones remain.

### 1. Handler validation tests

- invalid category rejected
- invalid date rejected
- invalid date range rejected
- missing required write fields rejected

### 2. Downstream client tests

- query string generation for `list_events`
- request body generation for `create_event`
- request body generation for `update_event`
- parsing of success and error envelopes

### 3. Handler integration tests

- `list_events` returns downstream success payload
- downstream validation errors are preserved
- downstream timeout returns adapter timeout error
- write handlers correctly forward POST and PUT calls

## Open questions

These still need confirmation before or during implementation:

1. Does `POST /api/events` work live exactly as documented?
2. Does `PUT /api/events/{id}` work live exactly as documented?
3. What is the exact live response shape for downstream writes?
4. Is `GET /api/events/{id}` actually supported but restricted, or simply not implemented?

These do not block the mirror-first architecture, but they do affect rollout order.

## Recommended rollout

Implement in this order:

1. `list_events`
2. shared downstream client and error handling
3. `create_event` after live verification of POST behavior
4. `update_event` after live verification of PUT behavior
5. `get_event` only if the downstream endpoint is verified

## Review note

The first version should favor fidelity over abstraction. The MCP server exists to make the Roster API MCP-compatible, not to redesign it. A thin mirror keeps the implementation small, makes failures easier to reason about, and leaves room to add normalization or safety features later only if they are proven necessary.

## Approval gate

No implementation should begin until this document is approved.
