# Pacman 

A classic Pacman clone built with **.NET 9 Blazor WebAssembly**, rendered at 60 FPS via **JS Interop + requestAnimationFrame**. Deployed as a static site on **Vercel**.

## Features

- **Classic 28×31 maze** with pellets, power pellets, and ghost house

- **60 FPS smooth movement** — `requestAnimationFrame` drives the game loop, JS updates DOM transforms directly
- **Input buffering** — Pacman queues your next turn before reaching the intersection
- **Tile snapping** — direction changes only at grid-aligned positions
- **Ghost AI** — Chase, Scatter, Frightened, and Eyes modes with shortest-path targeting
- **Classic scoring** — 10 pts/pellet, 50 pts/power pellet, ghost combo 200→400→800→1600
- **3 lives**, game over and win states

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