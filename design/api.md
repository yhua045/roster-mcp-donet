# Roster API Documentation

## Overview

The Roster API provides endpoints for managing church service rosters, including retrieving historical roster data, creating new events, and updating member assignments.

## Base URL

```
https://roster.efcsydney.org
```

## Authentication

API require no authentication.

## Implementation notes (observed)

- Base URL `https://roster.efcsydney.org` is correct and reachable.
- Verified endpoint: `GET /api/events` — supports `category`, `from`, and `to` query parameters and returns a list of events.
- Response envelope: the live API returns a JSON wrapper with `result`, `error`, and `data` fields. The `data` field contains the event array. Documentation examples below have been updated to reflect this.
- `GET /api/events/{id}` returned 404 for a sampled id and appears unimplemented or not publicly accessible; treat as unverified.
- `POST /api/events` and `PUT /api/events/{id}` were not discoverable via OpenAPI/Swagger (no `/openapi.json` or `/swagger/index.html`), so their availability is unverified. Treat them as "spec-only" until confirmed.

Recommendation: when wrapping this API with an MCP adapter, map MCP payloads to the live API's `data` envelope and handle the `error` object accordingly.

## Endpoints

### GET /api/events

Retrieve events within a specified date range and optional category filter.

#### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `category` | string | No | - | Service category filter (case-insensitive). See allowed values below. |
| `from` | string (ISO date) | No | today | Start date for filtering (inclusive). Format: YYYY-MM-DD |
| `to` | string (ISO date) | No | today + 7 days | End date for filtering (inclusive). Format: YYYY-MM-DD |

#### Allowed Category Values

The `category` parameter accepts the following case-insensitive values:
- `chinese` - Chinese language service
- `english` - English language service
- `sundayschool` - Sunday school service

#### Request Examples

##### Get all events for the next 7 days (default behavior)
```http
GET /api/events
```

##### Get Chinese service events for January 2024
```http
GET /api/events?category=chinese&from=2024-01-01&to=2024-01-31
```

##### Get all events for a specific date range
```http
GET /api/events?from=2024-01-14&to=2024-01-21
```

##### Get English service events (case-insensitive)
```http
GET /api/events?category=English&from=2024-02-01&to=2024-02-29
```

#### Response

##### Success Response (200 OK)

Returns an envelope object where `data` contains the event array (live API):

```json
{
  "result": "OK",
  "error": { "message": "" },
  "data": [
    {
      "id": 1,
      "date": "2024-01-14",
      "category": "chinese",
      "serviceInfo": {
        "id": 101,
        "footnote": "Special service",
        "skipService": false,
        "skipReason": null
      },
      "members": [
        {
          "id": 501,
          "role": "證道",
          "name": "Pastor Chen",
          "personId": 42,
          "confirmed": true,
          "notes": null
        },
        {
          "id": 502,
          "role": "司會",
          "name": "John Smith",
          "personId": 43,
          "confirmed": true,
          "notes": null
        }
      ]
    }
  ]
}
```

##### Empty Result (200 OK)

When no events match the criteria:

```json
{
  "result": "OK",
  "error": { "message": "" },
  "data": []
}
```

#### Error Responses

##### Invalid Category (400 Bad Request)

When an invalid category value is provided:

Live API error envelope example:

```json
{
  "result": "Error",
  "error": {
    "message": "Invalid category: 'invalidvalue'. Must be one of ['chinese','english','sundayschool']",
    "details": { "category": "Invalid value" }
  },
  "data": null
}
```

##### Invalid Date Format (400 Bad Request)

When date parameters are malformed:

```json
{
  "result": "Error",
  "error": {
    "message": "Invalid date format",
    "details": { "from": "Invalid date format: '2024/01/14'. Expected YYYY-MM-DD" }
  },
  "data": null
}
```

##### Invalid Date Range (400 Bad Request)

When `from` date is after `to` date:

```json
{
  "result": "Error",
  "error": {
    "message": "Invalid date range",
    "details": { "date_range": "Invalid date range: 'from' date (2024-02-01) must be before or equal to 'to' date (2024-01-01)" }
  },
  "data": null
}
```

##### Server Error (500 Internal Server Error)

```json
{
  "error": "Internal server error"
}
```

### GET /api/events/{id}

Retrieve a specific event by ID.

Note: this single-event endpoint was not observed on the live API (sampled id returned 404). Treat as unverified until confirmed with API owners.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | integer | Yes | Event identifier (in URL path) |

#### Request Example

```http
GET /api/events/123
```

#### Response

Returns a single event object (same structure as in GET /api/events response).

### POST /api/events

Create a new event.

#### Request Body

```json
{
  "date": "2024-01-21",
  "category": "chinese",
  "serviceInfo": {
    "footnote": "Special service",
    "skipService": false,
    "skipReason": null
  },
  "members": [
    {
      "role": "證道",
      "name": "Pastor Chen",
      "personId": 42
    }
  ]
}
```

#### Response

Returns the created event object with generated IDs.

### PUT /api/events/{id}

Update an existing event or add members to an event.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | integer | Yes | Event identifier (in URL path) |

#### Request Body

Same structure as POST /api/events.

#### Response

Returns the updated event object.

## Error Handling

All error responses follow a consistent format:

```json
{
  "message": "Human-readable error message",
  "errors": {
    "field_name": "Specific error detail"
  }
}
```

### Common HTTP Status Codes

- `200 OK` - Request successful
- `400 Bad Request` - Validation error or invalid parameters
- `401 Unauthorized` - Missing or invalid authentication
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Rate Limiting

API requests may be subject to rate limiting. Check response headers for rate limit information:

- `X-RateLimit-Limit` - Maximum requests per hour
- `X-RateLimit-Remaining` - Remaining requests in current window
- `X-RateLimit-Reset` - Unix timestamp when the rate limit resets

## Versioning

The API version is included in the URL path. The current version is v1 (implicit in the base URL).

## Security Considerations

- Always use HTTPS for API requests
- Never expose API keys in client-side code
- Implement proper error handling to avoid exposing sensitive information
- Validate and sanitize all input data before sending to the API