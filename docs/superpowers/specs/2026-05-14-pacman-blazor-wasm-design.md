# Pacman Blazor WASM — Design Spec

**Date:** 2026-05-14  
**Status:** Approved

## Tech Stack
- .NET 9 Blazor WebAssembly (static site)
- C# for game logic, JS Interop for 60fps rendering
- Deploy: Vercel via `vercel.json`

## Project Structure
```
src/Pacman.Core/   — Class Library: MazeData, Entity, Pacman, Ghost, GameEngine
src/Pacman.UI/     — Blazor WASM: Index.razor, gameInterop.js, app.css
imgs/              — Assets (copy to wwwroot/imgs/)
```

## Architecture: Approach 2 — JS Interop + requestAnimationFrame

**Data Flow per frame (~16ms):**
```
JS requestAnimationFrame
  → DotNet.invokeMethodAsync("GameLoop", deltaTime)
    → C# GameEngine.Update(deltaTime)
      → MoveEntity() with tile snapping + collision
      → Return List<EntityState>
  → JS updateDOM(id, x, y, direction) → transform: translate(x, y)
```

## Core Systems

### Maze: 28×31 Classic Grid
- 0 = empty/pellet, 1 = wall, 2 = power pellet, 3 = ghost house
- `MazeData.cs` static class with 2D int array

### Entity System
- Abstract `Entity` with float X,Y (pixel), int GridX,GridY (tile)
- `Pacman` with input buffer (NextDirection stored, applied at tile center)
- `Ghost` with Chase/Scatter/Frightened modes, simple target-tile AI

### Movement & Collision
- Tile snapping: direction change only when centered on tile (±2px)
- Collision: check target pixel against MazeData wall tiles before moving
- Slide along walls when diagonal input meets wall

### Game State Machine
Start → Playing → Frightened (7s) → Playing  
Playing → Win (all pellets) | GameOver (0 lives) | Dying (ghost hit)

### Scoring (Classic)
- Pellet: 10pts, Power Pellet: 50pts
- Ghost combo: 200/400/800/1600 (resets per power pellet)
- 3 lives

### Rendering
- Maze: CSS Grid, `.wall` tiles use `wall.png` background
- Entities: `<img>` elements with absolute positioning, `transform: translate(x,y)`
- JS directly manipulates DOM — no Blazor re-render for game loop

### Deployment
- `vercel.json`: `dotnet publish` → `output/wwwroot` as static site
