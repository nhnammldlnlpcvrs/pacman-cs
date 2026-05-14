# Pacman Full Refactor — 8-Bit Arcade Edition

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor Pacman Blazor WASM to pro-grade 8-bit arcade: BFS ghost AI, input buffer queue, scatter/chase timer, SVG sprites, siren music, and 60fps smooth movement.

**Architecture:** Keep existing two-project structure (Pacman.Core + Pacman.UI). Replace greedy ghost AI with BFS pathfinding + unique chase personalities. Add input buffer queue replacing single-slot NextDirection. Replace PNG sprites with inline SVG drawn via CSS. Activate Web Audio API siren background music. Optimize Vercel deployment.

**Tech Stack:** .NET 9, Blazor WASM, C#, JS Interop, Web Audio API, CSS Grid, SVG

---

### Task 1: Input Buffer Queue

**Files:**
- Modify: `src/Pacman.Core/Entities/Pacman.cs`
- Modify: `src/Pacman.Core/Entities/Entity.cs`

- [ ] **Step 1: Add InputBuffer queue to Pacman.cs**

Replace the single `NextDirection` slot with a `Queue<Direction>` buffer (max 3 entries). Update `HandleInput` to enqueue. Update `Update()` to dequeue at tile centers.

```csharp
// src/Pacman.Core/Entities/Pacman.cs
using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public class Pacman : Entity
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;
    private readonly Queue<Direction> _inputBuffer = new();
    private const int MaxBufferSize = 3;

    public Pacman()
    {
        Id = "pacman";
        Speed = 150f;
    }

    public void HandleInput(Direction input)
    {
        if (input == Direction.None) return;

        // Don't queue duplicate consecutive inputs
        Direction lastQueued = _inputBuffer.Count > 0
            ? _inputBuffer.Last()
            : CurrentDirection;

        if (input != lastQueued && _inputBuffer.Count < MaxBufferSize)
            _inputBuffer.Enqueue(input);
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            // Try to consume buffered input
            while (_inputBuffer.Count > 0)
            {
                Direction next = _inputBuffer.Peek();
                if (CanMoveInDirection(next))
                {
                    CurrentDirection = next;
                    _inputBuffer.Dequeue();
                    break;
                }
                else
                {
                    // If the buffered direction is the same as current
                    // or its opposite, consume it (dead input)
                    if (next == CurrentDirection || next == CurrentDirection.Opposite())
                    {
                        _inputBuffer.Dequeue();
                        continue;
                    }
                    break; // Keep queued for next tile center
                }
            }

            // Stop if facing a wall
            if (CurrentDirection != Direction.None && !CanMoveInDirection(CurrentDirection))
            {
                CurrentDirection = Direction.None;
            }

            SnapToTileCenter();
        }

        base.Update(deltaTime);

        // Tunnel wrapping
        float maxX = MazeData.Width * MazeData.TileSize;
        if (X < -MazeData.TileSize) X = maxX;
        if (X > maxX) X = -MazeData.TileSize;
    }

    public string GetSprite()
    {
        return CurrentDirection switch
        {
            Direction.Up => "imgs/pacmanUp.png",
            Direction.Down => "imgs/pacmanDown.png",
            Direction.Left => "imgs/pacmanLeft.png",
            Direction.Right => "imgs/pacmanRight.png",
            _ => "imgs/pacmanRight.png"
        };
    }
}
```

- [ ] **Step 2: Remove NextDirection from Entity base class**

