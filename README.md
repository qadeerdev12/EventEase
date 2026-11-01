# EventEase

A Blazor Server (.NET 10) front end for EventEase, a fictional corporate and social event management company. Users can browse events, view details, and register.

- **Activity 1:** Event Card component and basic routing.
- **Activity 2:** debugging and optimization (validation, invalid routes, large-list performance) plus automated tests.
- **Activity 3:** expansion (coming next).

## Features

- **EventCard component** (`Components/Shared/EventCard.razor`): shows event name, date, and location. Supports two-way binding via `@bind-Event` with a validated edit mode.
- **Mock data** (`Services/EventService.cs`): in-memory singleton with sample events, plus optional generated events for performance testing.
- **Event list search**: filter by name or location.
- **Routing**
  | Route | Page |
  |---|---|
  | `/`, `/events` | Event list |
  | `/events/{Id:int}` | Event details (editable card) |
  | `/events/{Id:int}/register` | Registration form with validation |
  | anything else | "Page not found" with HTTP 404 |

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

## Tests

```bash
dotnet test
```

`EventEase.Tests` (xUnit + bUnit + `WebApplicationFactory`) covers:

- **Validation:** empty, whitespace-only, too short/long, missing, past, and far-future values.
- **Event Card:** invalid input is rejected and not saved, valid input is trimmed and saved, Cancel discards changes, null data doesn't crash.
- **Routing:** valid pages return 200; missing, zero, negative, and non-numeric IDs and unknown paths return 404 with the Not Found page.
- **Performance:** with 5,000+ events, the list renders a bounded number of cards and the page stays small; search filters correctly; the sorted list is cached.
