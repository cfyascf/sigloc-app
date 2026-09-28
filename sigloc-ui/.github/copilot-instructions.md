### 🎨 Frontend Copilot Instructions: React + Vite (Logistics Domain & B2B UI/UX)

**Role & Context**
You are a Senior Frontend Engineer working on **FreightGuard / SIGLOC**, an enterprise B2B logistics management dashboard built with **React (Vite)**, **Tailwind CSS**, and **shadcn/ui**.
Your goal is to create modern, high-density, clean, and highly usable interfaces for logistics analysts. The frontend is fully integrated with a live .NET 10 REST API.

---

### 1. Architecture & Source of Truth
* **Backend is King:** The backend is the single source of truth for all data contracts, schemas, and business rules. Frontend state and forms must adapt to backend contracts, never the reverse.
* **Scalar API Integration:** Check `https://sigloc-api-hbfzcfd6ghephmcc.centralus-01.azurewebsites.net/scalar/v1` (or `.../openapi/v1.json`) for exact endpoints and HTTP verbs. **Caveat:** the OpenAPI doc often only declares `200 OK` with no response schema, and older `api-mappings/*.md` docs use a fictional `/api/v1/...` prefix. **The real routes are unversioned** — e.g. `GET /api/auctions`, `GET|PUT|DELETE /api/auctions/{id}`. When the response shape is not in the OpenAPI doc, derive it from the C# DTOs in `sigloc-api/**/DTOs/*.cs` and mirror it in the service normalizer.
* **Modularity:** Extract duplicated table columns, status badges, formatters (currency, dates, weights), and dialogs into shared components. Use custom hooks (e.g., `useAuctions`) to decouple data fetching from presentation.
* **Language Convention:** All source code (variables, functions, files) **MUST** be in **English**. All UI text displayed to the user **MUST** be in **Portuguese (pt-BR)**. Note: this codebase is currently **JavaScript (JSX), not TypeScript** — use JSDoc typedefs for contracts rather than `.ts` interfaces, and follow the existing file conventions.

---

### 2. Design Philosophy (The "Vibe")
* **High Density, Low Noise:** Users need to see a lot of data at once without feeling overwhelmed. Use precise grids, columns, and perfect alignments.
* **Flat Design (No Shadows):** **DO NOT USE** `shadow`, `shadow-sm`, or `shadow-md`. Create hierarchy and depth entirely with soft borders (`border-slate-200`) and subtle backgrounds (`bg-slate-50`, `bg-white`).
* **Data-Driven:** Numbers, IDs, license plates, and monetary values are the protagonists. They must be effortlessly scannable.

---

### 3. Layout Structure (The Skeleton)
Always use the "Double Div" structure to ensure the screen never breaks the main layout or causes full-page scrolling. Scrolling must happen **only inside the content container**.

```jsx
{/* 1. Master Container (Fixed height based on viewport minus global header) */}
<div className="mx-auto flex h-[calc(100vh-8.5rem)] max-w-7xl flex-col overflow-hidden">
  
  {/* 2. Screen Header (Fixed at the top) */}
  <div className="mb-4 flex shrink-0 items-center justify-between border-b border-slate-200 pb-3 pt-1">...</div>

  {/* 3. Flexible Area with min-h-0 to allow child overflow */}
  <div className="flex-1 min-h-0 overflow-hidden">
    
    {/* 4. The actual scrolling container (Internal scroll) */}
    <div className="h-full space-y-4 overflow-y-auto pb-6 pr-2">
        {/* Content (Cards, Grids, etc) */}
    </div>
  </div>
</div>

```

---

### 4. Typography & Micro-labels (The Visual Signature)

Use extreme combinations of size and weight to separate *Metadata* (Labels) from *Data* (Values).

* **Micro-labels (Field Titles):** Always use tiny, bold, uppercase letters with wide tracking.
* *Standard Class:* `text-[10px] font-bold uppercase tracking-wider text-slate-400`


* **Main Values:** Large and heavy fonts.
* *Standard Class:* `text-sm font-bold text-slate-800` or `text-xl font-black text-slate-900`


* **Structured Data (IDs, Currency, Plates, Weights):** ALWAYS use monospace fonts for tabular readability.
* *Standard Class:* `font-mono text-slate-700`



---

### 5. The B2B Card Pattern

Cards must be structured as information blocks with well-defined headers.

