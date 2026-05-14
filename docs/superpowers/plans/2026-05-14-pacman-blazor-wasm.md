# Pacman Blazor WASM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete Pacman clone with Blazor WASM (.NET 9), JS Interop 60fps rendering, Classic rules, and Vercel deployment.

**Architecture:** C# Class Library (Pacman.Core) holds all game logic — maze grid, entity movement with tile snapping, collision, ghost AI, scoring, game state. Blazor WASM project (Pacman.UI) hosts the UI shell and a JS bridge (`gameInterop.js`) that runs a requestAnimationFrame loop calling C# `GameLoop()` then directly updates DOM element transforms — bypassing Blazor's renderer for 60fps entity movement.

**Tech Stack:** .NET 9, Blazor WebAssembly, C# 13, JavaScript, CSS Grid, Vercel static deploy

**Key design decisions:**
- Tile size: 20px (maze = 28×20 by 31×20 = 560×620px play area)
- Pacman speed: 150 px/s, Ghost speed: 130 px/s (frightened: 80 px/s)
- Tile snapping threshold: ±2px from tile center before allowing direction change
- Power pellet duration: 7 seconds
- Ghost combo scoring: 200/400/800/1600, reset per power pellet

---

### Task 1: Create solution and project structure

**Files:**
- Create: `src/Pacman.Core/Pacman.Core.csproj`
- Create: `src/Pacman.UI/Pacman.UI.csproj`
- Create: `pacman-cs.sln` (at root)

- [ ] **Step 1: Create solution and class library**

```bash
cd "D:/Giselle_/My Project/pacman-cs"
dotnet new sln -n Pacman
mkdir -p src/Pacman.Core
cd src/Pacman.Core
dotnet new classlib -n Pacman.Core --framework net9.0
cd ../..
dotnet sln add src/Pacman.Core/Pacman.Core.csproj
```

- [ ] **Step 2: Create Blazor WASM project**

```bash
cd "D:/Giselle_/My Project/pacman-cs"
cd src
dotnet new blazorwasm -n Pacman.UI --framework net9.0 --pwa false
cd ..
dotnet sln add src/Pacman.UI/Pacman.UI.csproj
```

- [ ] **Step 3: Add project reference**

```bash
cd "D:/Giselle_/My Project/pacman-cs/src/Pacman.UI"
dotnet add reference ../Pacman.Core/Pacman.Core.csproj
```

- [ ] **Step 4: Copy assets to wwwroot**

```bash
cd "D:/Giselle_/My Project/pacman-cs"
cp -r imgs/* src/Pacman.UI/wwwroot/imgs/
```

- [ ] **Step 5: Remove default template files**

```bash
cd "D:/Giselle_/My Project/pacman-cs/src/Pacman.UI"
rm -f Pages/Counter.razor Pages/Weather.razor Pages/Home.razor
rm -rf Shared/
```

- [ ] **Step 6: Remove default Class1.cs from Core**

```bash
rm -f "D:/Giselle_/My Project/pacman-cs/src/Pacman.Core/Class1.cs"
```

- [ ] **Step 7: Verify build**

```bash
cd "D:/Giselle_/My Project/pacman-cs"
dotnet build
```
Expected: Build succeeded.

---

### Task 2: Create Direction.cs enum

**Files:**
- Create: `src/Pacman.Core/Enums/Direction.cs`

- [ ] **Step 1: Create Enums directory and Direction.cs**

```bash
mkdir -p "D:/Giselle_/My Project/pacman-cs/src/Pacman.Core/Enums"
```

- [ ] **Step 2: Write Direction.cs**

```csharp
namespace Pacman.Core.Enums;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    None
}

public static class DirectionExtensions
{
    public static (int dx, int dy) Delta(this Direction dir) => dir switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0)
    };

    public static Direction Opposite(this Direction dir) => dir switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => Direction.None
    };
}
```

---

### Task 3: Create MazeData.cs (28×31 classic maze)

**Files:**
- Create: `src/Pacman.Core/MazeData.cs`