```csharp
// src/Pacman.Core/Entities/Entity.cs — remove the NextDirection property
public abstract class Entity
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Speed { get; set; } = 150f;
    public Direction CurrentDirection { get; set; } = Direction.None;
    public bool Visible { get; set; } = true;

    // Grid position snapped to nearest tile
    public int GridX => (int)Math.Round(X / MazeData.TileSize);
    public int GridY => (int)Math.Round(Y / MazeData.TileSize);

    public bool IsAtTileCenter
    {
        get
        {
            float cx = GridX * MazeData.TileSize;
            float cy = GridY * MazeData.TileSize;
            return Math.Abs(X - cx) < 2f && Math.Abs(Y - cy) < 2f;
        }
    }

    public void SnapToTileCenter()
    {
        X = GridX * MazeData.TileSize;
        Y = GridY * MazeData.TileSize;
    }

    public virtual void Update(float deltaTime)
    {
        if (CurrentDirection == Direction.None) return;

        var (dx, dy) = CurrentDirection.Delta();
        float newX = X + dx * Speed * deltaTime;
        float newY = Y + dy * Speed * deltaTime;

        int targetGx = (int)Math.Round(newX / MazeData.TileSize);
        int targetGy = (int)Math.Round(newY / MazeData.TileSize);

        if (IsCellWalkable(targetGx, targetGy))
        {
            X = newX;
            Y = newY;
        }
        else
        {
            SnapToTileCenter();
        }
    }

    protected virtual bool IsCellWalkable(int gridX, int gridY)
        => MazeData.IsWalkable(gridX, gridY, false);

    public virtual bool CanMoveInDirection(Direction dir)
    {
        var (dx, dy) = dir.Delta();
        return MazeData.IsWalkable(GridX + dx, GridY + dy, false);
    }

    public void SetPosition(int gridX, int gridY)
    {
        X = gridX * MazeData.TileSize;
        Y = gridY * MazeData.TileSize;
    }
}
```

- [ ] **Step 3: Update GameEngine.cs references to NextDirection**

In `ResetPositions()` and `Restart()`, remove lines setting `Pacman.NextDirection` (no longer exists).

```csharp
// In ResetPositions():
Pacman.CurrentDirection = Direction.None;
// Remove: Pacman.NextDirection = Direction.None;

// In Restart():
Pacman.CurrentDirection = Direction.None;
// Remove: Pacman.NextDirection = Direction.None;
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build src/Pacman.Core/Pacman.Core.csproj`

- [ ] **Step 5: Commit**

```bash
git add src/Pacman.Core/Entities/Pacman.cs src/Pacman.Core/Entities/Entity.cs src/Pacman.Core/GameEngine.cs
git commit -m "feat: replace single-slot NextDirection with 3-slot input buffer queue"
```

---

### Task 2: BFS Pathfinding for Ghost AI

**Files:**
- Create: `src/Pacman.Core/Pathfinding.cs`
- Modify: `src/Pacman.Core/Entities/Ghost.cs`

- [ ] **Step 1: Create BFS pathfinding utility**

```csharp
// src/Pacman.Core/Pathfinding.cs
using Pacman.Core.Enums;

namespace Pacman.Core;

public static class Pathfinding
{
    /// <summary>
    /// Returns the best direction to move from (startX, startY) toward (targetX, targetY)
    /// using BFS. The ghost cannot reverse direction.
    /// </summary>
    public static Direction FindBestDirection(
        int startX, int startY,
        int targetX, int targetY,
        Direction currentDir,
        bool isGhost)
    {
        if (currentDir == Direction.None)
            return Direction.None;

        var directions = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        Direction opposite = currentDir.Opposite();

        Direction bestDir = currentDir;
        double bestDist = double.MaxValue;

        foreach (var dir in directions)
        {
            if (dir == opposite) continue;

            var (dx, dy) = dir.Delta();
            int nx = startX + dx;
            int ny = startY + dy;

            if (!MazeData.IsWalkable(nx, ny, isGhost)) continue;

            // BFS from this neighbor to target
            int steps = BfsDistance(nx, ny, targetX, targetY, isGhost);
            if (steps < bestDist)
            {
                bestDist = steps;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    private static int BfsDistance(int fromX, int fromY, int toX, int toY, bool isGhost)
    {
        if (fromX == toX && fromY == toY) return 0;

        bool[,] visited = new bool[MazeData.Height, MazeData.Width];
        var queue = new Queue<(int x, int y, int dist)>();
        queue.Enqueue((fromX, fromY, 0));
        visited[fromY, fromX] = true;

        var directions = new[] { (0, -1), (-1, 0), (0, 1), (1, 0) }; // Up, Left, Down, Right

        while (queue.Count > 0)
        {
            var (x, y, dist) = queue.Dequeue();

            foreach (var (dx, dy) in directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx == toX && ny == toY) return dist + 1;

                if (nx < 0 || nx >= MazeData.Width || ny < 0 || ny >= MazeData.Height)
                    continue;
                if (visited[ny, nx]) continue;
                if (!MazeData.IsWalkable(nx, ny, isGhost)) continue;

                visited[ny, nx] = true;
                queue.Enqueue((nx, ny, dist + 1));
            }
        }

        return int.MaxValue; // No path — shouldn't happen in Pacman maze
    }
}
```

