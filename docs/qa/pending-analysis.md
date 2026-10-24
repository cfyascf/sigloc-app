# QA findings not implemented yet

These items come from the exploratory QA pass of 2026-10-23. Each one is still open and
**needs analysis before anything is built**, because the right fix depends on a product or
architecture decision.

## M13 — Stops can be confirmed with no plausibility check

**What happens.** A carrier confirmed the pickup and the delivery of a 116 km trip one
minute apart, and no GPS position was recorded. The trip went to "Entregue".

**Where.** `sigloc-api/Sigloc.Application/Services/TripStopCompletion.cs` and
`CarrierTripService.CompleteStopAsync`. Any manual confirmation of the next pending stop is
accepted. The delivery requires a proof file, but nothing checks time or position.

**Why it matters.** The contractor's SLA, monitoring and delivery history rely on these
confirmations. A trip can be marked as delivered without the vehicle having moved.

**Questions to answer.**

- Should there be a minimum time between stops, derived from the route's estimated
  driving time? If so, how much tolerance should it allow?
- When the vehicle has a tracker, should a confirmation be rejected, or only flagged, if
  the last GPS position is far from the stop? What radius counts as "far"? (Monitoring
  already uses `GeofenceRadiusMeters`.)
- When the vehicle has no tracker, should manual confirmations be allowed at all, or only
  flagged for the contractor?
- Should a suspicious confirmation block the carrier, or be accepted with a warning event
  and a notification to the contractor?

## M17 — Logout does not revoke the token

**What happens.** "Sair" only clears the token in the browser. The JWT stays valid until
it expires, about 1 hour later. A request to `GET /api/vehicles` with the old token after
logout returned 200.

**Where.** This comes from the authentication design: stateless JWTs from
`Sigloc.Infrastructure/Authentication/JwtProvider.cs`, with no server-side session.

**Why it matters.** A token copied from a shared or compromised machine keeps working after
the user logs out. Password resets and removing a user also do not cut off tokens that were
already issued.

**Options to evaluate.**

1. **Accept and document it.** Keep 1-hour tokens and state the behaviour in the
   security notes. This costs nothing.
2. **Revocation list.** Store the `jti` of logged-out tokens until they expire and reject
   them in the JWT validation. Each request then needs a lookup in the database or a cache.
3. **Short-lived access tokens with a refresh token.** For example, 5–15 minute access
   tokens and a revocable refresh token kept server-side. This needs API and UI changes
   for the refresh flow.
4. **Token version per user.** Store a counter on the user, put it in the token, and
   increment it on logout or password reset. One read per request, and it revokes every
   session of that user at once.

## Also open

- **M22.** `GET /api/viagens/ativas` is requested twice on Rotas Ativas, and the first
  request is aborted. Check whether this also happens in a production build, or only
  because of React StrictMode in development, before changing `ActiveRoutes.jsx`.
- **Bug 14.** The "Piso ANTT" is a placeholder equal to the auction ceiling, and bids below it
  are accepted. It is left as it is for now, by decision. A real fix needs the ANTT
  minimum-freight table (distance, axles, cargo type).
- **Bug 1.** Google sign-in needs the deployed origin added to the OAuth client in the Google
  Cloud Console. This is a configuration change, not a code change.
