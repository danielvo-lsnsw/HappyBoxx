# HappyBoxx UI Layout & UX Guidelines

> Applies to **every** HappyBoxx front-end screen (admin portal now, other internal tools later).
> The buyer storefront may adapt the shell, but status colours, touch sizing and contrast rules still apply.
>
> **MUST** = mandatory · **SHOULD** = strong default.

UI kit: **Mantine** (`@mantine/core`), icons: **Tabler** (`@tabler/icons-react`).
Shared building blocks live in `src/components/` – use them instead of re-creating layout per page.

---

## 1. Top Navigation Bar (Header)

**Position:** fixed across the top of the screen (`AppShell.Header`, 64px).
**Purpose:** global tools, quick actions and system-wide visibility.

| Element | Placement | Behaviour | Component |
|---|---|---|---|
| **Global search** | Centre / left | Finds SKUs, products, order numbers, batch codes, customers. Opens with a click or **Ctrl/⌘ + K**. Results are grouped (Navigation, Products, …). | `GlobalSearch` (Mantine Spotlight) |
| **Quick action (+)** | Right of search | Prominent primary button that starts common tasks from anywhere: *New product, New category, Adjust stock, Create order, Log delivery, Receive shipment*. | `QuickActions` |
| **Notification bell** | Right | Badge count + popover of time-sensitive alerts: low stock, items nearing expiry, delayed deliveries. Each alert links to the affected record. | `NotificationBell` |
| **User profile & settings** | Far right | Avatar menu: switch warehouse, account settings, clock in/out, light/dark mode, sign out. | `UserMenu` |

Rules:
- The header **MUST NOT** scroll away.
- The header **MUST NOT** contain page-specific actions. Those go in the Page Header (§3).

## 2. Left Sidebar (Primary Navigation)

**Position:** fixed on the left (`AppShell.Navbar`).
**Collapsible:** full width (260px) ↔ icon rail (76px), so wide data tables have room. On tablet and mobile it becomes an off-canvas drawer opened by the burger button. The collapsed state is remembered per user.

Module order (single source of truth: `src/app/navigation.tsx`):

1. **Dashboard** – high-level metrics.
2. **Inventory**
   - Products · Fresh Produce (Vegetables, Fruits) · Packaging (Containers) · Stock Levels · Categories · Spoilage / Quality Control
3. **Orders** – Pending · Processing / Picking · Ready for Dispatch · Returns
4. **Logistics & Delivery** – Route Planning · Driver Dispatch · Proof of Delivery
5. **Purchasing & Suppliers**
6. **Customers**
7. **Reports & Analytics** – financials, yield, waste

Rules:
- Every route **MUST** be registered in `navigation.tsx`. The sidebar, breadcrumbs and global search all read from this file.
- Modules that aren't built yet show a "Coming soon" page with a `Soon` badge in the menu. Don't hide them, so users can see the roadmap.
- The active item and its parent group **MUST** be highlighted.

## 3. Main Content Area (Workspace)

Every page uses this vertical structure:

```
┌ PageHeader ───────────────────────────────────────────────┐
│ Breadcrumbs                                               │
│ Title + short description            [Secondary] [Primary]│
├ FilterBar ────────────────────────────────────────────────┤
│ [Search] [Dropdown filters…]  [Unit toggle]   [Reset]     │
├ Data view ────────────────────────────────────────────────┤
│ DataTable (dense) or Kanban board                         │
├ Pagination ───────────────────────────────────────────────┤
│ Showing 1–20 of 135      [Rows per page]   ‹ 1 2 3 … ›    │
└───────────────────────────────────────────────────────────┘
```

- **Page Header (`PageHeader`)**: breadcrumbs (generated automatically), a clear title, an optional description, and the page's primary action buttons (e.g. *New product*, *Export*, *Print pick list*). There is at most **one** filled primary button per page.
- **Filter & sort ribbon (`FilterBar`)**: filter state lives in the **URL query string**, so views can be shared, bookmarked and survive a refresh. Always offer a *Reset* when any filter is active.
- **Data view**
  - *Inventory and other lists*: `DataTable`. Dense rows, sticky header, striped, row hover, the whole row is clickable. Typical inventory columns: SKU, Item, Category, Quantity, Unit, Location (Zone/Bin), Status.
  - *Orders*: Kanban board (New → Picking → Packed → Out for Delivery) with a list-view toggle for bulk processing.
  - Numbers are right-aligned with tabular figures. Text is left-aligned.