- [ ] **Step 1: Write MazeData.cs**

```csharp
namespace Pacman.Core;

public static class MazeData
{
    public const int Width = 28;
    public const int Height = 31;

    // 0 = pellet, 1 = wall, 2 = power pellet, 3 = empty (ghost house)
    public static readonly int[,] Grid = new int[Height, Width]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,1,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,1,1,0,1},
        {1,2,1,1,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,1,1,2,1},
        {1,0,1,1,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,1,1,0,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,1,1,1,0,1,1,0,1,1,1,1,1,1,1,1,0,1,1,0,1,1,1,1,0,1},
        {1,0,1,1,1,1,0,1,1,0,1,1,1,1,1,1,1,1,0,1,1,0,1,1,1,1,0,1},
        {1,0,0,0,0,0,0,1,1,0,0,0,0,1,1,0,0,0,0,1,1,0,0,0,0,0,0,1},
        {1,1,1,1,1,1,0,1,1,1,1,1,3,1,1,3,1,1,1,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,1,1,1,3,1,1,3,1,1,1,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,3,3,3,3,3,3,3,3,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {0,0,0,0,0,0,0,3,3,3,1,1,1,1,1,1,1,1,3,3,3,0,0,0,0,0,0,0},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,3,3,3,3,3,3,3,3,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {1,1,1,1,1,1,0,1,1,3,1,1,1,1,1,1,1,1,3,1,1,0,1,1,1,1,1,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,1,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,1,1,0,1},
        {1,0,1,1,1,1,0,1,1,1,1,1,0,1,1,0,1,1,1,1,1,0,1,1,1,1,0,1},
        {1,2,0,0,1,1,0,0,0,0,0,0,0,3,3,0,0,0,0,0,0,0,1,1,0,0,2,1},
        {1,1,1,0,1,1,0,1,1,0,1,1,1,1,1,1,1,1,0,1,1,0,1,1,0,1,1,1},
        {1,1,1,0,1,1,0,1,1,0,1,1,1,1,1,1,1,1,0,1,1,0,1,1,0,1,1,1},
        {1,0,0,0,0,0,0,1,1,0,0,0,0,1,1,0,0,0,0,1,1,0,0,0,0,0,0,1},
        {1,0,1,1,1,1,1,1,1,1,1,1,0,1,1,0,1,1,1,1,1,1,1,1,1,1,0,1},
        {1,0,1,1,1,1,1,1,1,1,1,1,0,1,1,0,1,1,1,1,1,1,1,1,1,1,0,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
    };

    public static bool IsWall(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height)
            return false; // Tunnel wrapping — no walls at edges
        return Grid[gridY, gridX] == 1;
    }

    public static bool IsWalkable(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height)
            return true; // Tunnel wrapping allowed
        return Grid[gridY, gridX] != 1;
    }

    public static int GetCell(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height)
            return 0;
        return Grid[gridY, gridX];
    }

    public static void SetCell(int gridX, int gridY, int value)
    {
        if (gridX >= 0 && gridX < Width && gridY >= 0 && gridY < Height)
            Grid[gridY, gridX] = value;
    }
}
```

---

### Task 4: Create Entity.cs base class

**Files:**
- Create: `src/Pacman.Core/Entities/Entity.cs`

- [ ] **Step 1: Create Entities directory**

```bash
mkdir -p "D:/Giselle_/My Project/pacman-cs/src/Pacman.Core/Entities"
```

- [ ] **Step 2: Write Entity.cs**

