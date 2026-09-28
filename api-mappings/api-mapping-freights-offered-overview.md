# API Mapping — Freights Offered Overview (Auction Board)

### GET `/api/v1/auctions`
- **Consumer:** FreightsOfferedOverview
- **Goal:** Return a paged list of auctioned freights (segment plans) filtered and searchable for the dashboard cards.
- **Business Logic Specification:**
  1. Authenticate caller and resolve tenant scope.
  2. Support search by auction ID or name, filtering by status (exclude 'Em montagem' by default), and paging.
  3. Provide aggregated fields used by cards: bestBid, totalBids, stops, bidDeadline, risk.

**Input Contract (Request):**
```json
{ "pathVariables": {}, "queryParameters": { "search": "string (optional)", "status": "string (optional)", "page": "integer (optional)", "pageSize": "integer (optional)" }, "body": {} }
```

**Output Contract (Response):**
```json
{ "statusCode": 200, "body": { "items": [ { "id": "string", "name": "string", "stops": ["string"], "bestBid": "number|null", "totalBids": "integer", "bidDeadline": "string", "risk": "string" } ], "page": "integer", "pageSize": "integer", "total": "integer" } }
```

---

### GET `/api/v1/auctions/{auctionId}`
- **Consumer:** FreightsOfferedOverview / AuctionBids
- **Goal:** Return full auction context and minimal route plan details when opening details or analysis.

**Input Contract (Request):**
```json
{ "pathVariables": { "auctionId": "string (required)" }, "queryParameters": {}, "body": {} }
```

**Output Contract (Response):**
```json
{ "statusCode": 200, "body": { "id": "string", "name": "string", "stops": ["string"], "bestBid": "number|null", "totalBids": "integer", "bidDeadline": "string", "risk": "string", "segments": [ { "id": "string", "load": "string", "value": "number" } ] } }
```

---

### PUT `/api/auctions/{id}`
- **Consumer:** FreightsOfferedOverview (card inline edit)
- **Goal:** Update editable auction metadata (custom name and/or bid deadline) used by the inline editing flow. Both fields are optional; only the ones provided are changed. A custom `name` overrides the itinerary-derived label; sending an empty/whitespace name clears it and the listing falls back to the itinerary summary.

**Input Contract (Request):**
```json
{ "pathVariables": { "id": "string (uuid, required)" }, "body": { "name": "string (optional)", "expiresAt": "string (optional, ISO-8601)" } }
```

**Output Contract (Response):**
```json
{ "statusCode": 200, "body": { "id": "string (uuid)", "name": "string|null", "expiresAt": "string (ISO-8601)" } }
```

**Error Payload (404):**
```json
{ "statusCode": 404, "body": { "error": "AUCTION_NOT_FOUND", "message": "Auction ... does not exist or does not belong to this contractor." } }
```

**Error Payload (400):** invalid `expiresAt` (missing/in the past)
```json
{ "statusCode": 400, "body": { "error": "VALIDATION_ERROR", "message": "...", "details": [ { "field": "expiresAt", "reason": "Must be in the future." } ] } }
```

---

### DELETE `/api/auctions/{id}`
- **Consumer:** FreightsOfferedOverview
- **Goal:** Delete an auction after confirmation.

**Input Contract (Request):**
```json
{ "pathVariables": { "id": "string (uuid, required)" }, "body": {} }
```

**Output Contract (Response):** `204 No Content`

**Error Payload (404):**
```json
{ "statusCode": 404, "body": { "error": "AUCTION_NOT_FOUND", "message": "Auction ... does not exist or does not belong to this contractor." } }
```
