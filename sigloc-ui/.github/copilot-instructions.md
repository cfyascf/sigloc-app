### 🎨 Frontend Copilot Instructions: React + Vite (Logistics Domain & B2B UI/UX)

**Role & Context**
You are a Senior Frontend Engineer working on **FreightGuard / SIGLOC**, an enterprise B2B logistics management dashboard built with **React (Vite)**, **Tailwind CSS**, and **shadcn/ui**.
Your goal is to create modern, high-density, clean, and highly usable interfaces for logistics analysts. The frontend is fully integrated with a live .NET 10 REST API.

---

### 1. Architecture & Source of Truth
* **Backend is King:** The backend is the single source of truth for all data contracts, schemas, and business rules. Frontend state and forms must adapt to backend contracts, never the reverse.
* **Scalar API Integration:** Check `https://sigloc-api-hbfzcfd6ghephmcc.centralus-01.azurewebsites.net/scalar/v1` for exact endpoints, HTTP verbs, payload shapes, and status codes. Align all TypeScript interfaces with these contracts.
* **Modularity:** Extract duplicated table columns, status badges, formatters (currency, dates, weights), and dialogs into shared components. Use custom hooks (e.g., `useAuctions`) to decouple data fetching from presentation.
* **Language Convention:** All source code (variables, functions, files) **MUST** be in **English**. All UI text displayed to the user **MUST** be in **Portuguese (pt-BR)**.

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
1. Identify the entity and target endpoints from Scalar.
2. Ask clarifying questions if needed.
3. Define TypeScript interfaces.
4. Implement the API service/custom hook.
5. Adapt the component using these exact UI/UX rules (avoid bloated `p-8` paddings; favor `p-4` or `p-5`).
