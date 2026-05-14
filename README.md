# 🟡 Pacman — Blazor WebAssembly

A classic Pacman clone built with **.NET 9 Blazor WebAssembly**, rendered at 60 FPS via **JS Interop + requestAnimationFrame**. Deployed as a static site on **Vercel**.

## Features

- **Classic 28×31 maze** with pellets, power pellets, and ghost house
- **60 FPS smooth movement** — `requestAnimationFrame` drives the game loop, JS updates DOM transforms directly
- **Input buffering** — Pacman queues your next turn before reaching the intersection
- **Tile snapping** — direction changes only at grid-aligned positions
- **Ghost AI** — Chase, Scatter, Frightened, and Eyes modes with shortest-path targeting
- **Classic scoring** — 10 pts/pellet, 50 pts/power pellet, ghost combo 200→400→800→1600
- **3 lives**, game over and win states

## Project Structure

```
pacman-cs/
├── src/
│   ├── Pacman.Core/          # Class Library — all game logic
│   │   ├── Enums/Direction.cs
│   │   ├── MazeData.cs       # 28×31 classic maze grid
│   │   ├── Entities/         # Entity, Pacman, Ghost
│   │   └── GameEngine.cs     # Game loop, collision, scoring
│   └── Pacman.UI/            # Blazor WASM — rendering shell
│       ├── Pages/Index.razor  # Game board + keyboard handler
│       ├── wwwroot/
│       │   ├── js/gameInterop.js   # JS bridge (rAF → C# → DOM)
│       │   ├── css/app.css         # Neon maze styling
│       │   └── imgs/               # Game sprites
│       └── Layout/
├── vercel.json               # Vercel deploy config
└── .gitignore
```

## Architecture

The game splits into three layers: **Pacman.Core** (C# game logic), **gameInterop.js** (60 FPS bridge), and **Pacman.UI** (Blazor shell + CSS).

```mermaid
sequenceDiagram
    participant JS as gameInterop.js
    participant C# as GameEngine (C#)
    participant DOM as Browser DOM

    loop 60 FPS
        JS->>JS: requestAnimationFrame
        JS->>C#: Invoke GameLoop(deltaTime)
        C#->>C#: Move Pacman & Ghosts
        C#->>C#: Eat pellets, check collisions
        C#-->>JS: Return List&lt;EntityState&gt;
        JS->>DOM: style.transform = translate(x, y)
        JS->>DOM: Swap sprite src if direction changed
    end
```

```mermaid
graph TD
    subgraph Pacman.Core
        MD[MazeData<br/>28×31 grid]
        E[Entity<br/>X,Y pixel + GridX,GridY]
        P[Pacman<br/>input buffer + tunnel wrap]
        G[Ghost<br/>Chase/Scatter/Frightened/Eyes]
        GE[GameEngine<br/>state machine + scoring]
    end

    subgraph Pacman.UI
        IR[Index.razor<br/>CSS Grid maze + onkeydown]
        CSS[app.css<br/>neon walls + pellet animations]
    end

    subgraph Browser
        JSI[gameInterop.js<br/>rAF loop + DOM transforms]
    end

    MD --> GE
    E --> P
    E --> G
    P --> GE
    G --> GE
    IR -->|DotNetObjectReference| JSI
    JSI -->|invokeMethodAsync| GE
    GE -->|EntityState[]| JSI
    JSI -->|translate + src| DOM
    CSS --> DOM
    IR -->|onkeydown| GE
```

### Key Design Decisions

**Tile snapping.** Entities only change direction when centered on a tile (`|X - GridX * 24| < 2px`). Pacman's `NextDirection` buffers the last keypress and applies it at the next tile center. If the buffered direction faces a wall, it is silently discarded.

**Ghost AI.** Each ghost picks the direction that minimizes distance to a target tile. In Chase mode the target is Pacman's position. In Scatter mode each ghost targets a different corner (red → top-right, pink → top-left, blue → bottom-right, orange → bottom-left). In Frightened mode movement is random. Ghosts never reverse direction except when entering Frightened mode.

**Rendering strategy.** Blazor renders the maze once via CSS Grid. The hot loop bypasses Blazor entirely: `requestAnimationFrame` calls C# for logic, then JavaScript sets `transform: translate()` directly on `<img>` elements. This is a compositor-only CSS property — the browser animates it on the GPU without triggering layout or paint. Blazor only re-renders for score, lives, and overlay text changes.

**Scoring.** Regular pellets: 10 pts. Power pellets: 50 pts plus 7 seconds of Frightened mode. Eating ghosts in sequence: 200 → 400 → 800 → 1600 pts, resetting each power pellet. Three lives, game over at zero, win when all pellets are cleared.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Vercel CLI](https://vercel.com/docs/cli) (for deployment)

## Run Locally

```bash
# Clone the repo
git clone <repo-url> && cd pacman-cs

# Run the dev server
dotnet run --project src/Pacman.UI

# Open http://localhost:5000 — press any arrow key to start
```

## Controls

| Key | Action |
|---|---|
| `↑` `↓` `←` `→` | Move Pacman |
| `W` `A` `S` `D` | Alternative movement |

## Deploy to Vercel

```bash
# Login (first time only)
npx vercel login

# Deploy to production
npx vercel --prod
```

Vercel reads `vercel.json` — it runs `dotnet publish`, then serves `output/wwwroot` as a static site with SPA rewrites.

## Tech Stack

| Layer | Technology |
|---|---|
| Game Logic | C# 13, .NET 9 |
| UI Shell | Blazor WebAssembly |
| Rendering | JavaScript, CSS Grid, `transform: translate()` |
| Assets | PNG sprites (pixel art) |
| Deployment | Vercel (static site) |

## License

MIT
