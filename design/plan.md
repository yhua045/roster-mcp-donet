# MCP Adapter Plan

Reference design: `design/architecture.md`

## Phase 1

Approve the adapter architecture and the MCP tool contracts.

## Phase 2

Write failing tests for:

- `list_events` input validation
- `list_events` downstream mapping
- write confirmation behavior for `create_event` and `update_event`

## Phase 3

Implement the minimal read slice first, then add write support after downstream write verification.

## Approval status

Pending review. Do not implement until approved.
