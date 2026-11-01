# EventEase

A Blazor Server (.NET 10) front end for EventEase, a fictional corporate and social event management company. Users can browse events, view details, and register.

- **Activity 1:** Event Card component and basic routing.
- **Activity 2:** debugging and optimization (validation, invalid routes, large-list performance) plus automated tests.
- **Activity 3:** registration form with validation, user session tracking, attendance tracker, and production readiness.

## Features

- **EventCard component** (`Components/Shared/EventCard.razor`): shows event name, date, and location. Supports two-way binding via `@bind-Event` with a validated edit mode.
- **Mock data** (`Services/EventService.cs`): in-memory singleton with sample events, plus optional generated events for performance testing.
- **Event list search**: filter by name or location.
- **Registration form**: name, email, optional phone, seats and terms, with field-level and summary errors (see below).
- **User session tracker** (`Services/UserSession.cs`): remembers who the visitor is and which events they viewed.
- **Attendance tracker**: totals and per-event attendance, with check-in for each attendee.
- **Routing**
  | Route | Page |
  |---|---|
  | `/`, `/events` | Event list |
  | `/events/{Id:int}` | Event details (editable card) |
  | `/events/{Id:int}/register` | Registration form |
  | `/events/{Id:int}/attendance` | Attendee list with check-in |
  | `/attendance` | Attendance tracker across all events |
  | `/my-events` | The visitor's registrations and recently viewed events |
  | `/healthz` | Health check for hosting platforms |
  | anything else | "Page not found" with HTTP 404 |

## Activity 3: new features

### Registration form
- Validation rules: name 2–80 letters (accents, apostrophes and hyphens allowed), a real-looking email address, an optional phone number, 1–10 seats, and the terms box must be ticked. Clearing the optional phone field counts as "not provided".
- Errors show next to each field and in a summary at the top.
- Business rules are checked when saving, each with its own message: the same email can't register twice for one event, you can't book more seats than are left, and registration closes for past or sold-out events.
- The submit button is disabled while saving, so a double click can't register twice. Unexpected errors are logged and show a friendly message.
- Saved registrations are copies, normalized (trimmed, email lower-cased), and created under a lock, so concurrent sign-ups can't overbook.

### User session tracker
- `UserSession` is a scoped service: one per browser tab (Blazor circuit). It stores the visitor's name and email (set when they register or look up their bookings) and their last 5 viewed events.
- The session is saved to the browser's sessionStorage using `ProtectedSessionStorage`, which encrypts it, so it survives page reloads and lasts until the tab is closed. Storage is behind `ISessionStore`, so it can be tested without a browser.
- The header shows "Signed in as …" or "Browsing as guest"; the registration form is prefilled for returning visitors.
- If saved data can't be read (e.g. it was tampered with), the app logs a warning and starts a fresh session instead of failing.
- This is a lightweight visitor session, not authentication: there are no passwords, so it isn't a security boundary.

### Attendance tracker
- `/attendance` shows total registrations, seats booked, seats checked in, and the attendance rate, plus a per-event table (virtualized for large datasets).
- `/events/{id}/attendance` lists attendees with Check in / Undo buttons.
- Pages update live: `EventService` raises a `Changed` event after every registration or check-in, so an organizer's tracker refreshes when someone registers in another tab.
- Demo data includes a few sample attendees so the tracker isn't empty on first run.

### Production readiness
- Removed 43 unused Bootstrap files (8.4 MB of JavaScript, RTL/grid/reboot CSS variants and source maps); only `bootstrap.min.css` is used.
- Removed unused namespaces (`System.Net.Http`, `System.Net.Http.Json`, `Microsoft.JSInterop`) and the unused `coverlet.collector` test package.
- Replaced the template's external "About" link with the session status.
- Builds treat warnings as errors.
- Added a `/healthz` endpoint for hosting platforms and load balancers.
- Fixed a layout bug where wide tables made the whole page scroll sideways; every page was checked at 375 px and 711 px widths.
- A Release publish is 1.2 MB and was smoke-tested in Production mode: all pages return 200, bad paths return 404, the health check reports Healthy, and sessions persist across reloads.