- [ ] **Step 2: Update Ghost.ChooseDirection() to use BFS**

```csharp
// src/Pacman.Core/Entities/Ghost.cs — replace ChooseDirection()
private void ChooseDirection()
{
    int targetX, targetY;

    if (Mode == GhostMode.Frightened)
    {
        // Random movement when frightened
        var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        var valid = dirs.Where(d => d != CurrentDirection.Opposite() && CanMoveInDirection(d)).ToArray();
        if (valid.Length > 0)
            CurrentDirection = valid[Random.Shared.Next(valid.Length)];
        return;
    }

    if (Mode == GhostMode.Eyes)
    {
        targetX = 14;
        targetY = 14;
    }
    else if (Mode == GhostMode.Scatter)
    {
        targetX = ScatterTargetX;
        targetY = ScatterTargetY;
    }
    else // Chase — use per-ghost personality
    {
        targetX = GetChaseTargetX();
        targetY = GetChaseTargetY();
    }

    Direction bestDir = Pathfinding.FindBestDirection(
        GridX, GridY, targetX, targetY, CurrentDirection, true);

    if (bestDir != CurrentDirection.Opposite() && CanMoveInDirection(bestDir))
        CurrentDirection = bestDir;
}
```

- [ ] **Step 3: Add chase personality methods to Ghost**

Add to Ghost class:

```csharp
// In Ghost class:
private GameEngine? _engine;

public void SetEngine(GameEngine engine) => _engine = engine;

private int GetChaseTargetX()
{
    return Color switch
    {
        "red" => _engine?.Pacman.GridX ?? TargetGridX,
        "pink" => GetPinkyTargetX(),
        "blue" => GetInkyTargetX(),
        "orange" => GetClydeTargetX(),
        _ => _engine?.Pacman.GridX ?? TargetGridX
    };
}

private int GetChaseTargetY()
{
    return Color switch
    {
        "red" => _engine?.Pacman.GridY ?? TargetGridY,
        "pink" => GetPinkyTargetY(),
        "blue" => GetInkyTargetY(),
        "orange" => GetClydeTargetY(),
        _ => _engine?.Pacman.GridY ?? TargetGridY
    };
}

// Pinky: targets 4 tiles ahead of Pacman
private int GetPinkyTargetX()
{
    var (dx, dy) = _engine!.Pacman.CurrentDirection.Delta();
    return _engine.Pacman.GridX + dx * 4;
}

private int GetPinkyTargetY()
{
    var (dx, dy) = _engine!.Pacman.CurrentDirection.Delta();
    return _engine.Pacman.GridY + dy * 4;
}

// Inky: uses vector from Blinky — 2 * (pacman + 2 ahead) - blinky
private int GetInkyTargetX()
{
    var p = _engine!.Pacman;
    var blinky = _engine.Ghosts[0]; // Red ghost
    var (dx, dy) = p.CurrentDirection.Delta();
    int pivotX = p.GridX + dx * 2;
    return pivotX + (pivotX - blinky.GridX);
}

private int GetInkyTargetY()
{
    var p = _engine!.Pacman;
    var blinky = _engine.Ghosts[0];
    var (dx, dy) = p.CurrentDirection.Delta();
    int pivotY = p.GridY + dy * 2;
    return pivotY + (pivotY - blinky.GridY);
}

// Clyde: chase Pacman if distance > 8 tiles, else go to scatter corner
private int GetClydeTargetX()
{
    var p = _engine!.Pacman;
    double dist = Math.Sqrt((GridX - p.GridX) * (GridX - p.GridX) + (GridY - p.GridY) * (GridY - p.GridY));
    return dist > 8 ? p.GridX : ScatterTargetX;
}

private int GetClydeTargetY()
{
    var p = _engine!.Pacman;
    double dist = Math.Sqrt((GridX - p.GridX) * (GridX - p.GridX) + (GridY - p.GridY) * (GridY - p.GridY));
    return dist > 8 ? p.GridY : ScatterTargetY;
}
```

