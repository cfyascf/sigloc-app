import assert from "node:assert/strict"
import { test } from "node:test"
import { brasiliaToday, formatIsoDate, validityStatus } from "./document-validity.js"

test("validity is measured in Brasília days", () => {
  // 02:00 UTC on the 10th is still the 9th in Brasília (UTC-3).
  assert.equal(brasiliaToday(new Date("2026-11-10T02:00:00Z")), "2026-11-09")
})

test("expired, expiring within 30 days, valid, or nothing without a date", () => {
  assert.deepEqual(validityStatus("2026-11-08", "2026-11-09"), { kind: "expired", days: -1 })
  assert.deepEqual(validityStatus("2026-11-09", "2026-11-09"), { kind: "expiring", days: 0 })
  assert.deepEqual(validityStatus("2026-12-09", "2026-11-09"), { kind: "expiring", days: 30 })
  assert.deepEqual(validityStatus("2026-12-10", "2026-11-09"), { kind: "valid", days: 31 })
  assert.equal(validityStatus("", "2026-11-09"), null)
  assert.equal(formatIsoDate("2026-11-09"), "09/11/2026")
})