* **Border & Background:** `rounded-xl border border-slate-200 bg-white`
* **Card Header (Standard):** Fixed height for grid symmetry. Slightly gray background.
* `className="flex h-[52px] items-center gap-2 border-b border-slate-100 bg-slate-50/50 px-4 rounded-t-xl"`
* Always include a colored Icon (Lucide) + Uppercase Title (`text-xs font-bold uppercase text-slate-700`).


* **Card Body:** `p-4` or `p-5`. Use `space-y-4` to separate internal sections.
* **Background Alignment (`mt-auto`):** If cards are side-by-side (`grid-cols-2`) and one has less content, use `flex flex-col` on the card and `mt-auto` on the last element (like a button or footer) to push it to the bottom, keeping perfect alignment.

---

### 6. Semantic Colors

Do not overuse primary colors. The system should be mostly gray/white/slate, using colors strictly for meaning:

* **Slate (`slate-800`, `slate-500`, `slate-50`):** Structure, texts, borders, standard backgrounds.
* **Blue (`blue-600`, `blue-50`):** Primary actions, focus information, "Save" or "Submit Bid" buttons, links.
* **Emerald (`emerald-600`, `emerald-50`):** Money (Values receiving, profit, budget ceiling), positive status (Winning, Free), latest SLA delivery.
* **Amber (`amber-600`, `amber-50`):** Moderate alerts, "Under Maintenance" status, first SLA pickup.
* **Rose (`rose-600`, `rose-50`):** Destructive actions (Delete, Cancel Bid), critical status, lost auction, negative distances to the leader.

---

### 7. Modern Forms & Inputs

* **Base Inputs:** Clean, without heavy borders. Use `h-9` or `h-10` for compact screens.
* *Class:* `border-slate-200 text-sm focus:border-blue-500 focus:ring-blue-500`


* **Financial / High-Impact Inputs:** When it's a user bid or crucial value, remove the "form" look. Embed the suffix/prefix inline.
* *Example:*



```jsx
<div className="transition-all rounded-lg border border-slate-300 bg-white p-1 focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500">
    <div className="flex items-center px-2">
        <span className="text-xs font-bold text-slate-400">R$</span>
        <Input className="font-mono text-xl font-black text-slate-800 border-0 focus-visible:ring-0"/>
    </div>
</div>

```

---

### 8. Micro-interactions

* **Hovers:** Every clickable element must have a hover state. For table rows or lists, use `hover:bg-slate-50`.
* **Reveal on Hover (Hidden Actions):** For Edit/Delete buttons in lists, avoid visual pollution. Hide them with `opacity-0` and reveal on group hover.
* *Container:* `group relative ...`
* *Actions:* `absolute right-3 top-1/2 -translate-y-1/2 opacity-0 transition-opacity group-hover:opacity-100`



---

### 9. The Logistics Analytical Mindset

When designing a screen for a Logistics Operator or Carrier, answer these questions visually:

1. **Where:** Physical path (Origin and Destination must be visible instantly).
2. **What:** Is it palletized? Refrigerated? Truck type? (Crucial to avoid wasted trips).
3. **When:** Critical SLA (Show deadlines with badges).
4. **How much:** Ceiling, Current Bid, and "My Proposal" (Financial info separated from technical info).

---

### 10. AI Decision Making & Workflow (CRITICAL)