```csharp
using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public abstract class Entity
{
    public const float TileSize = 20f;

    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int GridX => (int)Math.Round(X / TileSize);
    public int GridY => (int)Math.Round(Y / TileSize);
    public float Speed { get; set; } = 150f;
    public Direction CurrentDirection { get; set; } = Direction.None;
    public Direction NextDirection { get; set; } = Direction.None;
    public bool Visible { get; set; } = true;

    public bool IsAtTileCenter
    {
        get
        {
            float cx = GridX * TileSize;
            float cy = GridY * TileSize;
            return Math.Abs(X - cx) < 2f && Math.Abs(Y - cy) < 2f;
        }
    }

    public void SnapToTileCenter()
    {
        X = GridX * TileSize;
        Y = GridY * TileSize;
    }

    public virtual void Update(float deltaTime)
    {
        if (CurrentDirection == Direction.None) return;

        var (dx, dy) = CurrentDirection.Delta();
        float newX = X + dx * Speed * deltaTime;
        float newY = Y + dy * Speed * deltaTime;

        if (!MazeData.IsWall((int)Math.Round(newX / TileSize), (int)Math.Round(newY / TileSize)))
        {
            X = newX;
            Y = newY;
        }
        else
        {
            SnapToTileCenter();
        }
    }

    public bool CanMoveInDirection(Direction dir)
    {
        var (dx, dy) = dir.Delta();
        int targetX = GridX + dx;
        int targetY = GridY + dy;
        return MazeData.IsWalkable(targetX, targetY);
    }

    public void SetPosition(int gridX, int gridY)
    {
        X = gridX * TileSize;
        Y = gridY * TileSize;
    }
}
```

---

### Task 5: Create Pacman.cs

**Files:**
- Create: `src/Pacman.Core/Entities/Pacman.cs`

- [ ] **Step 1: Write Pacman.cs**

```csharp
using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public class Pacman : Entity
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;

    public Pacman()
    {
        Id = "pacman";
        Speed = 150f;
    }

    public void HandleInput(Direction input)
    {
        NextDirection = input;
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            if (NextDirection != Direction.None && CanMoveInDirection(NextDirection))
            {
                CurrentDirection = NextDirection;
                NextDirection = Direction.None;
            }
            else if (!CanMoveInDirection(CurrentDirection))
            {
                CurrentDirection = Direction.None;
            }
            SnapToTileCenter();
        }

        base.Update(deltaTime);

        // Tunnel wrapping
        if (X < -TileSize) X = MazeData.Width * TileSize;
        if (X > MazeData.Width * TileSize) X = -TileSize;
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

---

### Task 6: Create Ghost.cs

**Files:**
- Create: `src/Pacman.Core/Entities/Ghost.cs`

- [ ] **Step 1: Write Ghost.cs**

```csharp
using Pacman.Core.Enums;

namespace Pacman.Core.Entities;

public enum GhostMode
{
    Chase,
    Scatter,
    Frightened,
    Eyes
}

public class Ghost : Entity
{
    public GhostMode Mode { get; set; } = GhostMode.Chase;
    public string Color { get; set; } = "red";
    public int ScatterTargetX { get; set; }
    public int ScatterTargetY { get; set; }

    // Set by GameEngine before each Update call
    public int TargetGridX { get; set; }
    public int TargetGridY { get; set; }

    public Ghost(string id, string color, int scatterX, int scatterY)
    {
        Id = id;
        Color = color;
        ScatterTargetX = scatterX;
        ScatterTargetY = scatterY;
        Speed = 130f;
    }

    public override void Update(float deltaTime)
    {
        if (IsAtTileCenter)
        {
            ChooseDirection();
            SnapToTileCenter();
        }

        if (Mode == GhostMode.Frightened)
            Speed = 80f;
        else if (Mode == GhostMode.Eyes)
            Speed = 200f;
        else
            Speed = 130f;

        base.Update(deltaTime);

        if (X < -TileSize) X = MazeData.Width * TileSize;
        if (X > MazeData.Width * TileSize) X = -TileSize;
    }