- [ ] **Step 4: Wire Ghost.SetEngine() in GameEngine constructor**

In `GameEngine.cs` constructor, after creating ghosts:

```csharp
foreach (var ghost in Ghosts)
    ghost.SetEngine(this);
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Pacman.Core/Pacman.Core.csproj`

- [ ] **Step 6: Commit**

```bash
git add src/Pacman.Core/Pathfinding.cs src/Pacman.Core/Entities/Ghost.cs src/Pacman.Core/GameEngine.cs
git commit -m "feat: BFS pathfinding with unique ghost chase personalities"
```

---

### Task 3: Scatter/Chase Timer

**Files:**
- Modify: `src/Pacman.Core/GameEngine.cs`

- [ ] **Step 1: Add scatter/chase cycle timer**

Replace the unconditional `ghost.Mode = GhostMode.Chase` with a proper alternating timer (7s scatter, 20s chase, repeating).

```csharp
// In GameEngine class, add fields:
private double _modeTimer;
private bool _isScatterPhase = true;
private const double ScatterDuration = 7.0;
private const double ChaseDuration = 20.0;

// In GameEngine constructor, initialize:
_modeTimer = ScatterDuration;
```

- [ ] **Step 2: Add UpdateModeTimer method**

```csharp
private void UpdateModeTimer(double deltaTime)
{
    _modeTimer -= deltaTime;
    if (_modeTimer > 0) return;

    _isScatterPhase = !_isScatterPhase;
    _modeTimer = _isScatterPhase ? ScatterDuration : ChaseDuration;

    foreach (var ghost in Ghosts)
    {
        if (ghost.Mode == GhostMode.Frightened || ghost.Mode == GhostMode.Eyes)
            continue;

        ghost.Mode = _isScatterPhase ? GhostMode.Scatter : GhostMode.Chase;

        // Reverse direction on mode switch (classic behavior)
        ghost.CurrentDirection = ghost.CurrentDirection.Opposite();
    }
}
```

- [ ] **Step 3: Integrate into Update loop**

In `GameEngine.Update()`, replace the unconditional chase mode assignment:

```csharp
// Replace:
// if (ghost.Mode != GhostMode.Frightened && ghost.Mode != GhostMode.Eyes)
//     ghost.Mode = GhostMode.Chase;

// With:
if (State == GameState.Playing)
    UpdateModeTimer(deltaTime);

// And in the ghost loop, remove the forced Chase assignment, keep only:
foreach (var ghost in Ghosts)
{
    ghost.TargetGridX = Pacman.GridX;
    ghost.TargetGridY = Pacman.GridY;
    ghost.Update((float)deltaTime);
}
```

- [ ] **Step 4: Reset timer in Restart() and ResetPositions()**

```csharp
// In Restart():
_modeTimer = ScatterDuration;
_isScatterPhase = true;

// In ResetPositions():
_modeTimer = ScatterDuration;
_isScatterPhase = true;
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Pacman.UI/Pacman.UI.csproj`

- [ ] **Step 6: Commit**

```bash
git add src/Pacman.Core/GameEngine.cs
git commit -m "feat: 7s scatter / 20s chase alternating timer"
```

---

### Task 4: Sound System — Siren Music & Alternating Waka

**Files:**
- Modify: `src/Pacman.UI/wwwroot/js/gameInterop.js`
- Modify: `src/Pacman.Core/GameEngine.cs`

- [ ] **Step 1: Add alternating waka-waka and siren music to JS**

Replace the simple `_beep` for 'eat' with alternating two-tone waka, and rewrite `playMusic` with a proper siren sweep.

