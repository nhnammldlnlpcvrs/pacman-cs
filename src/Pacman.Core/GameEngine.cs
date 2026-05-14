using Pacman.Core.Entities;
using Pacman.Core.Enums;
using PacmanPlayer = Pacman.Core.Entities.Pacman;

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
    public bool IsMouthOpen { get; set; }
    public bool IsGhost { get; set; }
    public int Facing { get; set; }
    public bool ScreenShake { get; set; }
}

public class GameEngine
{
    private readonly int[,] _mazeCopy;
    public PacmanPlayer Pacman { get; }
    public List<Ghost> Ghosts { get; }
    public GameState State { get; private set; } = GameState.Start;
    public int PelletsRemaining { get; private set; }
    public int GhostCombo { get; set; }

    // Timers
    private double _frightenedTimer;
    private const double FrightenedDuration = 7.0;
    private double _modeTimer;
    private bool _isScatterPhase = true;
    private const double ScatterDuration = 7.0;
    private const double ChaseDuration = 20.0;
    private double _mouthTimer;
    private bool _isMouthOpen;
    private double _shakeTimer;
    private double _dyingTimer;
    private const double DyingDuration = 1.5;
    private readonly List<string> _soundEvents = new();

    // Maximum deltaTime to prevent huge jumps (e.g. when tab is backgrounded)
    private const double MaxDeltaTime = 0.1;

    public bool IsRunning => State is GameState.Playing or GameState.Frightened;

    public List<string> FlushSoundEvents()
    {
        var copy = new List<string>(_soundEvents);
        _soundEvents.Clear();
        return copy;
    }

    private void EmitSound(string name) => _soundEvents.Add(name);

    public GameEngine()
    {
        _mazeCopy = new int[MazeData.Height, MazeData.Width];
        Array.Copy(MazeData.Grid, _mazeCopy, MazeData.Grid.Length);

        Pacman = new PacmanPlayer();
        Pacman.SetPosition(14, 23);

        Ghosts = new List<Ghost>
        {
            new("ghost-red", "red", 25, 0),
            new("ghost-pink", "pink", 2, 0),
            new("ghost-blue", "blue", 25, 30),
            new("ghost-orange", "orange", 2, 30)
        };
        Ghosts[0].SetPosition(12, 12);
        Ghosts[1].SetPosition(14, 12);
        Ghosts[2].SetPosition(11, 12);
        Ghosts[3].SetPosition(17, 12);

        foreach (var ghost in Ghosts)
            ghost.SetEngine(this);

        _modeTimer = ScatterDuration;
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
        if (State != GameState.Start) return;
        State = GameState.Playing;
        EmitSound("start");
    }

    public void HandleInput(Direction dir)
    {
        if (dir == Direction.None) return;

        // Start game on first input
        if (State == GameState.Start)
            Start();

        // Only accept input during active gameplay
        if (IsRunning)
            Pacman.HandleInput(dir);
    }

    public List<EntityState> Update(double deltaTime)
    {
        // Clamp deltaTime to avoid physics jumps
        if (deltaTime > MaxDeltaTime)
            deltaTime = MaxDeltaTime;
        if (deltaTime <= 0)
            return GetEntityStates();

        // Update screen shake timer
        if (_shakeTimer > 0)
            _shakeTimer -= deltaTime;

        // ── Non-running states ──────────────────────────
        if (!IsRunning)
        {
            if (State == GameState.Dying)
            {
                _dyingTimer -= deltaTime;
                if (_dyingTimer <= 0)
                {
                    if (Pacman.Lives <= 0)
                        State = GameState.GameOver;
                    else
                        ResetPositions();
                }
            }
            return GetEntityStates();
        }

        // ── Frightened timer ────────────────────────────
        if (State == GameState.Frightened)
        {
            _frightenedTimer -= deltaTime;
            if (_frightenedTimer <= 0)
            {
                State = GameState.Playing;
                GhostCombo = 0;
                foreach (var g in Ghosts)
                {
                    if (g.Mode == GhostMode.Frightened)
                    {
                        g.Mode = _isScatterPhase ? GhostMode.Scatter : GhostMode.Chase;
                    }
                }
            }
        }

        // ── Player movement ─────────────────────────────
        Pacman.Update((float)deltaTime);

        // ── Mouth animation ─────────────────────────────
        _mouthTimer -= deltaTime;
        if (_mouthTimer <= 0)
        {
            _isMouthOpen = Pacman.CurrentDirection != Direction.None && !_isMouthOpen;
            _mouthTimer = 0.12;
        }

        // ── Pellet collection ───────────────────────────
        EatPellets();

        // ── Scatter/Chase cycle (only during normal play) ──
        if (State == GameState.Playing)
            UpdateModeTimer(deltaTime);

        // ── Ghost updates ───────────────────────────────
        foreach (var ghost in Ghosts)
        {
            ghost.TargetGridX = Pacman.GridX;
            ghost.TargetGridY = Pacman.GridY;
            ghost.Update((float)deltaTime);
        }

        // ── Collision detection ─────────────────────────
        CheckGhostCollisions();

        // ── Win condition ───────────────────────────────
        if (PelletsRemaining <= 0)
            State = GameState.Win;

        return GetEntityStates();
    }

    // ═══════════════════════════════════════════════════════
    //  Pellet System
    // ═══════════════════════════════════════════════════════

    private void EatPellets()
    {
        int gx = Pacman.GridX;
        int gy = Pacman.GridY;

        if (gx < 0 || gx >= MazeData.Width || gy < 0 || gy >= MazeData.Height)
            return;

        int cell = _mazeCopy[gy, gx];
        switch (cell)
        {
            case 0: // Regular pellet
                _mazeCopy[gy, gx] = 3;
                PelletsRemaining--;
                Pacman.Score += 10;
                EmitSound("eat");
                break;

            case 2: // Power pellet
                _mazeCopy[gy, gx] = 3;
                PelletsRemaining--;
                Pacman.Score += 50;
                EmitSound("powerup");
                ActivatePowerPellet();
                break;
        }
    }