    private void ChooseDirection()
    {
        int targetX, targetY;

        if (Mode == GhostMode.Frightened)
        {
            var dirs = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            var valid = dirs.Where(d => d != CurrentDirection.Opposite() && CanMoveInDirection(d)).ToArray();
            if (valid.Length > 0)
            {
                CurrentDirection = valid[Random.Shared.Next(valid.Length)];
            }
            return;
        }
        else if (Mode == GhostMode.Eyes)
        {
            targetX = 14;
            targetY = 14;
        }
        else if (Mode == GhostMode.Scatter)
        {
            targetX = ScatterTargetX;
            targetY = ScatterTargetY;
        }
        else // Chase
        {
            targetX = TargetGridX;
            targetY = TargetGridY;
        }

        var directions = new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
        Direction bestDir = CurrentDirection;
        double bestDist = double.MaxValue;

        foreach (var dir in directions)
        {
            if (dir == CurrentDirection.Opposite()) continue;
            if (!CanMoveInDirection(dir)) continue;

            var (dx, dy) = dir.Delta();
            int nx = GridX + dx;
            int ny = GridY + dy;
            double dist = Math.Sqrt((nx - targetX) * (nx - targetX) + (ny - targetY) * (ny - targetY));

            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = dir;
            }
        }

        CurrentDirection = bestDir;
    }

    public string GetSprite()
    {
        if (Mode == GhostMode.Frightened)
            return "imgs/scaredGhost.png";
        return $"imgs/{Color}Ghost.png";
    }
}
```

---

### Task 7: Create GameEngine.cs

**Files:**
- Create: `src/Pacman.Core/GameEngine.cs`

- [ ] **Step 1: Write GameEngine.cs**

```csharp
using Pacman.Core.Entities;
using Pacman.Core.Enums;

namespace Pacman.Core;

public enum GameState
{
    Start,
    Playing,
    Frightened,
    Dying,
    GameOver,
    Win
}

public class EntityState
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public string Sprite { get; set; } = "";
    public bool Visible { get; set; } = true;
}

public class GameEngine
{
    private readonly int[,] _mazeCopy;
    public Pacman Pacman { get; }
    public List<Ghost> Ghosts { get; }
    public GameState State { get; private set; } = GameState.Start;
    public int PelletsRemaining { get; private set; }
    public int GhostCombo { get; set; } = 0;

    private double _frightenedTimer;
    private const double FrightenedDuration = 7.0;
    private const float TileSize = Entity.TileSize;

    public GameEngine()
    {
        _mazeCopy = new int[MazeData.Height, MazeData.Width];
        Array.Copy(MazeData.Grid, _mazeCopy, MazeData.Grid.Length);

        Pacman = new Pacman();
        Pacman.SetPosition(14, 23);

        Ghosts = new List<Ghost>
        {
            new Ghost("ghost-red", "red", 25, 0)    { Mode = GhostMode.Scatter },
            new Ghost("ghost-pink", "pink", 2, 0)    { Mode = GhostMode.Scatter },
            new Ghost("ghost-blue", "blue", 25, 30)   { Mode = GhostMode.Scatter },
            new Ghost("ghost-orange", "orange", 2, 30) { Mode = GhostMode.Scatter }
        };
        Ghosts[0].SetPosition(14, 11);
        Ghosts[1].SetPosition(14, 14);
        Ghosts[2].SetPosition(12, 14);
        Ghosts[3].SetPosition(16, 14);

        CountPellets();
    }

    private void CountPellets()
    {
        PelletsRemaining = 0;
        for (int y = 0; y < MazeData.Height; y++)
            for (int x = 0; x < MazeData.Width; x++)
                if (_mazeCopy[y, x] == 0 || _mazeCopy[y, x] == 2)
                    PelletsRemaining++;
    }

    public void Start()
    {
        State = GameState.Playing;
    }

    public void HandleInput(Direction dir)
    {
        if (State == GameState.Start)
            Start();
        Pacman.HandleInput(dir);
    }

    public List<EntityState> Update(double deltaTime)
    {
        if (State != GameState.Playing && State != GameState.Frightened)
            return GetEntityStates();

        // Frightened timer
        if (State == GameState.Frightened)
        {
            _frightenedTimer -= deltaTime;
            if (_frightenedTimer <= 0)
            {
                State = GameState.Playing;
                GhostCombo = 0;
                foreach (var g in Ghosts)
                    if (g.Mode == GhostMode.Frightened)
                        g.Mode = GhostMode.Scatter;
            }
        }

        // Update Pacman
        Pacman.Update((float)deltaTime);
        EatPellets();

        // Ghost scatter→chase cycle (simple: scatter for 7s, chase for 20s)
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode != GhostMode.Frightened && ghost.Mode != GhostMode.Eyes)
                ghost.Mode = GhostMode.Chase;

