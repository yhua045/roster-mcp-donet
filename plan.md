# Plan: Convert existing REST API to MCP-compatible service

Goal
----
Turn the existing REST API into an MCP (Model-Context-Protocol) compatible server suitable for integration with Copilot/agents frameworks. Deliver a minimal, well-documented migration plan that preserves existing behavior while adding the interfaces and runtime hooks required by MCP.

Phases
------

1. Analyze current API surface
   - Inventory controllers, endpoints, models, and data access patterns.
   - Identify long-running operations, background tasks, and external integrations.
   - Note auth/identity flows and configuration sources.

2. Design MCP interface surface
   - Define the MCP endpoints (health, metadata, open protocol endpoints) and management routes.
   - Define a lightweight adapter layer that maps existing REST endpoints to MCP message handlers.
   - Specify required metadata: schema, capabilities, verbs, and supported content-types.

3. Implement adapter and handlers
   - Add an MCP adapter project or folder under `Roster.Mcp.McpAdapter`.
   - Implement a request dispatcher that translates MCP messages to internal service calls.
   - Reuse existing controllers/services where possible; create thin facade classes to avoid duplication.

4. Add lifecycle and observability hooks
   - Expose readiness and liveness probes compatible with MCP expectations.
   - Add structured logging (Serilog) and correlation IDs.
   - Instrument traces and metrics (OpenTelemetry) for request/response/latency.

5. Security and configuration
   - Validate auth flows: JWT, Azure AD, or API keys mapped to MCP callers.
   - Ensure secrets and credentials use `UserSecrets` / Key Vault as appropriate.
   - Add CORS and CSP policies if exposing via browser-based agents.

6. Testing and validation
   - Unit tests for adapter translation logic and handlers.
   - Integration tests exercising MCP request -> internal handler -> response.
   - Contract tests validating schema and capability declarations.

7. Packaging, deployment, and runtime
   - Add Dockerfile and containerize the service.
   - Define Kubernetes manifests or Azure Container Apps configuration with MCP labels/annotations.
   - CI pipeline: build image, run tests, push to registry, deploy to staging.

8. Documentation and examples
   - Add `README.md` describing MCP endpoints and usage examples.
   - Provide example MCP messages and a small client script to exercise the service.

Deliverables
------------
- `Roster.Mcp.McpAdapter` adapter project (C#) with dispatcher and handler registration.
- `plan.md` (this file) and `README.md` with usage and example messages.
- Tests: adapter unit tests and integration tests.
- Dockerfile and deployment manifests.

Risks & Mitigations
-------------------
- Breaking changes to public REST endpoints — mitigate by keeping existing controllers and adding adapters as a compatibility layer.
- Authentication mismatches — test with representative tokens and provide a mapping layer for MCP callers.
- Performance overhead of translation layer — keep adapters thin and measure latency; consider in-process invocation where possible.

Next steps
----------
1. Run inventory: list controllers and endpoints (automated or manual).
2. Scaffold `Roster.Mcp.McpAdapter` project and add basic dispatcher.
3. Implement one end-to-end example (e.g., `GET /api/rosters`) and add tests.