```javascript
// In gameInterop.js, replace playSound 'eat' case:
case 'eat':
    this._wakaStep = !this._wakaStep;
    this._beep(this._wakaStep ? 500 : 700, 0.05, 'square', 0.06);
    break;

// Add _wakaStep field in the object:
_wakaStep: false,

// Replace playMusic with siren:
playMusic: function () {
    if (!this.audioCtx || this.musicInterval) return;
    this.resumeAudio();
    var self = this;
    var baseFreq = 220;
    var sweepUp = true;
    this.musicInterval = setInterval(function () {
        if (!self.musicInterval) return;
        var freq = sweepUp ? baseFreq + 20 : baseFreq - 20;
        self._beep(freq, 0.15, 'square', 0.025);
        sweepUp = !sweepUp;
    }, 150);
},
```

- [ ] **Step 2: Call playMusic from C# when game starts playing**

In `Index.razor`'s `GameLoop` method, start music when state transitions to Playing:

```csharp
// In Index.razor GameLoop(), after state check:
if (_engine.State != _lastState)
{
    _lastState = _engine.State;
    if (_engine.State == GameState.Playing)
        _ = JS.InvokeVoidAsync("gameInterop.playMusic");
    else if (_engine.State is GameState.GameOver or GameState.Win)
        _ = JS.InvokeVoidAsync("gameInterop.stopMusic");
    StateHasChanged();
}
```

- [ ] **Step 3: Stop music on restart/pause states**

In `RestartGame()` method:

```csharp
private void RestartGame()
{
    _engine?.Restart();
    _lastState = GameState.Start;
    _ = JS.InvokeVoidAsync("gameInterop.stopMusic");
    _ = JS.InvokeVoidAsync("gameInterop.resumeAudio");
}
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build src/Pacman.UI/Pacman.UI.csproj`

- [ ] **Step 5: Commit**

```bash
git add src/Pacman.UI/wwwroot/js/gameInterop.js src/Pacman.UI/Pages/Index.razor
git commit -m "feat: siren background music and alternating waka-waka sound"
```

---

### Task 5: SVG Sprites Replace PNGs

**Files:**
- Modify: `src/Pacman.UI/wwwroot/css/app.css`
- Modify: `src/Pacman.UI/Pages/Index.razor`
- Modify: `src/Pacman.UI/wwwroot/js/gameInterop.js`

- [ ] **Step 1: Replace entity `<img>` elements with SVG divs in Index.razor**

Replace `<img>` tags with `<div>` elements that will be styled with CSS/SVG backgrounds:

```html
<div id="pacman" class="entity pacman-sprite"
     style="transform: translate(@(14 * 24)px, @(23 * 24)px);"></div>
<div id="ghost-red" class="entity ghost-sprite ghost-red-sprite ghost-float"
     style="transform: translate(@(12 * 24)px, @(12 * 24)px);"></div>
<div id="ghost-pink" class="entity ghost-sprite ghost-pink-sprite ghost-float"
     style="transform: translate(@(14 * 24)px, @(12 * 24)px);"></div>
<div id="ghost-blue" class="entity ghost-sprite ghost-blue-sprite ghost-float"
     style="transform: translate(@(11 * 24)px, @(12 * 24)px);"></div>
<div id="ghost-orange" class="entity ghost-sprite ghost-orange-sprite ghost-float"
     style="transform: translate(@(17 * 24)px, @(12 * 24)px);"></div>
```

- [ ] **Step 2: Add SVG-based CSS for Pacman**

Replace the mouth-open/mouth-closed clip-path with pure CSS Pacman (yellow circle with triangle mouth):

```css
/* Pacman — pure CSS sprite */
.pacman-sprite {
    width: var(--tile-size);
    height: var(--tile-size);
    background: #ffff00;
    border-radius: 50%;
    z-index: 20;
    filter: drop-shadow(0 0 6px rgba(255, 230, 0, 0.7));
}

/* Mouth is a dark triangle mask — animated via JS class toggle */
.pacman-sprite.mouth-open {
    clip-path: polygon(50% 50%, 100% 10%, 100% 90%);
}

.pacman-sprite.mouth-closed {
    clip-path: polygon(50% 50%, 100% 48%, 100% 52%);
}
```

- [ ] **Step 3: Add SVG-based CSS for ghosts**