            ghost.TargetGridX = Pacman.GridX;
            ghost.TargetGridY = Pacman.GridY;
            ghost.Update((float)deltaTime);
        }

        // Check ghost collisions
        CheckGhostCollisions();

        // Check win condition
        if (PelletsRemaining <= 0)
            State = GameState.Win;

        return GetEntityStates();
    }

    private void EatPellets()
    {
        int gx = Pacman.GridX;
        int gy = Pacman.GridY;

        if (gx < 0 || gx >= MazeData.Width || gy < 0 || gy >= MazeData.Height)
            return;

        int cell = _mazeCopy[gy, gx];
        if (cell == 0)
        {
            _mazeCopy[gy, gx] = 3;
            PelletsRemaining--;
            Pacman.Score += 10;
        }
        else if (cell == 2)
        {
            _mazeCopy[gy, gx] = 3;
            PelletsRemaining--;
            Pacman.Score += 50;
            ActivatePowerPellet();
        }
    }

    private void ActivatePowerPellet()
    {
        State = GameState.Frightened;
        _frightenedTimer = FrightenedDuration;
        GhostCombo = 0;
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode != GhostMode.Eyes)
            {
                ghost.Mode = GhostMode.Frightened;
                ghost.CurrentDirection = ghost.CurrentDirection.Opposite();
            }
        }
    }

    private void CheckGhostCollisions()
    {
        foreach (var ghost in Ghosts)
        {
            float dist = MathF.Sqrt(
                (Pacman.X - ghost.X) * (Pacman.X - ghost.X) +
                (Pacman.Y - ghost.Y) * (Pacman.Y - ghost.Y)
            );

            if (dist < TileSize * 0.7f)
            {
                if (ghost.Mode == GhostMode.Frightened)
                {
                    GhostCombo++;
                    int points = GhostCombo switch
                    {
                        1 => 200,
                        2 => 400,
                        3 => 800,
                        _ => 1600
                    };
                    Pacman.Score += points;
                    ghost.Mode = GhostMode.Eyes;
                    ghost.SetPosition(14, 14);
                }
                else if (ghost.Mode != GhostMode.Eyes)
                {
                    Pacman.Lives--;
                    if (Pacman.Lives <= 0)
                    {
                        State = GameState.GameOver;
                    }
                    else
                    {
                        ResetPositions();
                    }
                    return;
                }
            }
        }

        // Ghost eyes return to ghost house
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode == GhostMode.Eyes &&
                ghost.GridX == 14 && ghost.GridY == 14)
            {
                ghost.Mode = GhostMode.Scatter;
            }
        }
    }

    private void ResetPositions()
    {
        State = GameState.Playing;
        Pacman.SetPosition(14, 23);
        Pacman.CurrentDirection = Direction.None;
        Pacman.NextDirection = Direction.None;

        Ghosts[0].SetPosition(14, 11); Ghosts[0].Mode = GhostMode.Scatter;
        Ghosts[1].SetPosition(14, 14); Ghosts[1].Mode = GhostMode.Scatter;
        Ghosts[2].SetPosition(12, 14); Ghosts[2].Mode = GhostMode.Scatter;
        Ghosts[3].SetPosition(16, 14); Ghosts[3].Mode = GhostMode.Scatter;
        GhostCombo = 0;
    }

    public int[,] GetMaze() => _mazeCopy;

    private List<EntityState> GetEntityStates()
    {
        var states = new List<EntityState>
        {
            new() { Id = Pacman.Id, X = Pacman.X, Y = Pacman.Y, Sprite = Pacman.GetSprite(), Visible = true }
        };

        foreach (var ghost in Ghosts)
        {
            states.Add(new EntityState
            {
                Id = ghost.Id,
                X = ghost.X,
                Y = ghost.Y,
                Sprite = ghost.GetSprite(),
                Visible = true
            });
        }

        return states;
    }
}
```

---

### Task 8: Create gameInterop.js

**Files:**
- Create: `src/Pacman.UI/wwwroot/gameInterop.js`

- [ ] **Step 1: Write gameInterop.js**

```javascript
window.gameInterop = {
    dotNetRef: null,
    animFrameId: null,
    lastTime: 0,
    entities: {},

    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.lastTime = performance.now();
        this.cacheEntityElements();
    },

    cacheEntityElements: function () {
        const elements = document.querySelectorAll('.entity');
        elements.forEach(el => {
            this.entities[el.id] = el;
        });
    },

    startLoop: function () {
        const self = this;
        const loop = function (timestamp) {
            const deltaTime = (timestamp - self.lastTime) / 1000.0;
            self.lastTime = timestamp;

            if (self.dotNetRef) {
                self.dotNetRef.invokeMethodAsync('GameLoop', deltaTime)
                    .then(function (entityStates) {
                        for (let i = 0; i < entityStates.length; i++) {
                            self.updateDOM(entityStates[i]);
                        }
                    });
            }

            self.animFrameId = requestAnimationFrame(loop);
        };
        this.animFrameId = requestAnimationFrame(loop);
    },

    stopLoop: function () {
        if (this.animFrameId) {
            cancelAnimationFrame(this.animFrameId);
            this.animFrameId = null;
        }
    },

    updateDOM: function (state) {
        const el = this.entities[state.id];
        if (!el) return;

        el.style.transform = `translate(${state.x}px, ${state.y}px)`;
        el.style.display = state.visible ? 'block' : 'none';

        if (state.sprite && el.src !== state.sprite) {
            el.src = state.sprite;
        }
    },

    updatePellets: function (mazeJson) {
        const maze = JSON.parse(mazeJson);
        const container = document.querySelector('.maze-grid');
        if (!container) return;

        const cells = container.querySelectorAll('.maze-cell');
        let idx = 0;
        for (let y = 0; y < maze.length; y++) {
            for (let x = 0; x < maze[y].length; x++) {
                const cell = cells[idx];
                if (!cell) continue;
                idx++;

                if (maze[y][x] === 0) {
                    cell.classList.add('has-pellet');
                    cell.classList.remove('has-power-pellet', 'no-pellet');
                } else if (maze[y][x] === 2) {
                    cell.classList.add('has-power-pellet');
                    cell.classList.remove('has-pellet', 'no-pellet');
                } else {
                    cell.classList.add('no-pellet');
                    cell.classList.remove('has-pellet', 'has-power-pellet');
                }
            }
        }
    }
};
```

---

### Task 9: Write Index.razor

**Files:**
- Create: `src/Pacman.UI/Pages/Index.razor`

- [ ] **Step 1: Write Index.razor**

```html
@page "/"
@using Pacman.Core
@using Pacman.Core.Enums
@using Pacman.Core.Entities
@implements IDisposable
@inject IJSRuntime JS

