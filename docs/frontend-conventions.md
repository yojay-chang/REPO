# Frontend conventions (detailed)

Frontend core rules plus widget/pattern recipes. The reference-feature index lives in
[`CLAUDE.md`](../CLAUDE.md); read this when building or modifying a component.

## Core

- **Angular 20 standalone** + signals; no NgModules. `MessageService`/`ConfirmationService` provided
  globally in `app.config.ts`.
- **API base URL** from `environments/environment*.ts` via `fileReplacements`.
- **Layout**: `features/{table-plural}/{table}-list|-detail|-form`; shared `core/models` + `core/services`.
- **List**: sortable `p-table`, `p-drawer` filter, session keys `{table}-list-{filters,sort,page}`.
- **Form**: Reactive Forms; `forkJoin` lookups; disable the PK control in edit, read via `getRawValue()`.

## FK dropdowns

- `p-select` bound to `filter.{x}Pkid` / `formControlName`, options mapped to `{ pkid, label }[]`
  from the lookup, `appendTo="body"`, `[filter]="true"` for 10+.
- Nullable FK adds `[showClear]="true"` + a （無）/全部 placeholder.
- List binds the JOINed label (`{{ course.partnerName }}`), never the raw pkid.
- N-N uses `p-multiselect` (`[maxSelectedLabels]="9999"`).

## Date columns

- `p-datepicker` in form; filter uses two-`p-datepicker` from/to ranges.
- Convert ISO `string` ↔ `Date` via `core/utils/date.util.ts` (`toIsoDate`/`parseIsoDate`) —
  **local** components only, never `toISOString()` (UTC+8 off-by-one).

## Bool columns

- List shows `pi pi-check`/`—`; detail uses `p-tag` 是/否; form uses `p-toggleswitch`.
- Filters are tri-state `p-select` (全部/是/否, `value:null` = no filter).

## Nav links

- **Primary-foreign** (FK-target entity): the detail page adds `[outlined]` buttons routing to each
  child list pre-filtered by `queryParams: { {parent}Pkid }` (e.g. Partner → `/courses?partnerPkid=`).
  Wire them even if the child feature is pending (Partner reference). The child list reads the param
  in `ngOnInit` and pre-fills the filter (Course reads `partnerPkid`).
- **Foreign-primary** (FK-consumer, e.g. Course detail): `[outlined]` buttons routing to each
  **parent's** detail page (`/partners/{partnerPkid}`); render the nullable-FK button only when the
  value is set.

## Sidebar nav

In `app.ts`/`app.html`, styled after Ultima, Aura `var(--p-*)` tokens.

## Inline list editing (Course list)

- **Double-click** a cell to edit; single-click does nothing. Read-only columns (PK + FK-label
  columns) have no editor.
- Editor matches the column: text `pInputText`, number `p-inputnumber`, date `p-datepicker`,
  `p-select` (dropdown), `p-checkbox` (bool). Auto-focus the new editor with the `shared/autofocus`
  directive.
- **Commit on blur** for text/number/date; **on change** for `p-select`/`p-checkbox` (their overlays
  blur spuriously).
- **Validate before persisting**: required fields not cleared, non-negative numbers, valid dates,
  cross-field (e.g. 上架日期 ≤ 下架日期). On failure show an inline error and stay in edit mode.
- **Persist** with `getById` then `update`, so the write does not wipe the N-N lists the list row
  omits. The row model is mutated only on success, so a failed save reverts automatically; surface
  the error.

## Sticky form toolbar

The form is a fixed-height flex column (`height: calc(100vh - 2rem)`) whose body (`.form-scroll`)
scrolls; the Save/Cancel card (`.sticky-toolbar` — `position: sticky; top: 0; z-index`) pins to the
top. A form-local scroller is required because the app `.content` region sets `overflow-x`, which
makes a naive page-level sticky bind to a non-scrolling ancestor and never stick. See Course form.

## QR code

Reusable `shared/qr-code` (`QrCode`) component + `QrCodeService` wrapping the `qrcode` dependency.
The parent passes a pre-built `value` URL + `title` + `fileName`; the component encodes it to a PNG
`data:` URL, shows the title, and downloads the PNG on demand. Used on the Course detail page.

## Row-audit history badge

Reusable standalone `shared/row-audit-badge` (`RowAuditBadge`) with signal inputs `tableName` (the
audited table, e.g. `'Course'`) and `pkid` (the record's **numeric** pkid; `null` on a new/unsaved
record). An `effect` fetches that record's trail via `RowAuditService.getForRecord` whenever `pkid`
becomes a positive number (unsaved records skip the request), and the badge shows the most recent entry
inline (`Update by alice · 2026-06-04 14:30`), or a neutral "尚無異動紀錄 no history" when there is none.
Clicking opens a `p-dialog` listing the full trail (DateTime / User / Action / Description), newest first,
with a "no history yet" empty state. Dropped into the `.page-actions` toolbar of every detail + edit page
of the six pkid-keyed entities (PublishStatus, Partner, CourseGroup, AppRole, AppUser, Course) — for the
string-keyed AppRole/AppUser and the create forms, bind `pkid` to the loaded record's numeric pkid
(`recordPkid()` / `pkidDisplay()`), which is `null` until a record exists. FeaturedPromoItem has no
single-record page, so it carries no badge. Because the badge injects `HttpClient`, any host page's unit
spec needs `provideHttpClient()` + `provideHttpClientTesting()`.

## Global error handling (interceptor)

`core/auth/auth.interceptor.ts` centralizes HTTP error handling for every request:

- **401** → `auth.logout()` + redirect to `/login` (unchanged).
- **500-class (`status >= 500`)** → surfaces a friendly error **toast** using the safe `message` from
  the response body (the backend's `{ "message": "An unexpected error occurred." }`), falling back to a
  generic bilingual message when the body carries none. It does **not** clear the session or redirect.
- **Other statuses (e.g. validation 400/403)** pass through untouched, so the form/page still handles
  them inline as before.

The toast is added via the globally-provided `MessageService` with a dedicated **`key`**
(`GLOBAL_TOAST_KEY = 'global'`), and the app shell hosts a matching app-level `<p-toast [key]="…">`
(`App` imports `ToastModule`) rendered **outside** the authenticated `@if`, so global errors show on any
route (including `/login`). The dedicated key isolates it from the keyless per-page `<p-toast>` used for
per-component success/save messages, so a global error never double-renders. Any spec whose component
renders that app-level toast (or that exercises the interceptor) must provide `MessageService`. Tests:
`auth.interceptor.spec` (500 → keyed error toast + no redirect/session-clear; 500 with no body → generic
fallback; 401 still redirects and raises no toast).