    private void ActivatePowerPellet()
    {
        State = GameState.Frightened;
        _frightenedTimer = FrightenedDuration;
        GhostCombo = 0;
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode == GhostMode.Eyes) continue;
            ghost.Mode = GhostMode.Frightened;
            ghost.CurrentDirection = ghost.CurrentDirection.Opposite();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scatter / Chase Mode Timer
    // ═══════════════════════════════════════════════════════

    private void UpdateModeTimer(double deltaTime)
    {
        _modeTimer -= deltaTime;
        if (_modeTimer > 0) return;

        _isScatterPhase = !_isScatterPhase;
        _modeTimer = _isScatterPhase ? ScatterDuration : ChaseDuration;

        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode is GhostMode.Frightened or GhostMode.Eyes) continue;

            ghost.Mode = _isScatterPhase ? GhostMode.Scatter : GhostMode.Chase;
            ghost.CurrentDirection = ghost.CurrentDirection.Opposite();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Ghost Collision
    // ═══════════════════════════════════════════════════════

    private void CheckGhostCollisions()
    {
        float hitDist = MazeData.TileSize * 0.65f;

        foreach (var ghost in Ghosts)
        {
            float dx = Pacman.X - ghost.X;
            float dy = Pacman.Y - ghost.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq >= hitDist * hitDist) continue;

            switch (ghost.Mode)
            {
                case GhostMode.Frightened:
                    GhostCombo++;
                    int points = GhostCombo switch
                    {
                        1 => 200, 2 => 400, 3 => 800, _ => 1600
                    };
                    Pacman.Score += points;
                    ghost.Mode = GhostMode.Eyes;
                    EmitSound("ghostEat");
                    break;

                case GhostMode.Eyes:
                    // Ghost is returning to house — no collision
                    break;

                default:
                    // Player dies
                    State = GameState.Dying;
                    _dyingTimer = DyingDuration;
                    Pacman.Lives--;
                    Pacman.CurrentDirection = Direction.None;
                    Pacman.ClearInputBuffer();
                    _isMouthOpen = false;
                    EmitSound("death");
                    _shakeTimer = 0.5;
                    return;
            }
        }

        // Check if eyes-mode ghosts have reached the ghost house
        foreach (var ghost in Ghosts)
        {
            if (ghost.Mode == GhostMode.Eyes && ghost.GridX == 14 && ghost.GridY == 14)
            {
                ghost.Mode = _isScatterPhase ? GhostMode.Scatter : GhostMode.Chase;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Reset & Restart
    // ═══════════════════════════════════════════════════════

    private void ResetPositions()
    {
        State = GameState.Playing;
        Pacman.SetPosition(14, 23);
        Pacman.CurrentDirection = Direction.None;
        Pacman.ClearInputBuffer();

        Ghosts[0].SetPosition(12, 12); Ghosts[0].Mode = GhostMode.Scatter;
        Ghosts[1].SetPosition(14, 12); Ghosts[1].Mode = GhostMode.Scatter;
        Ghosts[2].SetPosition(11, 12); Ghosts[2].Mode = GhostMode.Scatter;
        Ghosts[3].SetPosition(17, 12); Ghosts[3].Mode = GhostMode.Scatter;

        GhostCombo = 0;
        _modeTimer = ScatterDuration;
        _isScatterPhase = true;
        _dyingTimer = 0;
        _isMouthOpen = false;
        _mouthTimer = 0;
    }

    public int[,] GetMaze() => _mazeCopy;

    public void Restart()
    {
        Array.Copy(MazeData.Grid, _mazeCopy, MazeData.Grid.Length);
        CountPellets();

        Pacman.SetPosition(14, 23);
        Pacman.CurrentDirection = Direction.None;
        Pacman.ClearInputBuffer();
        Pacman.Score = 0;
        Pacman.Lives = 3;

        Ghosts[0].SetPosition(12, 12); Ghosts[0].Mode = GhostMode.Scatter;
        Ghosts[1].SetPosition(14, 12); Ghosts[1].Mode = GhostMode.Scatter;
        Ghosts[2].SetPosition(11, 12); Ghosts[2].Mode = GhostMode.Scatter;
        Ghosts[3].SetPosition(17, 12); Ghosts[3].Mode = GhostMode.Scatter;

        GhostCombo = 0;
        _frightenedTimer = 0;
        _modeTimer = ScatterDuration;
        _isScatterPhase = true;
        _mouthTimer = 0;
        _isMouthOpen = false;
        _shakeTimer = 0;
        _dyingTimer = 0;
        State = GameState.Start;
    }

    // ═══════════════════════════════════════════════════════
    //  Entity State Serialization
    // ═══════════════════════════════════════════════════════

    private List<EntityState> GetEntityStates()
    {
        bool shaking = _shakeTimer > 0;
        var states = new List<EntityState>(5)
        {
            new()
            {
                Id = Pacman.Id, X = Pacman.X, Y = Pacman.Y,
                Sprite = Pacman.GetSprite(), Visible = true,
                IsMouthOpen = _isMouthOpen, IsGhost = false,
                Facing = (int)Pacman.CurrentDirection, ScreenShake = shaking
            }
        };

        foreach (var ghost in Ghosts)
        {
            states.Add(new EntityState
            {
                Id = ghost.Id, X = ghost.X, Y = ghost.Y,
                Sprite = ghost.GetSprite(), Visible = true,
                IsGhost = true, Facing = 0, ScreenShake = shaking
            });
        }

        return states;
    }
}