<div class="game-wrapper" @onkeydown="HandleKeyDown" @onkeydown:preventDefault tabindex="0">
    <div class="game-header">
        <div class="score">SCORE: @_engine?.Pacman.Score</div>
        <div class="lives">LIVES: @_engine?.Pacman.Lives</div>
        <div class="state">@GetStateText()</div>
    </div>

    <div class="game-board" style="width: @(MazeData.Width * 20)px; height: @(MazeData.Height * 20)px;">
        <div class="maze-grid" style="grid-template-columns: repeat(@MazeData.Width, 20px); grid-template-rows: repeat(@MazeData.Height, 20px);">
            @for (int y = 0; y < MazeData.Height; y++)
            {
                for (int x = 0; x < MazeData.Width; x++)
                {
                    int cell = MazeData.Grid[y, x];
                    string cellClass = cell switch
                    {
                        1 => "maze-cell wall",
                        2 => "maze-cell has-power-pellet",
                        0 => "maze-cell has-pellet",
                        _ => "maze-cell no-pellet"
                    };
                    <div class="@cellClass"></div>
                }
            }
        </div>

        <img id="pacman" class="entity" src="imgs/pacmanRight.png" style="transform: translate(@(14 * 20)px, @(23 * 20)px);" />
        <img id="ghost-red" class="entity" src="imgs/redGhost.png" style="transform: translate(@(14 * 20)px, @(11 * 20)px);" />
        <img id="ghost-pink" class="entity" src="imgs/pinkGhost.png" style="transform: translate(@(14 * 20)px, @(14 * 20)px);" />
        <img id="ghost-blue" class="entity" src="imgs/blueGhost.png" style="transform: translate(@(12 * 20)px, @(14 * 20)px);" />
        <img id="ghost-orange" class="entity" src="imgs/orangeGhost.png" style="transform: translate(@(16 * 20)px, @(14 * 20)px);" />
    </div>

    @if (_engine?.State == GameState.GameOver)
    {
        <div class="overlay">GAME OVER</div>
    }
    else if (_engine?.State == GameState.Win)
    {
        <div class="overlay">YOU WIN!</div>
    }
    else if (_engine?.State == GameState.Start)
    {
        <div class="overlay">PRESS ARROW KEY TO START</div>
    }