- **Pagination (`TablePagination`)**: server-side, with a rows-per-page selector (20/50/100) and a "Showing x–y of z" summary.
- Every data view **MUST** handle *loading* (skeleton), *error* (alert with retry) and *empty* (friendly message + primary action) states. Use `QueryState`.

## 4. Contextual Right Panel (Slide-out Drawer)

**Position:** hidden by default. Slides in from the right (`Drawer position="right"`, 480–560px) when a row is clicked or a "New …" action is used.
**Purpose:** view or edit details **without leaving the list**.

- The open record is part of the URL (e.g. `?productId=…`, `?create=1`), so a deep link opens the same panel.
- Layout: title + status badge → key facts → sections (details, stock, history) → sticky footer with *Cancel* / *Save*.
- Examples:
  - **Product**: details, current stock with unit toggle, batches, supplier, storage requirements, price history.
  - **Order** *(future)*: packing slip, customer address, assigned driver, items to pick.
- Closing the drawer **MUST** return focus to the list. Unsaved changes **SHOULD** ask for confirmation.

---

## 5. Industry-specific UX rules

### 5.1 Colour-coded status tags (`StatusBadge`)

| Tone | Colour | Meaning (examples) |
|---|---|---|
| `success` | **Green** | Fresh · In stock · Active · Delivered |
| `warning` | **Yellow** | Low stock · Approaching expiry · Delayed |
| `danger` | **Red** | Out of stock · Spoiled · Failed delivery |
| `neutral` | **Grey** | Inactive · Draft · Not tracked |
| `info` | **Blue** | Processing · In transit |

- Status **MUST** always have a **text label**, not colour alone (accessibility / colour-blindness).
- Use these colours only for status, not for decoration.

### 5.2 Touch-friendly elements
Warehouse staff use tablets, often with gloves.
- Minimum touch target **44 × 44 px**. Default button size is `md`. Workflow actions (*Pick, Pack, Dispatch*) use `lg`.
- Keep at least 8px spacing between adjacent tap targets.
- Don't put critical actions behind hover-only UI. Hover effects are a bonus, never the only way to reach something.

### 5.3 Unit toggles (`UnitToggle`)
Produce and packaging are counted in different units. Wherever quantities appear, users **SHOULD** be able to switch the display unit (e.g. *kg ↔ g*, *units ↔ cartons ↔ pallets*).
- Conversion uses `src/lib/units.ts`. Only offer conversions that are known for the item (pack sizes come from Catalog).
- The stored/base unit is always shown in tooltips or details, so there's no ambiguity.

### 5.4 High contrast & dark mode
- Text contrast of at least **WCAG AA 4.5:1**. Body text is near-black on white (light mode) or near-white on dark grey (dark mode).
- Filled buttons use shade 7+ (e.g. `brand`, `red.8`). Lighter fills with white text fail contrast.
- A dark mode toggle is in the user menu. It follows the OS setting by default.
- Don't put light-grey text on white for important data. Secondary text uses `dimmed` only for supporting information.

---

## 6. Visual language

| Token | Value |
|---|---|
| Primary colour | `brand` green (fresh produce), filled shade 7 `#15803d` – ≥4.5:1 with white text in both modes |
| Font | System UI stack (`Inter` if available); tabular numbers in tables |
| Radius | `md` (8px) |
| Spacing scale | Mantine `xs–xl`; page padding `lg` |
| Elevation | Cards and drawers use `shadow="sm"` and a 1px border, never heavy shadows |
| Icons | Tabler, stroke 1.75, 18–20px in navigation |

## 7. Accessibility checklist (per screen)
- [ ] Keyboard reachable. Visible focus ring. Drawer traps focus and closes with Esc.
- [ ] Every input has a label. Errors are shown under the field (server validation errors are mapped to their fields).
- [ ] Tables use `<th scope="col">`. Clickable rows also respond to Enter.
- [ ] Status uses text + colour.
- [ ] Contrast checked in both light and dark mode.