* **Ask Before Guessing:** If a backend contract differs from a prototype screen, or if you face multiple viable UX approaches, **STOP and ASK**. Present trade-offs and wait for confirmation before generating code.
* **Step-by-Step Protocol:**
1. Identify the entity and target endpoints (real unversioned paths; confirm DTO shapes from the C# DTOs when OpenAPI is thin).
2. Ask clarifying questions if needed.
3. Add/extend the service module in `src/services/*-service.js` with `toXxx` normalizers (see section 11).
4. Wire the component with the `useAsyncAction` hook and the standard state machine.
5. Adapt the component using these exact UI/UX rules (avoid bloated `p-8` paddings; favor `p-4` or `p-5`).
6. **Validate with `npx vite build`, not ESLint.** `npm run lint` currently fails across untouched pages (the `eslint-plugin-react-hooks` "latest" config flags pre-existing `react-hooks/set-state-in-effect`, which then cascades into false-positive `no-unused-vars` for JSX identifiers). This is a **known repo-wide lint baseline issue** — do not chase those errors on files you didn't change. The authoritative gate that a screen compiles is a clean `vite build`. Still, write **new** effects in the rule-compliant form from §11 so you don't add to the baseline.

---

### 11. API Integration Playbook (Proven Patterns)

Follow the existing, battle-tested layering. **Never call `fetch` directly from a component.**

* **HTTP layer — `src/lib/api-client.js`:** All requests go through `apiClient.get/post/put/patch/delete`. It prefixes the base URL (`src/config/env.js`, `VITE_API_BASE_URL`), serializes JSON, attaches the bearer token (registered once via `setAuthTokenProvider` in the auth layer), and normalizes **every** failure into an `ApiError { message, code, status, details }` — including RFC 9457 `problem+json` and the domain `{ error, message, details:[{field,reason}] }` shape. Import `ApiError` when you need to branch on failures.

* **Service layer — `src/services/<entity>-service.js`:** One module per backend resource. Export:
  * Named async functions that call `apiClient` and return **plain, normalized objects** (never raw DTOs).
  * A `toXxx(dto)` normalizer per response shape that null-coalesces every field (e.g. `metrics.totalBids ?? 0`). This is the FE's contract mirror — keep it in sync with the C# DTO.
  * A `BASE` constant with the real path (e.g. `const BASE = "/api/auctions"`).
  * A `toIso(value)` helper to convert `Date`/`datetime-local` values to ISO before sending; only include optional fields in the payload when the caller provided them (partial updates).
  * Also export a grouped `entityService` object for convenience.

* **Component layer — the `useAsyncAction` hook (`src/hooks/use-async-action.js`):** Wrap each API call: `const listAction = useAsyncAction((q) => listAuctions(q))`. It exposes `run(...args)` (returns `{ ok, data }` / `{ ok:false, error }`), `pending`, `error` (normalized), `fieldErrors` (map for inline form errors), and `reset()`. It also guards against setting state after unmount.

* **Standard data-screen state machine** (mirror `RouteSegmentManagement.jsx` / `FreightsOfferedOverview.jsx`):
  1. Local `useState` for the rendered collection/entity + a debounced search term (~350 ms) feeding a server-side `search` query param.
  2. A `loadX` callback wrapped in `useCallback`, invoked from a `useEffect`, that calls `action.run(...)` and stores `result.data` on `ok`. **ESLint caveat (`react-hooks/set-state-in-effect`):** calling a `loadX` that synchronously triggers `setState` directly inside the effect is flagged by the current `eslint-plugin-react-hooks` config. To satisfy the rule, run the async call *inside* the effect and set state in its `.then(...)` behind a mounted flag, e.g. `useEffect(() => { let active = true; run().then(r => { if (active && r.ok) setData(r.data) }); return () => { active = false } }, [run])`. Keep a separate `useCallback` wrapper for the "Tentar novamente" (retry) button. (Older screens still use the plain `loadX()`-in-effect form and remain flagged — see the lint-baseline note below.)
  3. Render branches **in this order**: `pending` → spinner (`<Loader2 className="animate-spin" />` + "Carregando…"); `error` → `<FormAlert message={action.error.message} />` + a "Tentar novamente" button calling `loadX`; empty → dashed-border empty state; not-found (detail screens) → "não encontrada"; else the content.
  4. After a successful mutation (create/update/delete), either patch local state optimistically **or refetch** — prefer a **refetch** when the server computes derived/display fields (e.g. an itinerary-fallback name) so the UI matches the server.
  5. On mutations, disable the action buttons while `action.pending` and swap the label for a spinner; surface `action.error.message` inline near the control.

* **Reading route params:** use `useParams()`; a route's `:id`/`:loadId` param is passed straight to `getById(id)`.

* **Dates:** send ISO (UTC) via the service `toIso`; for editing, use `<input type="datetime-local">` and convert both directions with small pad-based helpers (see `toDateTimeLocal` in `FreightsOfferedOverview.jsx`). Format for display with `Intl.DateTimeFormat("pt-BR", …)`.

* **Validation before shipping:** run **`npm run build`** (Vite) — it is the reliable gate. **`npm run lint` is currently misconfigured repo-wide** (it flags obviously-used imports via `no-unused-vars` even in untouched files), so a failing lint is not a signal your change is broken; do not "fix" unrelated files to satisfy it.