## Activity 2: issues found and fixed

| Issue | Cause | Fix |
|---|---|---|
| Event Card accepted invalid input | Inputs were bound straight to the shared event, so an empty name, a whitespace-only location, or a cleared date went live immediately (a cleared date silently snapped back). | The card now edits a draft (`Models/EventEditModel.cs`) inside an `EditForm` with data-annotation rules: required fields, length limits, trimmed text, and a date within the next 5 years. `EventChanged` only fires with valid data; Cancel discards the draft. |
| Edits leaked to other users | The singleton event object was mutated on every keystroke. | The card hands the parent a copy; the details page saves it through `EventService.Update`. |
| Card crashed on missing data | A null `Event` threw a `NullReferenceException`. | Null or blank fields show fallback text. |
| Invalid routes | Unknown events rendered an empty "not found" message with HTTP 200; the Not Found page was the bare template. | Details and registration pages call `NavigationManager.NotFound()`, which returns HTTP 404 and shows a helpful Not Found page with a link back to events. Covers bad IDs, non-numeric IDs, and unknown paths. |
| Slow event list | Every event was rendered as a card, and the list was re-sorted on every request. | The list uses `<Virtualize>` to render only the rows on screen; the sorted list is cached until an event changes. |
| Concurrency | The shared singleton list was modified without synchronization (Blazor Server shares it between users). | Reads and writes are guarded by a lock. |
| Accessibility | Card labels weren't linked to their inputs. | Labels use `for`/`id`. |

### Performance (5,004 events, headless Chrome, median of runs)

| Measure | Before | After |
|---|---|---|
| Elements in page | 45,086 | 419 |
| Cards rendered | 5,004 | 39 |
| First HTML response | 2.7 MB | 8 KB |
| Navigate back to event list | 572 ms | 44 ms |
| Full page load until cards show | 549 ms | 306 ms |

## Run

```bash
dotnet run --project EventEase
```

Then open http://localhost:5151 (or open `EventEase.slnx` in Visual Studio and press F5).

To try a large dataset, add generated events:

```bash
dotnet run --project EventEase -- --EventEase:GeneratedEventCount=5000
```

## Deploy

```bash
dotnet publish EventEase -c Release -o ./publish
```

Run `./publish/EventEase` (or `dotnet EventEase.dll`) behind HTTPS on any host that supports ASP.NET Core 10, such as Azure App Service. Point the platform's health check at `/healthz`. Data is in memory, so it resets when the app restarts. Running more than one instance needs a shared database and a shared Data Protection key store, plus sticky sessions for Blazor Server.

## Tests

```bash
dotnet test
```

`EventEase.Tests` (xUnit + bUnit + `WebApplicationFactory`) covers:

- **Validation:** empty, whitespace-only, too short/long, missing, past, and far-future values.
- **Event Card:** invalid input is rejected and not saved, valid input is trimmed and saved, Cancel discards changes, null data doesn't crash.
- **Routing:** valid pages return 200; missing, zero, negative, and non-numeric IDs and unknown paths return 404 with the Not Found page.
- **Performance:** with 5,000+ events, the list renders a bounded number of cards and the page stays small; search filters correctly; the sorted list is cached.
- **Registration form:** every validation rule; empty, invalid and non-numeric input shows the right messages; duplicate, overbooked and sold-out cases show friendly errors; success confirms and starts the session; returning visitors are prefilled.
- **Registration rules:** saved copies, case-insensitive duplicate check, seat limits, closed events, and 100 concurrent sign-ups never overbooking.
- **Session:** identity and recent views survive a simulated reload; recent views are deduplicated and capped; sign out clears storage; storage failures don't break the page.
- **Attendance:** totals, live updates when someone registers, check-in and undo.
- **Routing:** new pages return 200, unknown events' attendance pages return 404, and `/healthz` reports Healthy.
