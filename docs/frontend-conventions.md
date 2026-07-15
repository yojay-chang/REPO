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
