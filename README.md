# EventEase

A Blazor Server (.NET 10) front end for EventEase, a fictional corporate and social event management company. Users can browse events, view details, and register.

This is Activity 1 of 3: the Event Card component and basic routing. Activity 2 covers debugging and optimization; Activity 3 covers expansion.

## Features

- **EventCard component** (`Components/Shared/EventCard.razor`): shows event name, date, and location; supports two-way binding via `@bind-Event` with an inline edit mode.
- **Mock data** (`Services/EventService.cs`): in-memory singleton with sample events.
- **Routing**
  | Route | Page |
  |---|---|
  | `/`, `/events` | Event list |
  | `/events/{Id:int}` | Event details (editable card) |
  | `/events/{Id:int}/register` | Registration form with validation |

## Run

```bash
dotnet run --project EventEase
```

Then open http://localhost:5151 (or open `EventEase.slnx` in Visual Studio and press F5).