</div>

@code {
    private GameEngine? _engine;
    private DotNetObjectReference<Index>? _dotNetRef;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _engine = new GameEngine();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("gameInterop.init", _dotNetRef);
            await JS.InvokeVoidAsync("gameInterop.startLoop");
            _initialized = true;

            // Focus the wrapper for keyboard events
            await JS.InvokeVoidAsync("eval", "document.querySelector('.game-wrapper').focus()");
        }
    }

    [JSInvokable]
    public List<EntityState> GameLoop(double deltaTime)
    {
        if (_engine == null) return new List<EntityState>();
        var states = _engine.Update(deltaTime);
        StateHasChanged();
        return states;
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        Direction dir = e.Key switch
        {
            "ArrowUp" or "w" or "W" => Direction.Up,
            "ArrowDown" or "s" or "S" => Direction.Down,
            "ArrowLeft" or "a" or "A" => Direction.Left,
            "ArrowRight" or "d" or "D" => Direction.Right,
            _ => Direction.None
        };

        if (dir != Direction.None)
        {
            _engine?.HandleInput(dir);
        }
    }

    private string GetStateText()
    {
        return _engine?.State switch
        {
            GameState.Start => "READY!",
            GameState.Playing => "",
            GameState.Frightened => "FRIGHTENED!",
            GameState.Dying => "OUCH!",
            GameState.GameOver => "",
            GameState.Win => "",
            _ => ""
        };
    }

    public void Dispose()
    {
        _ = JS.InvokeVoidAsync("gameInterop.stopLoop");
        _dotNetRef?.Dispose();
    }
}
```

---

### Task 10: Write app.css

**Files:**
- Modify: `src/Pacman.UI/wwwroot/css/app.css` (overwrite default)

- [ ] **Step 1: Write app.css**

```css
*, *::before, *::after {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
}

body {
    background: #000;
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    font-family: 'Press Start 2P', 'Courier New', monospace;
    overflow: hidden;
}

.game-wrapper {
    outline: none;
    position: relative;
}

.game-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 0;
    color: #fff;
    font-size: 16px;
    gap: 20px;
}

.score {
    color: #ffff00;
}

.lives {
    color: #00ffff;
}

.state {
    color: #ff00ff;
    animation: blink 0.5s infinite;
}

@keyframes blink {
    50% { opacity: 0; }
}

.game-board {
    position: relative;
    border: 2px solid #2121DE;
    overflow: hidden;
}

.maze-grid {
    display: grid;
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
}

.maze-cell {
    width: 20px;
    height: 20px;
}

.maze-cell.wall {
    background: url('imgs/wall.png') center/cover no-repeat;
    background-color: #2121DE;
    border: 1px solid #0000ff;
}

