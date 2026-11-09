/** Test data helpers: unique names per run and dates in the app's time zone (Brasília). */

const RUN = Date.now().toString(36).toUpperCase()

/** Unique suffix for this run, so data created by different runs never collides. */
function tag(prefix = "E2E") {
  return `${prefix}-${RUN}-${Math.random().toString(36).slice(2, 6).toUpperCase()}`
}

/** A valid Mercosul plate that is very unlikely to exist: Q + random letters/digits. */
function randomPlate() {
  const letter = () => String.fromCharCode(65 + Math.floor(Math.random() * 26))
  const digit = () => String(Math.floor(Math.random() * 10))
  return `Q${letter()}${letter()}${digit()}${letter()}${digit()}${digit()}`
}

/** Value for an `<input type="datetime-local">`, `hours` from now, in America/Sao_Paulo. */
function localDateTime(hours) {
  const date = new Date(Date.now() + hours * 3_600_000)
  const parts = Object.fromEntries(
    new Intl.DateTimeFormat("en-CA", {
      timeZone: "America/Sao_Paulo",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      hourCycle: "h23",
    })
      .formatToParts(date)
      .map((part) => [part.type, part.value])
  )
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`
}

/** "YYYY-MM-DD" `days` from today, for `<input type="date">` document validities. */
const isoDate = (days) => new Date(Date.now() + days * 864e5).toISOString().slice(0, 10)

/** A valid CPF (check digits computed), e.g. for a third-party RNTRC holder. */
function randomCpf() {
  const digits = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10))
  const check = (list) => {
    const sum = list.reduce((total, digit, index) => total + digit * (list.length + 1 - index), 0) % 11
    return sum < 2 ? 0 : 11 - sum
  }
  digits.push(check(digits))
  digits.push(check(digits))
  return digits.join("")
}

/** Short code shown on segment cards (last 8 characters of the id). */
const segmentShortId = (id) => String(id).slice(-8)

/** Short code shown on auction cards ("#" + first 6 hex characters, upper case). */
const auctionShortId = (id) => String(id).replace(/-/g, "").slice(0, 6).toUpperCase()

module.exports = { RUN, tag, randomPlate, localDateTime, isoDate, randomCpf, segmentShortId, auctionShortId }