Each ghost is a div with CSS `clip-path` to create the classic ghost shape:

```css
/* Ghost base — inverted U + wavy bottom */
.ghost-sprite {
    width: var(--tile-size);
    height: var(--tile-size);
    clip-path: polygon(
        5% 100%, 0% 15%, 0% 0%, 100% 0%, 100% 15%, 95% 100%,
        87% 85%, 80% 100%, 70% 85%, 62% 100%,
        52% 85%, 45% 100%, 35% 85%, 28% 100%,
        18% 85%, 10% 100%
    );
}

.ghost-red-sprite    { background: #ff0000; }
.ghost-pink-sprite   { background: #ffb8ff; }
.ghost-blue-sprite   { background: #00ffff; }
.ghost-orange-sprite { background: #ffb852; }

/* Frightened ghost */
.ghost-sprite.frightened {
    background: #2121de !important;
}

/* Ghost eyes — pseudo-elements for eyeballs */
.ghost-sprite::before {
    content: '';
    position: absolute;
    width: 6px; height: 8px;
    background: #fff;
    border-radius: 50%;
    top: 6px;
    left: 6px;
    box-shadow: 10px 0 0 #fff, 0 3px 0 #00f, 10px 3px 0 #00f;
}
```

- [ ] **Step 4: Update JS updateEntity() to handle SVG sprites**

Remove the sprite `src` logic and add frightened class toggling:

```javascript
updateEntity: function (state) {
    var el = this.entities[state.id];
    if (!el) {
        this.cacheEntities();
        el = this.entities[state.id];
        if (!el) return;
    }

    // Position
    el.style.display = state.visible ? 'block' : 'none';

    // Ghost frightened mode
    if (state.isGhost) {
        if (state.sprite && state.sprite.indexOf('scared') !== -1) {
            el.classList.add('frightened');
        } else {
            el.classList.remove('frightened');
        }
    }

    // Pacman: apply rotation + mouth
    if (state.id === 'pacman') {
        var rotation = 0;
        switch (state.facing) {
            case 1: rotation = -90; break;  // Up
            case 2: rotation = 90;  break;  // Down
            case 3: rotation = 180; break;  // Left
            default: rotation = 0;  break;
        }
        el.style.transform = 'translate(' + state.x + 'px, ' + state.y + 'px) rotate(' + rotation + 'deg)';

        if (state.isMouthOpen) {
            el.classList.add('mouth-open');
            el.classList.remove('mouth-closed');
        } else {
            el.classList.add('mouth-closed');
            el.classList.remove('mouth-open');
        }
    } else {
        el.style.transform = 'translate(' + state.x + 'px, ' + state.y + 'px)';
    }

    if (state.screenShake) {
        this.triggerShake();
    }
},
```

- [ ] **Step 5: Update cacheEntities to use querySelectorAll('.entity') (no change needed, already uses class)**

No change needed — `cacheEntities` already queries `.entity` class.

- [ ] **Step 6: Build and verify**

Run: `dotnet build src/Pacman.UI/Pacman.UI.csproj`

- [ ] **Step 7: Commit**

```bash
git add src/Pacman.UI/Pages/Index.razor src/Pacman.UI/wwwroot/css/app.css src/Pacman.UI/wwwroot/js/gameInterop.js
git commit -m "feat: replace PNG sprites with pure CSS/SVG characters"
```

---

### Task 6: Deployment Optimization

**Files:**
- Modify: `.github/workflows/deploy.yml`
- Modify: `vercel.json`

- [ ] **Step 1: Optimize deploy.yml for faster builds**

```yaml
name: Deploy to Vercel

on:
  push:
    branches: [main, master]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore src/Pacman.UI/Pacman.UI.csproj

      - name: Publish Blazor WASM
        run: dotnet publish src/Pacman.UI/Pacman.UI.csproj -c Release -o publish-output

      - name: Create .nojekyll
        run: touch publish-output/wwwroot/.nojekyll

      - name: Copy vercel.json to output
        run: cp vercel.json publish-output/wwwroot/

      - name: Deploy to Vercel
        uses: amondnet/vercel-action@v25
        with:
          vercel-token: ${{ secrets.VERCEL_TOKEN }}
          vercel-org-id: ${{ secrets.VERCEL_ORG_ID }}
          vercel-project-id: ${{ secrets.VERCEL_PROJECT_ID }}
          vercel-args: '--prod --yes'
          working-directory: publish-output/wwwroot
```

