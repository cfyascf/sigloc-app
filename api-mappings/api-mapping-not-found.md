# API Mapping — Not Found (404) Page

### Purpose
- **Consumer:** NotFound
- **Goal:** UI-only route displayed when a requested path is not found or the user lacks permission. This screen requires no dedicated backend endpoint; it consumes application routing and authorization state provided by existing endpoints (auth, route resolution, resource reads).

### Notes for Backend
- No new API surfaces are required to support this page.
- Ensure existing endpoints return clear HTTP statuses and structured error payloads so the UI can render helpful messages:
  - 404 Not Found for missing resources
  - 403 Forbidden for access denied
  - 401 Unauthorized for unauthenticated requests

### Recommended Error Response Shape
```json
{
  "statusCode": 404,
  "body": { "error": "NOT_FOUND", "message": "Resource not found" }
}
```

and

```json
{
  "statusCode": 403,
  "body": { "error": "FORBIDDEN", "message": "Access denied" }
}
```

### UX Guidance for Backend Teams
- Provide `role` and `tenant` claims in auth tokens so the UI can compute `getDefaultRouteForRole` without extra calls.
- When redirecting or returning errors from APIs, include `resourceType` and `resourceId` when helpful for diagnostics.

### Implemented Behavior (US21)
Error bodies are flat (`{ error, message, ... }`), matching the global exception middleware:

| Status | `error` | When |
|---|---|---|
| 401 | `UNAUTHORIZED` | Missing/invalid/expired token (JWT challenge) |
| 403 | `FORBIDDEN` | Authenticated but role not allowed (policy) or `ForbiddenException` |
| 404 | `*_NOT_FOUND` / `NOT_FOUND` | Missing resource, or resource of another tenant (never 403, to avoid leaking existence) |

Not-found payloads include `resourceType` (`auction`, `bid`, `product`, `contractor`, `routeSegment`, `offer`, `vehicle`) and `resourceId`. `ForbiddenException` may include them too.

JWT claims: `role` (from the role claim) and `tenant` (company id, alias of `empresaId`).
