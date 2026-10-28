# QA findings: decisions and what is still open

These items come from the exploratory QA passes of 2026-10-23 and 2026-10-27. The ones that
needed a product or architecture decision were decided on 2026-10-28 and are now implemented;
this file records the decision so the behaviour is not mistaken for a bug later.

## Decided and implemented

### M13 — Implausible stop confirmations are accepted and flagged

A manual confirmation is never blocked. `TripStopPlausibility` checks it and, when something is
off, logs a `PARADA_SUSPEITA` event on the trip and sends the contractor a "Confirmação suspeita"
notification. A confirmation is suspicious when:

- it comes in less than half the expected driving time after the previous stop (straight-line
  distance between the stops at 60 km/h), or
- the vehicle sent a GPS ping in the last 30 minutes from more than 5 km away from the stop.

The tolerance is kilometres, not the 200 m `GeofenceRadiusMeters`, because stops are geocoded
at city level. Vehicles without a tracker are only checked on time.

### M17 — Logout and password reset revoke tokens (token version per user)

`User.TokenVersion` is copied into every JWT (`versaoToken` claim) and checked on each request
(`TokenVersionValidator`). `POST /api/auth/logout` and a password reset increment it, which
ends every session of that user. Tokens issued before this change have no claim and count as
version 0, so they keep working until they expire. The UI clears the session on any 401 from
an authenticated request.

### Automatic award

Auctions created with "Seleção automática do vencedor" are closed by `AuctionAutoAwardWorker`
(every 60 s, `AutoAward:IntervalSeconds`, off with `AutoAward:Enabled=false`). The lowest total
(freight + toll) wins, the earliest bid breaks a tie, and a bid whose vehicle is no longer free
is skipped. With no winner, or once the first pickup deadline has passed, the auction closes
and its segments go back to "Disponível". The App Service has `alwaysOn: true`, which the
worker needs.

### Other decisions

- **Fleet status.** The manual "Em Trânsito" status is retired: "Em viagem" comes from the
  vehicle's active trip. A migration turns stored `EM_TRANSITO` into `LIVRE`; the API rejects it.
- **`/control-tower`.** The mock page was removed; the URL redirects to the role's trip list.
- **M22.** The double request on the trip lists also happened in production builds. Fixed in
  `trip-list-view.jsx` (the search debounce no longer replaces an unchanged query).

## Still open

- **Bug 14.** The "Piso ANTT" is a placeholder equal to the auction ceiling. It is left as it
  is, by decision. A real floor needs the ANTT minimum-freight table (distance, axles, cargo
  type). Carrier and contractor screens now at least show the same value.
- **Bug 1.** Google sign-in needs the deployed origin added to the OAuth client in the Google
  Cloud Console. This is a configuration change, not a code change.
- **Production data.** `cleanup-2026-10-28.sql` closes the auctions left open by the old
  award bug and removes the QA test data. Run it before deploying the auto-award worker.