- [ ] **Step 2: Update vercel.json with proper SPA headers**

```json
{
  "version": 2,
  "public": true,
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ],
  "headers": [
    {
      "source": "/_framework/(.*)",
      "headers": [
        { "key": "Cache-Control", "value": "public, max-age=31536000, immutable" }
      ]
    },
    {
      "source": "/css/(.*)",
      "headers": [
        { "key": "Cache-Control", "value": "public, max-age=86400" }
      ]
    },
    {
      "source": "/js/(.*)",
      "headers": [
        { "key": "Cache-Control", "value": "public, max-age=86400" }
      ]
    }
  ]
}
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy.yml vercel.json
git commit -m "chore: optimize Vercel deploy with caching headers"
```

---

### Task 7: Minor Fixes — Arrow Keys, Dying State, Favicon

**Files:**
- Modify: `src/Pacman.UI/wwwroot/js/gameInterop.js`
- Modify: `src/Pacman.Core/GameEngine.cs`
- Create: `src/Pacman.UI/wwwroot/favicon.svg`

- [ ] **Step 1: Add arrow key support in JS keyboard handler**

```javascript
// In gameInterop.js initKeyboard(), add arrow key cases:
switch (e.key) {
    case 'w': case 'W': case 'ArrowUp':    dir = 1; break;
    case 's': case 'S': case 'ArrowDown':  dir = 2; break;
    case 'a': case 'A': case 'ArrowLeft':  dir = 3; break;
    case 'd': case 'D': case 'ArrowRight': dir = 4; break;
}
```

- [ ] **Step 2: Add Dying state animation flow**

In `GameEngine.cs`, when Pacman hits a non-frightened ghost:

```csharp
// In CheckGhostCollisions(), when ghost hits pacman:
else if (ghost.Mode != GhostMode.Eyes)
{
    State = GameState.Dying;
    _dyingTimer = 1.5; // 1.5 second death animation
    EmitSound("death");
    _shakeTimer = 0.5;
    return;
}

// Add Dying handling in Update():
if (State == GameState.Dying)
{
    _dyingTimer -= deltaTime;
    if (_dyingTimer <= 0)
    {
        Pacman.Lives--;
        if (Pacman.Lives <= 0)
            State = GameState.GameOver;
        else
            ResetPositions();
    }
    return GetEntityStates();
}
```

Add `private double _dyingTimer;` field to GameEngine.

- [ ] **Step 3: Create favicon.svg with Pacman icon**

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
  <circle cx="16" cy="16" r="14" fill="#ffff00"/>
  <polygon points="16,16 30,8 30,24" fill="#000"/>
  <circle cx="8" cy="12" r="2" fill="#000"/>
</svg>
```

Save to `src/Pacman.UI/wwwroot/favicon.svg`.

- [ ] **Step 4: Update index.html favicon reference**

```html
<link rel="icon" type="image/svg+xml" href="favicon.svg" />
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Pacman.UI/Pacman.UI.csproj`

- [ ] **Step 6: Commit**

```bash
git add src/Pacman.UI/wwwroot/js/gameInterop.js src/Pacman.Core/GameEngine.cs src/Pacman.UI/wwwroot/favicon.svg src/Pacman.UI/wwwroot/index.html
git commit -m "fix: arrow key support, dying state animation, SVG favicon"
```

---

### Verification Checklist

After all tasks complete, verify:

1. `dotnet build src/Pacman.UI/Pacman.UI.csproj` — builds without errors
2. `dotnet run --project src/Pacman.UI/Pacman.UI.csproj` — game runs, WASD/arrows work
3. BFS ghosts intelligently chase Pacman (not just greedy)
4. Scatter/Chase cycles alternate every 7s/20s
5. Siren music plays during gameplay
6. Characters render as CSS shapes (not PNG images)
7. Deployment config is valid JSON/YAML
