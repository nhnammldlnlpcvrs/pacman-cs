# Pacman — 8-Bit Arcade Edition

A classic Pacman clone built with **.NET 9 Blazor WebAssembly**, rendered at 60 FPS via **JS Interop + requestAnimationFrame**. Pure CSS sprites, Web Audio API 8-bit sound, deployed as a static site on **Vercel**.

## Features

- **Classic 28x31 maze** with pellets, power pellets, and ghost house
- **60 FPS smooth movement** — `requestAnimationFrame` drives the game loop, JS updates DOM transforms directly
- **Input buffer queue** (3 slots) — queue turns before reaching intersections, responsive WASD
- **BFS pathfinding** — each ghost has unique chase personality (Blinky/Pinky/Inky/Clyde)
- **Scatter/Chase cycle** — 7s scatter / 20s chase alternating timer with direction reversal
- **CSS sprites** — Pacman yellow circle + ghost clip-path silhouettes, zero image dependencies
- **8-bit sound** — Web Audio API synthesis: siren background music, alternating waka-waka, death sweep
- **Classic scoring** — 10 pts/pellet, 50 pts/power pellet, ghost combo 200/400/800/1600
- **3 lives**, Dying animation, Game Over and Win states

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Run Locally

```bash
# 1. Clone the repo
git clone https://github.com/nhnammldlnlpcvrs/pacman-cs.git
cd pacman-cs

# 2. Restore dependencies
dotnet restore

# 3. Run the dev server
dotnet run --project src/Pacman.UI

# 4. Open browser at the URL shown in terminal (usually http://localhost:5000)
```

## Controls

| Key | Action |
|---|------|
| `W` / `Arrow Up` | Move Up |
| `S` / `Arrow Down` | Move Down |
| `A` / `Arrow Left` | Move Left |
| `D` / `Arrow Right` | Move Right |
| Click game area | Focus + resume audio |

## Deploy

Push to `main` branch — GitHub Actions builds and deploys to Vercel automatically.

```bash
git push origin main
```

Manual deploy:

```bash
dotnet publish src/Pacman.UI -c Release -o publish-output
npx vercel publish-output/wwwroot --prod
```