.maze-cell.has-pellet::after {
    content: '';
    display: block;
    width: 4px;
    height: 4px;
    background: #ffffaa;
    border-radius: 50%;
    margin: 8px auto;
}

.maze-cell.has-power-pellet::after {
    content: '';
    display: block;
    width: 10px;
    height: 10px;
    background: #ffffaa;
    border-radius: 50%;
    margin: 5px auto;
    animation: powerPulse 0.3s infinite alternate;
}

@keyframes powerPulse {
    from { transform: scale(1); opacity: 1; }
    to { transform: scale(1.3); opacity: 0.5; }
}

.maze-cell.no-pellet::after {
    content: none;
}

.entity {
    position: absolute;
    top: 0;
    left: 0;
    width: 20px;
    height: 20px;
    image-rendering: pixelated;
    will-change: transform;
    z-index: 10;
}

#pacman {
    z-index: 20;
}

.overlay {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    color: #ffff00;
    font-size: 24px;
    text-align: center;
    z-index: 100;
    text-shadow: 2px 2px #000;
    pointer-events: none;
}
```

---

### Task 11: Update index.html to include JS and font

**Files:**
- Modify: `src/Pacman.UI/wwwroot/index.html`

- [ ] **Step 1: Read current index.html then add JS reference and font in `<head>`**

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Pacman</title>
    <base href="/" />
    <link href="https://fonts.googleapis.com/css2?family=Press+Start+2P&display=swap" rel="stylesheet">
    <link href="css/app.css" rel="stylesheet" />
</head>
<body>
    <div id="app">
        <svg class="loading-progress">
            <circle r="40%" cx="50%" cy="50%" />
            <circle r="40%" cx="50%" cy="50%" />
        </svg>
        <div class="loading-progress-text"></div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="_framework/blazor.webassembly.js"></script>
    <script src="gameInterop.js"></script>
</body>
</html>
```

Add line after `<script src="_framework/blazor.webassembly.js"></script>`:
```html
    <script src="gameInterop.js"></script>
```

---

### Task 12: Create vercel.json

**Files:**
- Create: `vercel.json` (at project root)

- [ ] **Step 1: Write vercel.json**

```json
{
  "buildCommand": "dotnet publish src/Pacman.UI/Pacman.UI.csproj -c Release -o output",
  "outputDirectory": "output/wwwroot",
  "framework": "blazor-webassembly",
  "installCommand": "dotnet restore src/Pacman.UI/Pacman.UI.csproj",
  "rewrites": [
    {
      "source": "/(.*)",
      "destination": "/index.html"
    }
  ]
}
```

---

### Task 13: Remove unused template files

**Files:**
- Modify: `src/Pacman.UI/Pages/Index.razor` (overwrite with Task 9 content)
- Delete: `src/Pacman.UI/Pages/Counter.razor`, `src/Pacman.UI/Pages/Weather.razor`, `src/Pacman.UI/Pages/Home.razor`
- Delete: `src/Pacman.UI/Shared/` directory contents
- Modify: `src/Pacman.UI/wwwroot/css/app.css` (overwrite with Task 10 content)
- Modify: `src/Pacman.UI/wwwroot/index.html` (update with Task 11 content)

- [ ] **Step 1: Clean up and build**

```bash
cd "D:/Giselle_/My Project/pacman-cs/src/Pacman.UI"
rm -f Pages/Counter.razor Pages/Weather.razor Pages/Home.razor
rm -rf Shared/
cd ../..
dotnet build
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 2: Run locally to test**

```bash
cd "D:/Giselle_/My Project/pacman-cs/src/Pacman.UI"
dotnet run
```
Open browser to https://localhost:5000, press an arrow key — Pacman should move, pellets should disappear, ghost AI should chase.

---

### Task 14: Commit

- [ ] **Step 1: Commit all files**

```bash
cd "D:/Giselle_/My Project/pacman-cs"
git add -A
git commit -m "feat: implement Pacman game with Blazor WASM + JS Interop 60fps rendering"
```
