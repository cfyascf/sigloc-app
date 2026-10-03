# API Mapping — Auction Bids

### GET `/api/v1/segments/{segmentId}/auction-bids`
- **Consumer:** AuctionBids
- **Goal:** Return the ranked list of bids for a specific transport segment, including the segment context needed to display the auction comparison panel.
- **Business Logic Specification:**
  1. Authenticate the caller and verify access to freight auction operations.
  2. Resolve the segment by identifier and ensure it belongs to the authenticated tenant/organization.
  3. Retrieve all bids associated with the segment, including carrier, vehicle, proposed value, and bid metadata.
  4. Sort bids by proposed value ascending to produce the ranking shown in the UI.
  5. Compute the savings relative to the segment target fare and the ANTT floor threshold for display.
  6. Retrieve supporting risk data for each bid, including fleet compliance, insurance validation status, and OTD performance metrics.
  7. Return only the fields required by the UI to avoid client-side joins and calculations.
  8. If the segment does not exist, return a 404 response.

**Input Contract (Request):**
```json
{
  "pathVariables": {
    "segmentId": "string (required)"
  },
  "queryParameters": {},
  "body": {}
}
```

## **Output Contract (Response):**
```json
{
  "statusCode": 200,
  "body": {
    "segment": {
      "id": "string",
      "name": "string",
      "itemCount": "integer",
      "stops": ["string"],
      "targetFare": "number",
      "anttFloorFare": "number"
    },
    "bids": [
      {
        "id": "string",
        "segmentRef": "string",
        "carrier": "string",
        "vehicle": "string",
        "proposedValue": "number",
        "savings": "number",
        "savingsPercent": "number",
        "risk": {
          "fleetVehicles": "integer",
          "insuranceStatus": "string",
          "insuranceDescription": "string",
          "otd": "number"
        },
        "isWinner": "boolean"
      }
    ],
    "ranking": ["string"]
  }
}
```

**Error Payloads:**
```json
{
  "statusCode": 404,
  "body": {
    "error": "SEGMENT_NOT_FOUND",
    "message": "Segment not found"
  }
}
```

```json
{
  "statusCode": 401,
  "body": {
    "error": "UNAUTHORIZED",
    "message": "Authentication token is missing or invalid"
  }
}
```

```json
{
  "statusCode": 403,
  "body": {
    "error": "FORBIDDEN",
    "message": "User does not have permission to view auction bids"
  }
}
```

### POST `/api/v1/segments/{segmentId}/auction-bids/{bidId}/select`
- **Consumer:** AuctionBids
- **Goal:** Mark a specific bid as the selected winner for the segment auction.
- **Business Logic Specification:**
  1. Authenticate the caller and verify permissions to adjudicate freight bids.
  2. Validate that the segment exists and that the bid belongs to that segment.
  3. Ensure the bid is still eligible for selection according to business rules, including budget and compliance constraints.
  4. Update the selected bid state for the segment, replacing any previous winner.
  5. Return the updated auction state and selected bid summary.

**Input Contract (Request):**
```json
{
  "pathVariables": {
    "segmentId": "string (required)",
    "bidId": "string (required)"
  },
  "queryParameters": {},
  "body": {}
}
```

## **Output Contract (Response):**
```json
{
  "statusCode": 200,
  "body": {
    "segmentId": "string",
    "selectedBidId": "string",
    "status": "string",
    "winner": {
      "id": "string",
      "carrier": "string",
      "vehicle": "string",
      "proposedValue": "number"
    }
  }
}
```

**Error Payloads:**
```json
{
  "statusCode": 400,
  "body": {
    "error": "INVALID_SELECTION",
    "message": "Bid cannot be selected"
  }
}
```

```json
{
  "statusCode": 404,
  "body": {
    "error": "BID_NOT_FOUND",
    "message": "Bid not found"
  }
}
```
